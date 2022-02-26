namespace OrderTaking.PlaceOrder

open System.Collections.Generic

open OrderTaking
open OrderTaking.Common
open OrderTaking.PlaceOrder.InternalTypes

// ==================================
// DTOs for PlaceOrder workflow
// ==================================

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

/// Functions for coverting between the DTO and corresponding domain object
// TODO: thing about changin the names to toDomain and fromDoamin standards
// TODO: TEST automapper implementation
module internal CustomerInfoDto = 

    let toUnvalidatedCustomerInfo (dto:CustomerInfoDto) 
