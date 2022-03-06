module internal OrderTaking.PlaceOrder.Implementation

open OrderTaking.Common
open OrderTaking.PlaceOrder.InternalTypes
open System.Runtime.ExceptionServices
open System.ComponentModel.DataAnnotations
open Newtonsoft.Json.Schema

// ==================================================================
// Final implementation for the PlaceOrderWorkflow
// =================================================================


// ----------------------------------------------------
// validateOrder step
// ----------------------------------------------------


let toCustomerInfo (unvalidatedCustomerInfo : UnvalidatedCostumerInfo) =
    result {
        let! firstName = 
            unvalidatedCustomerInfo.FirstName
            |> String50.create "FirstName"
            |> Result.mapError ValidationError // covnert creation error into ValidationError

        let! lastName = 
            unvalidatedCustomerInfo.LastName
            |> String50.create "LastName"
            |> Result.mapError ValidationError // convert creation error into ValidationError

        let! emailAddress = 
            unvalidatedCustomerInfo.EmailAddress
            |> EmailAddress.create "EmailAddress"
            |> Result.mapError ValidationError // convert creation error into ValidationError

        let! vipStatus = 
            unvalidatedCustomerInfo.VipStatus 
            |> VipStatus.fromString "vipStatus"
            |> Result.mapError ValidationError // convert creation error into ValidationError

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
            |> Result.mapError ValidationError


        let! addressLine2 = 
            checkedAddress.AddressLine2
            |> String50.createOption "AddressLine2"
            |> Result.mapError ValidationError

        let! addressLine3 = 
            checkedAddress.AddressLine3
            |> String50.createOption "AddressLine3"
            |> Result.mapError ValidationError

        let! addressLine4 = 
            checkedAddress.AddressLine4
            |> String50.createOption "AddressLine4"
            |> Result.mapError ValidationError

        let! city = 
            checkedAddress.City
            |> String50.create "City"
            |> Result.mapError ValidationError

        let! zipCode = 
            checkedAddress.ZipCode
            |> ZipCode.create "ZipCode"
            |> Result.mapError ValidationError

        let! state = 
            checkedAddress.State
            |> UsStateCode.create "State"
            |> Result.mapError ValidationError

        let! country = 
            checkedAddress.Country
            |> String50.create "Country"
            |> Result.mapError ValidationError

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


let validateOrder : ValidateOrder = 
    fun checkproductCodeExists checkAddressExists unvalidatedOrder ->
        let toCheckedAddress = toCheckedAddress checkAddressExists
        asyncResult {
            let! orderId = 
                unvalidatedOrder.OrderId
                |> toOrderId
                |> AsyncResult.ofResult

            let! customerInfo = 
                unvalidatedOrder.CustomerInfo
                |> toCustomerInfo
                |> AsyncResult.ofResult

            let! checkedShippingAddres = 
                unvalidatedOrder.ShippingAddress 
                |> toCheckedAddress 

            let! shippingAddress = 
                checkedShippingAddres
                |> toAddress 
                |> AsyncResult.ofResult

            let! checkedBillingAddres = 
                unvalidatedOrder.BillingAddress 
                |> toCheckedAddress 

            let! billingAddress = 
                checkedBillingAddres
                |> toAddress
                |> AsyncResult.ofResult

            let! lines = 
                unvalidatedOrder.Lines
                |> List.map (toValidateOrderLine checkproductCodeExists)
                |> Result.sequence//convert a list of Results to a single Result //TODO: change this to applicative
                |> AsyncResult.ofResult


            let pricingMethod = 
                unvalidatedOrder.PromotionCode
                |> PricingModule.createPricingMethod

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


let addShippingInfoToOrder : AddShippingInfoToOrder = throwNotImplemented ()

let freeVipShipping : FreeVipShipping = throwNotImplemented ()

let acknowledgeOrder : AcknowledgeOrder = throwNotImplemented ()

let createEvents : CreateEvents = throwNotImplemented ()



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
                |> AsyncResult.mapError PlaceOrderError.Validation

            let! pricedOrder = 
                priceOrder getProductPrice validatedOrder 
                |> AsyncResult.ofResult
                |> AsyncResult.mapError PlaceOrderError.Pricing

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

