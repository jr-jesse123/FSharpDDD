module End2EndTests

open OrderTaking.PlaceOrder
open Xunit
open Swensen.Unquote
open OrderTaking.PlaceOrder
open OrderTaking.PlaceOrder.Api
open Newtonsoft.Json

let customerInfoDto : CustomerInfoDto = {
       FirstName = "Jessé"
       LastName = "Junior"
       EmailAddress = "junior.jesse@gmail.com"
       VipStatus = "Normal"   
}

let addressDto : AddressDto = {
       AddressLine1 = "av flamboyant lote 22 bloco c"
       AddressLine2 = "vicnete pires lote 24"
       AddressLine3 = ""
       AddressLine4 = ""
       City = "Brasília"
       ZipCode = "70917000"
       State = "DF"
       Country = "Brazil"
}

let line : OrderFormLineDto = {
    OrderLineId = "orderlineId"
    ProductCode = "product1"
    Quantity = 1
}

let unvalidatedFormDto : OrderFormDto = {
    OrderId = "orderid"
    CustomerInfo = customerInfoDto
    ShippingAddress = addressDto
    BillingAddress = addressDto
    Lines = [line]
    PromotionCode = "code"
}

let request : OrderTaking.PlaceOrder.Api.HttpRequest = {
    Action = ""
    Uri = ""
    Body = JsonConvert.SerializeObject unvalidatedFormDto
}


[<Fact>]
let teste1 () =

    let output =
        Api.placeOrderApi request 
        |> Async.RunSynchronously  
           
    

    test <@ output.HttpStatusCode = 200 @>