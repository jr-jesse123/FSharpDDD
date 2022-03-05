module internal OrderTaking.PlaceOrder.Implementation

open OrderTaking.Common
open OrderTaking.PlaceOrder.InternalTypes
open System.Runtime.ExceptionServices
open System.ComponentModel.DataAnnotations

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

let toOrderId orderId = 
    orderId
    |> OrderId.create "OrderId"
    |> Result.mapError ValidationError


/// Helper function for validateOrder 
let toOrderLidId orderLineId = 
    orderLineId
    |> OrderLineId.create "OrderLineId"
    |> Result.mapError ValidationError


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

