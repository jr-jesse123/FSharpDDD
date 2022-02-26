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
    AddresLine1 : string
    AddresLine2 : string
    AddresLine3 : string
    AddresLine4 : string
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
    CostumerInfo : UnvalidatedCostumerInfo
    ShippingAddress : UnvalidatedAddress
    BillingAddres : UnvalidatedAddress
    Lines : UnvalidatedOrderLine list
    PromotionCode : string
}


// ---------------------------------------
// outputs from the workflow (sucess case)

///Event will be created if the Aknowledgment was succesfully posted
type OrderAknowledgmentSent = {
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
    BillingAddres : Address
    AmountToBill : BillingAmount //TODO: think about turn this into a property
    Pdf : PdfAttachment
}

type BillableOrderPlaced = {
    OrderId : OrderId
    BillingAddres : Address
    AmountToBill : BillingAmount  //TODO: think about turn this into a property
}

/// The possible events resulting from the PlaceOrder workflow
/// Not all event will occur, depending on the logic of the workflow
type PlacedOrderEvent = 
    | ShippableOrderPlaced of ShippableOrderPlaced
    | BillableOrderPlaced of BillableOrderPlaced
    | AcknowledmentSent of OrderAknowledgmentSent

// --------------------------------------------------------
// error outputs 

/// All the thisn that can go wrong in this workflow
type ValidationError = ValidationError of string
type PricingError = PricinError of string

type  ServiceInfo = {
    Name: string
    EndPoint : System.Uri
}

type RemoteServiceError = {
    Service : ServiceInfo
    Exception : System.Exception
}

type PlacedOrderError = 
    | Validation of ValidationError
    | Pricing of PricingError
    | RemoteService of RemoteServiceError


// --------------------------------------
// hte workflow itself

type PlaceOrder = UnvalidatedOrder -> Result<PlacedOrderEvent list, PlacedOrderError> //TODO ALTERAR RESULTADO PARA ASYNC RESULT