module CostumerInfoDtoTest 

open OrderTaking.Common
open OrderTaking.PlaceOrder
open FsCheck
open FsCheck.Xunit

let string50Gen =
    Arb.generate<NonEmptyString> 
    |> Gen.map (fun x -> x.Get)
    |> Gen.filter (fun (x:string) ->  x.Length < 50 )


type validCustomerInfoDtoArb = 
    static member CustomerInfoDto() =
        gen {
            let! fistName = string50Gen
            let! lastName = string50Gen
            let! mail = Arb.Default.MailAddress().Generator
            let! vipStatus = Arb.generate<VipStatus> |> Gen.map VipStatus.toString
            let out : CustomerInfoDto = 
                {FirstName = fistName; LastName = lastName; EmailAddress = mail.Address ; VipStatus = vipStatus}
            return  out
        }
        |> Arb.fromGen


[<Property(Arbitrary = [|typeof<validCustomerInfoDtoArb>|])>]
let ``Valide CustomerInfoDto can be converted to Domain and back and are still the same"`` customerDto =
    
    result {
        let! out = CustomerInfoDto.toCustomerInfo customerDto
        let nvDto = CustomerInfoDto.fromCustomerInfo out
        return nvDto = customerDto
    }
    |> function |Ok v -> v
    


[<Property>]
let ``InValide CustomerInfoDto can be converted to Domain and back and are still the same"`` customerDto =
    
    CustomerInfoDto.toCustomerInfo customerDto
    //|> fun x -> printfn "%A" x ; x
    |>  Result.isOk |> not



[<Property>]
let ``CostumerInfoDtocan be converted to InValidatedCustomerInfoDto and back and are still the same using Automapper"`` 
    customerDto =
    
    let out = CustomerInfoDto.toUnvalidatedCustomerInfo2 customerDto
    
    out.FirstName = customerDto.FirstName && out.LastName = customerDto.LastName 
    && out.EmailAddress = customerDto.EmailAddress && out.VipStatus = customerDto.VipStatus

    
    


    