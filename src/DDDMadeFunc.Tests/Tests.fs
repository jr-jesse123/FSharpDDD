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

[<Property(DisplayName = "Valides" + nameof(CustomerInfoDto) + "can be to Domain and converted back",
    Arbitrary = [|typeof<validCustomerInfoDtoArb>|])>]
let ``My test`` x =
    
    printfn "%A" x

    result {
        let! out = CustomerInfoDto.toCustumerInfo x
        let nvDto = CustomerInfoDto.fromCustomerInfo out
        return nvDto = x
    }
    |> function |Ok v -> v
    
    
    

    
    

    //let prop (Superint x) = 
    //    //printfn "%A" x
    //    x > 20
    
    //Check.QuickThrowOnFailure prop
