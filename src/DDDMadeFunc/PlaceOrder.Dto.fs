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
    let defaultIfNone defaultValue  = 
        //match opt with
        //| Some v -> v
        //| None -> defaultValue
        Option.defaultValue defaultValue 

 

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
    
    let retn x = Ok x


    ///Monadic
    let apply fResult xResult = 
        match fResult, xResult with
        | Ok f , Ok x -> Ok (f x)
        | Ok _ , Error err //-> Error err
        | Error err, Ok _ -> Error err
        | Error e1, Error e2 ->  Error e1


    let isOk = function |Ok _ -> true |Error _ -> false

    

[<AutoOpen>]
module ResultComputationExpression  = 

    type ResultBuilder() =

        member _.MergeSources(x,y) = 
            ()

        member _.Bind(x,f) = 
            match x with 
            | Ok x -> f x
            | Error err -> Error err


        member _.Return x = Ok x

    let result = new ResultBuilder()





type CustomerInfoDto = {
    FirstName : string
    LastName : string
    EmailAddress : string
    VipStatus : string        
}   

open AutoMapper
/// Functions for coverting between the DTO and corresponding domain object
// TODO: thing about changin the names to toDomain and fromDoamin standards
// TODO: TEST automapper implementation
module internal CustomerInfoDto = 
    
    //type CustomerInfoDtoProfile() as this =
    //    inherit Profile()
    //    do 
    //        this
    //            .CreateMap<CustomerInfoDto,UnvalidatedCostumerInfo>()
    //            .ReverseMap() 
    //        |> ignore
        
    let mapConfig = new MapperConfiguration(
        fun cfg -> 
            cfg.CreateMap<CustomerInfoDto,UnvalidatedCostumerInfo>()
               .ReverseMap()
            |> ignore)
    
    let mapper = mapConfig.CreateMapper()

    do mapConfig.AssertConfigurationIsValid();
    
    let toUnvalidatedCustomerInfo2 (dto:CustomerInfoDto) : UnvalidatedCostumerInfo =
        mapper.Map<CustomerInfoDto,UnvalidatedCostumerInfo>(dto)
    

    //let toUnvalidatedCustomerInfo (dto:CustomerInfoDto) : UnvalidatedCostumerInfo =
    //    {
    //        FirstName = dto.FirstName
    //        LastName = dto.LastName
    //        EmailAddress = dto.EmailAddress
    //        VipStatus = dto.VipStatus
    //    }
    
    //TODO: USE APPLICATIVE VALIDATION
    let toCustomerInfo (dto:CustomerInfoDto)  =
        result {
               let! first = dto.FirstName |> String50.create "FirstName"
               //and! last = dto.LastName |> String50.create "LastName"
               //and! email = dto.EmailAddress |> EmailAddress.create "EmailAddress"
               //and! vipStatus = dto.VipStatus |> VipStatus.fromString "VipStatus"
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
            VipStatus = customerInfo.VipStatus  |> VipStatus.toString //another way of achiving the same thing
        }


type CustomerInfoDto with
    member this.ToCustomerInfo () = CustomerInfoDto.toCustomerInfo this


//===========================================
// DTO for Address
// ===========================================

type AddressDto = {
    AddressLine1 : string
    AddressLine2 : string
    AddressLine3 : string
    AddressLine4 : string
    City : string
    ZipCode : string
    State : string
    Country : string
}



/// Functions for converting between the DTO and corresponding domain object

module internal AddressDto =
    
    /// Convert the DTO into a UnvalidatedAddress
    //TODO: TESTAR AUTOMAPPER
    let toUnvalidatedAddress (dto: AddressDto) : UnvalidatedAddress =
        {
            AddressLine1 = dto.AddressLine1
            AddressLine2 = dto.AddressLine2
            AddressLine3 = dto.AddressLine3
            AddressLine4 = dto.AddressLine4
            City = dto.City
            ZipCode = dto.ZipCode
            State = dto.State
            Country = dto.Country
        }


    //TODO: change this to applicative validationz\sdreoti45t/~ityu86
    let toAddress (dto: AddressDto) (*: Result<Address,string>*) =
        result{
            let! addressLine1 = dto.AddressLine1 |> String50.create "AddresLine1"
            let! addressLine2 = dto.AddressLine2 |> String50.createOption "AddresLine2"
            let! addressLine3 = dto.AddressLine3 |> String50.createOption "AddresLine3"
            let! addressLine4 = dto.AddressLine4 |> String50.createOption "AddresLine4"
                       
            let! city = dto.City |> String50.create "City"
            let! zipCode = dto.ZipCode |> ZipCode.create "ZipCode"
            let! state = dto.State |> UsStateCode.create "State"
            let! country = dto.Country |> String50.create "Country"

            // combine the components to create the domain object
            let address : Common.Address = {
                AddressLine1 = addressLine1 
                AddressLine2 = addressLine2 
                AddressLine3 = addressLine3 
                AddressLine4 = addressLine4 
                City = city
                ZipCode = zipCode
                State = state
                Country = country
                }
            return address
        }

        //TODO: try a value CE
    let fromAddress (domainObj:Address) : AddressDto =
        {
            AddressLine1 = domainObj.AddressLine1 |> String50.value
            AddressLine2 = domainObj.AddressLine2 |> Option.map String50.value |> defaultIfNone null
            AddressLine3 = domainObj.AddressLine3 |> Option.map String50.value |> defaultIfNone null
            AddressLine4 = domainObj.AddressLine4 |> Option.map String50.value |> defaultIfNone null
            City = domainObj.City |> String50.value
            ZipCode = domainObj.ZipCode |> ZipCode.value
            State  = domainObj.ZipCode |> ZipCode.value
            Country = domainObj.Country |> String50.value
        }
     
     

//===============================================
// DTOs for OrderLines
//===============================================

/// From the order form used as input
type OrderFormLineDto = {
    OrderLineId : string
    ProductCode : string
    Quantity : decimal
}

/// Functions relating to the OrderLine DTOs
module internal OrderLineDto = 
    
    /// Convert the OrderFormline into a UnvalidatedOrderLine
    let toUnvalidatedOrderLine (dto:OrderFormLineDto) : UnvalidatedOrderLine = 
        {
            OrderLineId = dto.OrderLineId
            ProductCode = dto.ProductCode
            Quantity = dto.Quantity
        }



//===============================================
// DTOs for PricedOrderLines
//===============================================

/// Used in the output of the workflow
type PricedOrderLineDto = {
    OrderLineId : string
    ProductCode : string
    Quantity : decimal
    LinePrice : decimal
    Comment : string
}

module internal PricedOrderLineDto = 
    
    
    let fromDomain (domainObj:PricedOrderLine) : PricedOrderLineDto =
        match domainObj with
        | ProductLine line ->
            {
                OrderLineId = line.OrderLineId.value
                ProductCode = line.ProductCode.value
                Quantity = line.Quantity.value
                LinePrice = line.LinePrice.value
                Comment = ""
            }
        | CommentLine comment ->
            {
                OrderLineId = null
                ProductCode = null
                Quantity = 0M
                LinePrice = 0M
                Comment = comment
            }



//===============================================
// DTO for OrderForm
//===============================================
type OrderFormDto =  {
    OrderId : string
    CustomerInfo : CustomerInfoDto
    ShippingAddress : AddressDto
    BillingAddress : AddressDto
    Lines : OrderFormLineDto list
    PromotionCode : string
}


/// Functions relating to the Order DTOs
module internal OrderFormDto = 
    
    let toUnvalidatedOrder (dto:OrderFormDto) : UnvalidatedOrder =
        {
            OrderId = dto.OrderId
            CustomerInfo = dto.CustomerInfo |> CustomerInfoDto.toUnvalidatedCustomerInfo2
            ShippingAddress = dto.ShippingAddress |> AddressDto.toUnvalidatedAddress
            BillingAddress = dto.BillingAddress |> AddressDto.toUnvalidatedAddress
            Lines = dto.Lines |> List.map  OrderLineDto.toUnvalidatedOrderLine
            PromotionCode = dto.PromotionCode

        }