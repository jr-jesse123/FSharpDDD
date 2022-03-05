namespace OrderTaking.PlaceOrder

open OrderTaking.Common

// ==================================
// This file contains the definitions of PUBLIC types (exposed at the boundary of the bounded context)
// related to the PlaceOrder workflow 
// ==================================


// ==================================
// PlaceOrder workflow
// ==================================

// ------------------------------------
// inputs to the workflow

type UnvalidatedCostumerInfo = {
    FirstName: string
    LastName: string
    EmailAddress : string
    VipStatus : string
}


type UnvalidatedAddress = {
    AddressLine1 : string
    AddressLine2 : string
    AddressLine3 : string
    AddressLine4 : string
    City: string
    ZipCode: string
    State: string
    Country: string
}

type UnvalidatedOrderLine = {
    OrderLineId : string
    ProductCode : string
    Quantity : decimal
}

type UnvalidatedOrder = {
    OrderId : string
    CustomerInfo : UnvalidatedCostumerInfo
    ShippingAddress : UnvalidatedAddress
    BillingAddress : UnvalidatedAddress
    Lines : UnvalidatedOrderLine list
    PromotionCode : string
}


// ---------------------------------------
// outputs from the workflow (sucess case)

///Event will be created if the Aknowledgment was succesfully posted
type OrderAcknowledgmentSent = {
    OrderId : OrderId
    EmailAddress : EmailAddress
}


type PricedOrderLine = {
    OrderLineId : OrderLineId
    ProductCode : ProductCode
    Quantity : OrderQuantity
    LinePrice : Price
}

type PricedOrder = {
    OrderId : OrderId
    CustomerInfo : CustomerInfo
    ShippingAddress : Address
    BillingAddres : Address
    Amounttobill : BillingAmount //TODO: think about turning this into a property
    Lines : PricedOrderLine list
}

/// Event to send to shipping context
type OrderPlaced = PricedOrder 


type ShippableOrderLine = {
    ProductCode : ProductCode
    Quantity : OrderQuantity
}

type ShippableOrderPlaced = {
    OrderId : OrderId
    ShippingAddress : Address
    ShipmentLines :  ShippableOrderLine list //TODO: think about turn this into a property
    Pdf : PdfAttachment
}

type BillableOrderPlaced = {
    OrderId : OrderId
    BillingAddress : Address
    AmountToBill : BillingAmount  //TODO: think about turn this into a property
}

/// The possible events resulting from the PlaceOrder workflow
/// Not all event will occur, depending on the logic of the workflow
type PlaceOrderEvent = 
    | ShippableOrderPlaced of ShippableOrderPlaced
    | BillableOrderPlaced of BillableOrderPlaced
    | AcknowledgmentSent of OrderAcknowledgmentSent

// --------------------------------------------------------
// error outputs 

/// All the things that can go wrong in this workflow
type ValidationError = ValidationError of string

type PricingError = PricingError of string

type  ServiceInfo = {
    Name: string
    EndPoint : System.Uri
}

type RemoteServiceError = {
    Service : ServiceInfo
    Exception : System.Exception
}

type PlaceOrderError = 
    | Validation of ValidationError
    | Pricing of PricingError
    | RemoteService of RemoteServiceError


// --------------------------------------
// hte workflow itself

type PlaceOrder = UnvalidatedOrder -> AsyncResult<PlaceOrderEvent list, PlaceOrderError> //TODO ALTERAR RESULTADO PARA ASYNC RESULT



