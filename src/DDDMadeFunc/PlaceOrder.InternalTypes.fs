module internal OrderTaking.PlaceOrder.InternalTypes

open OrderTaking.Common
open OrderTaking.PlaceOrder


// =======================================================
// Define each step in the PlaceOrder workflow using internal types
//========================================================


// ---------------------------
// Validation step
// ---------------------------

// Product validation
type CheckProductCodeExists = ProductCode -> bool

// Address validation
type AddressValidationError = 
    | InvalidFormat 
    | AddressNotFound

type PricingMethod =
    | Stantad
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
    | PorudctLine of PricedOrderProductLine
    | ComentLine of string


type PricedOrder =  {
    OrderId : OrderId
    CustomerInfo : CustomerInfo
    ShippingAddress : Address
    BillingAddress : Address
    AmountToBill : BillingAmount
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


// -------------------------
// VIP shipping 
// ------------------------

//TODO: check the effects of removing this
type FreeShipping = PricedOrderWithShippingMethod -> PricedOrderWithShippingMethod



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
    CreateOrderAcknowledgmentLetter -> SendOrderAcknowledgment -> PricedOrderWithShippingMethod -> OrderAcknowledgment option


// ------------------
// Create events
// ------------------

type CreateEvents = PricedOrder -> OrderAknowledgmentSent option -> PlacedOrderEvent list

