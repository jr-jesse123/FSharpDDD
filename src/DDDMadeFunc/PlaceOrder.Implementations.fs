

module internal OrderTaking.PlaceOrder.Implementation

open OrderTaking.PlaceOrder
open OrderTaking.Common
open OrderTaking.PlaceOrder.InternalTypes
open System.Runtime.ExceptionServices
open System.ComponentModel.DataAnnotations
open Newtonsoft.Json.Schema
open System.Numerics

// ==================================================================
// Final implementation for the PlaceOrderWorkflow
// =================================================================


// ----------------------------------------------------
// validateOrder step
// ----------------------------------------------------


let toCustomerInfo (unvalidatedCustomerInfo : UnvalidatedCostumerInfo) =
    let mapError a = 
        Result.mapError  (ValidationError >> List.singleton) a
        
    result {
        let! firstName = 
            unvalidatedCustomerInfo.FirstName
            |> String50.create "FirstName"
            |> mapError

        and! lastName = 
            unvalidatedCustomerInfo.LastName
            |> String50.create "LastName"
            |> mapError

        and! emailAddress = 
            unvalidatedCustomerInfo.EmailAddress
            |> EmailAddress.create "EmailAddress"
            |> mapError

        and! vipStatus = 
            unvalidatedCustomerInfo.VipStatus 
            |> VipStatus.fromString "vipStatus"
            |> mapError

        let customerInfo = {
            Name = {FirstName = firstName; LastName = lastName }
            EmailAddress = emailAddress
            VipStatus = vipStatus
        }

        return customerInfo
    }


/// Call the checkAddressExists and convert the error to a ValidationError
let toAddress (CheckedAddress checkedAddress)  =
    result {
        let! addressLine1 = 
            checkedAddress.AddressLine1 
            |> String50.create "AddressLine1"
            |> Result.mapError (ValidationError >> List.singleton)


        and! addressLine2 = 
            checkedAddress.AddressLine2
            |> String50.createOption "AddressLine2"
            |> Result.mapError (ValidationError >> List.singleton)

        and! addressLine3 = 
            checkedAddress.AddressLine3
            |> String50.createOption "AddressLine3"
            |> Result.mapError (ValidationError >> List.singleton)

        and! addressLine4 = 
            checkedAddress.AddressLine4
            |> String50.createOption "AddressLine4"
            |> Result.mapError (ValidationError >> List.singleton)

        and! city = 
            checkedAddress.City
            |> String50.create "City"
            |> Result.mapError (ValidationError >> List.singleton)

        and! zipCode = 
            checkedAddress.ZipCode
            |> ZipCode.create "ZipCode"
            |> Result.mapError (ValidationError >> List.singleton)

        and! state = 
            checkedAddress.State
            |> UsStateCode.create "State"
            |> Result.mapError (ValidationError >> List.singleton)

        and! country = 
            checkedAddress.Country
            |> String50.create "Country"
            |> Result.mapError (ValidationError >> List.singleton)

        let address = Address.create (
            addressLine1, addressLine2, addressLine3, addressLine4, city, zipCode, state, country
        )

        return address
    }

/// Call the checkAddressExists and convert the error toa ValidationError
let toCheckedAddress (checkAddres:CheckAddressExists) address =
    let toValidationError err = 
        match err with
        | AddressNotFound -> ValidationError "Address not found"
        | InvalidFormat -> ValidationError "Address has bad format"

    address
    |> checkAddres
    |> AsyncResult.mapError toValidationError

let toOrderId orderId = 
    orderId
    |> OrderId.create "OrderId"
    |> Result.mapError ValidationError


/// Helper function for validateOrder 
let toOrderLineId orderLineId = 
    orderLineId
    |> OrderLineId.create "OrderLineId"
    |> Result.mapError ValidationError

/// Helper function for validateOrder
let toProductCode (checkProductCodeExists:CheckProductCodeExists) productCode =
    
    // create a ProductCode -> Result<ProductCode,...> function 
    // suitable for using in a pipeline
    let checkProduct productCode = 
        if checkProductCodeExists productCode then
            Ok productCode
        else
            sprintf "Invalid: %A" productCode
            |> ValidationError
            |> Error

    productCode 
    |> ProductCode.create "ProductCode"
    |> Result.mapError ValidationError
    |> Result.bind checkProduct
    

/// helper function for validateOrder
let toOrderQuantity productCode quantity = 
    OrderQuantity.create "OrderQuantity" productCode quantity
    |> Result.mapError ValidationError


let toValidateOrderLine checkProductExists (unvalidatedOrderLine:UnvalidatedOrderLine) =
    result {
        let! orderlineId =
            unvalidatedOrderLine.OrderLineId |> toOrderLineId

        let! productCode = unvalidatedOrderLine.ProductCode |> toProductCode checkProductExists

        let! quantity = unvalidatedOrderLine.Quantity |> toOrderQuantity productCode

        let validatedOrderline : ValidatedOrderLine = {
            OrderLineId = orderlineId
            ProductCode = productCode
            Quantity = quantity
        }

        return validatedOrderline
    }


let validateOrder (*: ValidateOrder*) = 
    fun checkproductCodeExists checkAddressExists (unvalidatedOrder : UnvalidatedOrder) ->
        let toCheckedAddress = toCheckedAddress checkAddressExists
        asyncResult {
            let! orderId = 
                unvalidatedOrder.OrderId
                |> toOrderId
                |> Result.mapError List.singleton
                |> AsyncResult.ofResult
                

            and! customerInfo = 
                unvalidatedOrder.CustomerInfo
                |> toCustomerInfo
                //|> Result.mapError List.singleton
                |> AsyncResult.ofResult
                //|> List.singleton

            and! checkedShippingAddres = 
                unvalidatedOrder.ShippingAddress 
                |> toCheckedAddress 
                |> AsyncResult.mapError List.singleton

            
            //and! checkedBillingAddres = 
              

            and! billingAddress = 
                unvalidatedOrder.BillingAddress 
                |> toCheckedAddress 
                |> AsyncResult.mapError (List.singleton)
                |> AsyncResult.bind (toAddress >> AsyncResult.ofResult )
                //|> AsyncResult.mapError List.singleton
                //|> AsyncResult.ofResult

            and! lines = 
                unvalidatedOrder.Lines
                |> List.map (toValidateOrderLine checkproductCodeExists)
                |> Result.sequence//convert a list of Results to a single Result //TODO: change this to applicative
                |> Result.mapError List.singleton
                |> AsyncResult.ofResult


            let pricingMethod = 
                unvalidatedOrder.PromotionCode
                |> PricingModule.createPricingMethod

            let! shippingAddress = 
                checkedShippingAddres
                |> toAddress 
                |> AsyncResult.ofResult


            let validatedOrder : ValidatedOrder = {
                OrderId = orderId
                CustomerInfo = customerInfo
                ShippingAddress = shippingAddress
                BillingAddress = billingAddress
                Lines = lines
                PricingMethod = pricingMethod
            }
            
            return validatedOrder
        }

        
// ----------------------------------
// PriceOrder setp
// ----------------------------------

let toPricedOrderLine (getProductPrice:GetProductPrice) (validatedOrderLine:ValidatedOrderLine) =
    result{
        let qty = validatedOrderLine.Quantity.value
        let price = validatedOrderLine.ProductCode |> getProductPrice
        let! linePrice = 
            Price.multiply qty price
            |> Result.mapError PricingError
        
        let pricedLine : PricedOrderProductLine = {
            OrderLineId = validatedOrderLine.OrderLineId
            ProductCode = validatedOrderLine.ProductCode
            Quantity = validatedOrderLine.Quantity
            LinePrice = linePrice
        }

        return (ProductLine pricedLine)
    }


// add the special commento line if needed
let addCommentLine pricingMethod lines = 
    match pricingMethod with
    | Standard ->
        lines //unchanged
    | Promotion (PromotionCode promoCode) ->
        let commentLine = 
            sprintf "Applied promotion %s" promoCode
            |> CommentLine
        List.append lines [commentLine]

let getLinePrice line = 
    match line with
    | ProductLine line ->
        line.LinePrice
    | CommentLine _ ->
        Price.unsafeCreate 0M

// add the special comment line
let priceOrder : PriceOrder = 
    fun getPricingFunction validatedOrder ->
        let getProductPrice = getPricingFunction validatedOrder.PricingMethod
        result{
            let! lines = 
                    validatedOrder.Lines
                    |> List.map (toPricedOrderLine getProductPrice)
                    |> Result.sequence
                    |> Result.map(fun lines -> 
                        lines |> addCommentLine validatedOrder.PricingMethod
                        )

            let! amountToBill = 
                lines
                |> List.map getLinePrice
                |> BillingAmount.sumPrices
                |> Result.mapError PricingError

            let pricedOrder : PricedOrder = {
                OrderId = validatedOrder.OrderId
                CustomerInfo = validatedOrder.CustomerInfo
                ShippingAddress = validatedOrder.ShippingAddress
                BillingAddress = validatedOrder.BillingAddress
                Lines = lines
                AmountToBill = amountToBill
                PricingMethod = validatedOrder.PricingMethod
            }
            return pricedOrder
        }


// --------------------------------------
// Shipping step 
// --------------------------------------

let (|UsLocalState|UsRemoteState|Internationl|) (address:Address) =
    if address.Country.value = "US" then
        match address.State.value with
        | "CA" | "OR" | "AZ" | "NV" ->  UsLocalState
        | _ -> UsRemoteState
    else
        Internationl


let calculateShippingCost : CalculateShippingCost =
    fun pricedOrder -> 
        match pricedOrder.ShippingAddress with
        | UsLocalState -> 5.0M
        | UsRemoteState -> 10.0M
        | Internationl -> 20.M
        |> Price.unsafeCreate


let addShippingInfoToOrder : AddShippingInfoToOrder = 
    fun calculateShippingcost pricedOrder ->
        // create the shipping info
        let shippingInfo = {
            ShippingMethod = Fedex24
            ShippingCost = calculateShippingcost pricedOrder
        }
        // add to the order
        {
            PricedOrder = pricedOrder
            ShippingInfo = shippingInfo
        }

let freeVipShipping : FreeVipShipping = 
    fun order ->
        let updatedShippingInfo =
            match order.PricedOrder.CustomerInfo.VipStatus with
            | Normal -> order.ShippingInfo // umtouched
            | VIP -> 
                {
                    order.ShippingInfo with
                        ShippingCost = Price.unsafeCreate 0.0M
                        ShippingMethod = Fedex24
                }
        {order with ShippingInfo = updatedShippingInfo}


// -------------------------------
// AcknowlegeOrder setp
// -------------------------------

let acknowledgeOrder : AcknowledgeOrder = 
    fun createAcknowledgmentLetter SendAcknowledgment PricedOrderWithShipping ->
        let pricedOrder = PricedOrderWithShipping.PricedOrder

        let letter = createAcknowledgmentLetter PricedOrderWithShipping
        let acknowledgment = {
            EmailAddress = pricedOrder.CustomerInfo.EmailAddress
            Letter = letter
        }

        // if the acknowledgement was successfully sent,
        // return the corresponding event, else return None
        match SendAcknowledgment acknowledgment with
        | Sent ->
            let event = {
                OrderId = pricedOrder.OrderId
                EmailAddress = pricedOrder.CustomerInfo.EmailAddress
            } 
            Some event
        | NotSent -> None


// -------------------------
// Create events 
// -------------------------

let makeShipmentLine (line:PricedOrderLine) : ShippableOrderLine option =
    match line with
    | ProductLine line -> 
        {
            ProductCode = line.ProductCode
            Quantity = line.Quantity
        } |> Some

    | CommentLine _ -> None

let createShippingEvent (placedOrder:PricedOrder) : ShippableOrderPlaced =
        {
            OrderId = placedOrder.OrderId
            ShippingAddress = placedOrder.ShippingAddress
            ShipmentLines = placedOrder.Lines |> List.choose makeShipmentLine
            // empty pdf
            Pdf = 
                {
                    Name = sprintf "Order%s.pdf" (placedOrder.OrderId.value)
                    Bytes    = [||]
                }
        }  

let createSBillingEvent (placedOrder:PricedOrder) : BillableOrderPlaced option = 
    if placedOrder.AmountToBill.value > 0M then
        let event : BillableOrderPlaced = 
            {
                OrderId = placedOrder.OrderId
                BillingAddress = placedOrder.BillingAddress
                AmountToBill = placedOrder.AmountToBill
            } 
        Some event
    else
        None

/// helper to convert an option into a List
let listOfOption = 
    function
    | Some x -> [x]
    | None -> []


let createEvents : CreateEvents = 
    fun pricedOrder acknowledgmentEeventOpt ->
        let acknowledgmentEvents = 
            acknowledgmentEeventOpt 
            |> Option.map PlaceOrderEvent.AcknowledgmentSent
            |> listOfOption
        let shippingEvents = 
            pricedOrder
            |> createShippingEvent
            |> PlaceOrderEvent.ShippableOrderPlaced
            |> List.singleton
        let billingEvents = 
            pricedOrder
            |> createSBillingEvent
            |> Option.map    PlaceOrderEvent.BillableOrderPlaced
            |> listOfOption
        [
            yield! acknowledgmentEvents
            yield! shippingEvents
            yield! billingEvents
        ]


// ---------------------------
// overall workflow
// ---------------------------
let placeOrder 
    checkProductExists // dependency
    checkAddressExists // dependency
    getProductPrice    // dependency
    calculateShippingCost // dependency
    createOrderAcknowledgmentLetter  // dependency
    sendOrderAcknowledgment // dependency
    : PlaceOrder =       // definition of function
    //failwith "not implemented"
    fun unvalidatedOrder -> 
        asyncResult {
            let! validatedOrder = 
                validateOrder
                    checkProductExists 
                    checkAddressExists 
                    unvalidatedOrder 
                |> AsyncResult.mapError (PlaceOrderError.Validation)
                //|> List.singleton

            let! pricedOrder = 
                priceOrder getProductPrice validatedOrder 
                |> AsyncResult.ofResult
                |> AsyncResult.mapError (PlaceOrderError.Pricing)
                //|> List.singleton

            let pricedOrderWithShipping = 
                pricedOrder 
                |> addShippingInfoToOrder calculateShippingCost
                |> freeVipShipping
            let acknowledgementOption = 
                acknowledgeOrder createOrderAcknowledgmentLetter sendOrderAcknowledgment pricedOrderWithShipping 
            
            let events = 
                createEvents pricedOrder acknowledgementOption 
            return events
            
        }

