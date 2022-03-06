module OrderTaking.PlaceOrder.Api

open Newtonsoft.Json
open OrderTaking.Common
open OrderTaking.PlaceOrder.InternalTypes
open OrderTaking.PlaceOrder
open OrderTaking.PlaceOrder
open Newtonsoft.Json
open Newtonsoft.Json
open OrderTaking.PlaceOrder


type JsonString = string

//very simplified version!

type HttpRequest = {
    Action : string
    Uri: string
    Body : JsonString
}


type HttpResponse = {
    HttpStatusCode: int
    Body : JsonString
}


type PlaceOrderApi = HttpRequest -> Async<HttpResponse>

// =========================
// Implementation
// =========================


/// setup dummy dependencies

module internal Dependencies =

    let checkProductExists : CheckProductCodeExists =
        fun productCode -> true


    let checkAddressExists : CheckAddressExists  =
        fun unvalidatedAddress -> 
            let checkedAddress = CheckedAddress unvalidatedAddress
            AsyncResult.retn checkedAddress


    let getStandardPrices() : GetProductPrice =
        fun productCode -> Price.unsafeCreate 10M


    let getPromotionPrices (PromotionCode promotionCode) : TryGetProductPrice =
    
        let halfPricedPromotion : TryGetProductPrice = 
            fun productCode -> 
                if ProductCode.value productCode = "ONSALE" 
                    then Price.unsafeCreate 5M |> Some
                else None
            
        
        let quarterPricePromotion : TryGetProductPrice = 
            fun productCode -> 
            if ProductCode.value productCode = "ONSALE" then
                Price.unsafeCreate 2.5M |> Some
            else None

        let noPromotion : TryGetProductPrice =
            fun productCode -> None

        match promotionCode with
        | "HALF" -> halfPricedPromotion
        | "QUARTER" -> quarterPricePromotion
        | _ -> noPromotion


    let getPricingFunction : GetPricingFunction = 
        PricingModule.getPricingFunction getStandardPrices getPromotionPrices


    let createOrderAcknowledgmentLetter : CreateOrderAcknowledgmentLetter =
        fun pricedOrder -> HtmlString "some text"

    let sendOrderAcknowledgment : SendOrderAcknowledgment =
        fun orderAcknowledgment -> Sent

    let calculateShippingCost : CalculateShippingCost = throwNotImplemented ()


// ------------------------------------
// wordflow 
// ----------------------------------


/// this function converts the workflow output into a httpResponse
let workflowResulttoHttpResponse result = 
    match result with
    | Ok events ->
        // turn domain events into dtso 
        let dtos = 
            events 
            |> List.map PlaceOrderEventDto.fromDomain
            //|> List.toArray //arrays are json firendly

        let json = JsonConvert.SerializeObject dtos
        {HttpStatusCode = 200; Body= json}
        
    | Error err ->
        let dto = err |> PlaceOrderErrorDto.fromDomain
        let json = JsonConvert.SerializeObject dto
        {HttpStatusCode = 401 ; Body = json}
        
open Dependencies

let placeOrderApi : PlaceOrderApi = 
    fun request ->
        // complete serialization pipeline

        let orderForm = JsonConvert.DeserializeObject<OrderFormDto> request.Body

        let unvalidatedOrder = orderForm |> OrderFormDto.toUnvalidatedOrder

        let workflow = 
            Implementation.placeOrder 
                      checkProductExists // dependency
                      checkAddressExists // dependency
                      getPricingFunction // dependency
                      calculateShippingCost // dependency
                      createOrderAcknowledgmentLetter  // dependency
                      sendOrderAcknowledgment // dependency
            
        let asyncResult = workflow unvalidatedOrder

        asyncResult
        |> Async.map workflowResulttoHttpResponse