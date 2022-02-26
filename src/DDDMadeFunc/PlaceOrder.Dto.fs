namespace OrderTaking.PlaceOrder

open System.Collections.Generic

open OrderTaking
open OrderTaking.Common
open OrderTaking.PlaceOrder.InternalTypes
open System
open System.Linq

// ==================================
// DTOs for PlaceOrder workflow
// ==================================

[<AutoOpen>]
module internal Utils = 
    
    //TODO: think about changin this to the defaulvalue function
    let defaultIfNone defaultValue opt = 
        //match opt with
        //| Some v -> v
        //| None -> defaultValue
        Option.defaultWith defaultValue opt


type CustomerInfoDto = {
    FirstName : string
    LastName : string
    EmailAddress : string
    VipStatus : string        
}    

[<RequireQualifiedAccess>]
module Result =
    //TODO: remove this built in function wrotten for studies propourses
    let map f result = 
        match result with
        | Ok success -> Ok (f success)
        | Error err -> Error err 
    
    //TODO: remove this built in function wrotten for studies propourses
    let mapError f result = 
        match result with
        | Ok success -> Ok success
        | Error err -> Error (f err)

    //TODO: remove this built in function wrotten for studies propourses
    let bind f result = 
        match result with
        | Ok success -> f success 
        | Error err -> Error err
    ()    

[<AutoOpen>]
module ResultComputationExpression  = 

    type ResultBuilder() =
        member _.Bind(x,f) = 
            match x with 
            | Ok x -> f x
            | Error err -> Error err


        member _.Return x = Ok x

    let result = new ResultBuilder()

/// Functions for coverting between the DTO and corresponding domain object
// TODO: thing about changin the names to toDomain and fromDoamin standards
// TODO: TEST automapper implementation
module internal CustomerInfoDto = 

    let toUnvalidatedCustomerInfo (dto:CustomerInfoDto) : UnvalidatedCostumerInfo =
        {
            FirstName = dto.FirstName
            LastName = dto.LastName
            EmailAddress = dto.EmailAddress
            VipStatus = dto.VipStatus
        }

    let toCustumerInfo (dto:CustomerInfoDto)  =
        result {
               let! first = dto.FirstName |> String50.create "FirstName"
               let! last = dto.LastName |> String50.create "LastName"
               let! email = dto.EmailAddress |> EmailAddress.create "EmailAddress"
               let! vipStatus = dto.VipStatus |> VipStatus.fromString "VipStatus"
               let name  = {FirstName = first; LastName = last}
               let info = {Name = name ; EmailAddress = email; VipStatus = vipStatus}
               return info
            }

    /// Covnert a CustomerInfo object into the corresponding DTO
    //TODO: create and string50 CE for praticing
    let fromCustomerInfo (customerInfo:CustomerInfo) =
        {
            FirstName = customerInfo.Name.FirstName |> String50.value
            LastName = customerInfo.Name.LastName |> String50.value
            EmailAddress = customerInfo.EmailAddress |> EmailAddress.value
            VipStatus = customerInfo.VipStatus.value //another way of achiving the same thing
        }


        



