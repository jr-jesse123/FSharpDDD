module internal OrderTaking.PlaceOrder.Implementation

open OrderTaking.Common
open OrderTaking.PlaceOrder.InternalTypes

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
let toAddress (CheckedAddress checkedAddress) : Address =
    failwith "não implementado"

let toOrderId orderId = 
    orderId
    |> OrderId.create "OrderId"
    |> Result.mapError ValidationError


/// Helper function for validateOrder 
let toOrderLidId orderId = 
    failwith "not implemented"


/// Helper function for validateOrder 

let validateOrder : ValidateOrder = failwith ""


let priceOrder : PriceOrder = throwNotImplemented ()


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

