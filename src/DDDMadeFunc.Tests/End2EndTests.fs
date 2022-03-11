module End2EndTests

open OrderTaking.PlaceOrder
open Xunit
open Swensen.Unquote
open OrderTaking.PlaceOrder
open OrderTaking.PlaceOrder.Api
open Newtonsoft.Json
open FsCheck
open OrderTaking.Common

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
    OrderLineId = System.Guid.NewGuid().ToString().Substring(0,10)
    ProductCode = "W1234"
    Quantity = 1.0M
}

let unvalidatedFormDto : OrderFormDto = {
    OrderId = "orderid"
    CustomerInfo = customerInfoDto
    ShippingAddress = addressDto
    BillingAddress = addressDto
    Lines = [line]
    PromotionCode = "code"
}

let request : Api.HttpRequest = {
    Action = ""
    Uri = ""
    Body = JsonConvert.SerializeObject unvalidatedFormDto
}


[<Fact>]
let ``PlaceOrder Workflow should succed with valid input`` () =

    let output =
        Api.placeOrderApi request 
        |> Async.RunSynchronously  
        
    printfn "%s" output.Body
    
    test <@ output.HttpStatusCode = 200 @>




let invalidcustomerInfoDto : CustomerInfoDto = {
       FirstName = "Jesséxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
       LastName = "Juniorxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
       EmailAddress = "junior.jesse@gmail.com"
       VipStatus = "Normal"   
}

let invalidaddressDto : AddressDto = {
       AddressLine1 = "av flamboyant lote 22 bloco c"
       AddressLine2 = "vicnete pires lote 24"
       AddressLine3 = ""
       AddressLine4 = ""
       City = "Brasília"
       ZipCode = "70917000"
       State = "DF"
       Country = "Brazil"
}

let invalidline : OrderFormLineDto = {
    OrderLineId = System.Guid.NewGuid().ToString().Substring(0,10)
    ProductCode = "W1234"
    Quantity = 1.0M
}

let invalidunvalidatedFormDto : OrderFormDto = {
    OrderId = "orderid"
    CustomerInfo = invalidcustomerInfoDto
    ShippingAddress = invalidaddressDto
    BillingAddress = invalidaddressDto
    Lines = [invalidline]
    PromotionCode = "code"
}

let invalidrequest : Api.HttpRequest = {
    Action = ""
    Uri = ""
    Body = JsonConvert.SerializeObject invalidunvalidatedFormDto
}


[<Fact>]
let ``PlaceOrder Workflow should fail with Ivalid input, showing all the error messages`` () =

    let output =
        Api.placeOrderApi invalidrequest
        |> Async.RunSynchronously  
        
    printfn "%s" output.Body
    
    test <@ output.HttpStatusCode = 401 @>

    //TODO: REDUCE THIS FUNCTIONS
    let shouldcontain words (setence:string) = 
        words 
        |> Seq.map (fun (x:string) -> setence.Contains(x))
        |> Seq.reduce (&&)

        

    test <@ output.Body |> shouldcontain ["FirstName" ; "LastName"]  @>


let failOnError = 
    function
    | Ok _ -> ()
    | Error _ -> failwith "Process faild"

