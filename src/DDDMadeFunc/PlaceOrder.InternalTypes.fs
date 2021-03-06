module internal OrderTaking.PlaceOrder.InternalTypes

open OrderTaking.Common
open OrderTaking.PlaceOrder
open OrderTaking.PlaceOrder


// =======================================================
// Define each step in the PlaceOrder workflow using internal types
//========================================================


// ---------------------------
// Validation step
// ---------------------------

// Product validation
type CheckProductCodeExists = ProductCode -> bool


// these two types are internal in the book but I couldn?t compile 
type CheckedAddress = CheckedAddress of UnvalidatedAddress

type AddressValidationError = 
    | InvalidFormat 
    | AddressNotFound


// Address validation
type CheckAddressExists = UnvalidatedAddress -> AsyncResult<CheckedAddress,AddressValidationError>


// --------------------------------------
// Validated Order 
// -------------------------------------


type PricingMethod =
    | Standard
    | Promotion of PromotionCode

type ValidatedOrderLine = {
    OrderLineId : OrderLineId
    ProductCode : ProductCode
    Quantity : OrderQuantity
}

type ValidatedOrder = {
    OrderId : OrderId
    CustomerInfo : CustomerInfo
    ShippingAddress : Address
    BillingAddress : Address
    Lines : ValidatedOrderLine list 
    PricingMethod : PricingMethod
}

type ValidateOrder = 
    CheckProductCodeExists -> CheckAddressExists -> UnvalidatedOrder -> AsyncResult<ValidatedOrder, ValidationError>
// ----------------------------
// Pricing step
// ----------------------------

type GetProductPrice = ProductCode -> Price

type TryGetProductPrice = ProductCode -> Price option

type GetPricingFunction = PricingMethod -> GetProductPrice

type GetStandardPrices = unit -> GetProductPrice

type GetPromotionPrices = PromotionCode -> TryGetProductPrice

/// priced state
type PricedOrderProductLine = {
    OrderLineId : OrderLineId
    ProductCode : ProductCode
    Quantity : OrderQuantity
    LinePrice : Price
}

type PricedOrderLine = 
    | ProductLine of PricedOrderProductLine
    | CommentLine of string


type PricedOrder =  {
    OrderId : OrderId
    CustomerInfo : CustomerInfo
    ShippingAddress : Address
    BillingAddress : Address
    Lines : PricedOrderLine list
    AmountToBill : BillingAmount
    PricingMethod : PricingMethod
}


//TODO: check the effects of removing this
type PriceOrder = 
    GetPricingFunction -> //dependency
        ValidatedOrder -> //input
        Result<PricedOrder, PricingError> //output



// --------------------------
// Shipping
// ---------------------------

type ShippingMethod = 
    | PostalService
    | Fedex24
    | Fedex48
    | Ups48


type ShippingInfo = {
    ShippingMethod : ShippingMethod
    ShippingCost : Price
}

type PricedOrderWithShippingMethod = {
    ShippingInfo : ShippingInfo
    PricedOrder : PricedOrder
}

type CalculateShippingCost = PricedOrder -> Price

type AddShippingInfoToOrder = 
    CalculateShippingCost -> PricedOrder -> PricedOrderWithShippingMethod


// -------------------------
// VIP shipping 
// ------------------------

//TODO: check the effects of removing this
type FreeVipShipping = PricedOrderWithShippingMethod -> PricedOrderWithShippingMethod



// ------------------------------------------
// Send OrderAcknowledgment 
// -----------------------------------------

type HtmlString = HtmlString of string


type OrderAcknowledgment = {
    EmailAddress : EmailAddress
    Letter : HtmlString
}

type CreateOrderAcknowledgmentLetter=
    PricedOrderWithShippingMethod -> HtmlString


/// Send the order acknowledgement to the customer
/// Note that this does NOT generate an Result-type error (at least not in this workflow)
/// because on failure we will continue anyway.
/// On success, we will generate a OrderAcknowledgmentSent event,
/// but on failure we won't.
//TODO: check what about would be to use result and not failling
type SendResult = Sent | NotSent

type SendOrderAcknowledgment = 
    OrderAcknowledgment -> SendResult


type AcknowledgeOrder = 
    CreateOrderAcknowledgmentLetter -> SendOrderAcknowledgment -> PricedOrderWithShippingMethod -> OrderAcknowledgmentSent option


// ------------------
// Create events
// ------------------

type CreateEvents = PricedOrder -> OrderAcknowledgmentSent option -> PlaceOrderEvent list

