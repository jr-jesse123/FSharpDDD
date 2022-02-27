namespace OrderTaking.Common
open System

//TODO: move this helper functions to another file where it doesn´t colide with domain types 
module ConstrainedTypes = 
    let (|NullOrEmpty|_|)  str = 
        if String.IsNullOrEmpty str then Some str else None
    let (|TooLong|_|) maxLen (str: string) =
        if str.Length > maxLen then Some str else None
   
    let (|StartsWith|_|) (txtToBegin: string) (txtToCheck: string) = 
        if txtToCheck.StartsWith(txtToBegin) then Some txtToCheck else None

    /// Create a constrained string using the constructor provided if the input isnot too big or null/empty
    let createString fieldName ctor maxLen str = 
        // if String.IsNullOrEmpty str then
        //     let msg = sprintf "%s must not be null or empty" fieldName
        //     Error msg
        // elif str.Length > maxLen then
        //     let msg = sprintf "%s must not be more than %i chars" fieldName maxLen
        //     Error msg
        //  else 
        //     ctor str |> Ok   
        match str with
        | NullOrEmpty _ -> 
            sprintf "%s must not be null or empty" fieldName
            |>  Error 
        | TooLong maxLen _ ->    
            sprintf "%s must not be more than %i chars" fieldName maxLen
            |> Error 
        | _ -> Ok (ctor str)

    /// Create a optional constrained string using the constructor provided
    /// Return None if input is null, empty. 
    /// Return error if length > maxLen
    /// Return Some if the input is valid
    let createStringOption fieldName ctor maxLen str = 
        match str with
        | NullOrEmpty _ -> Ok None
        | TooLong maxLen  _ -> 
            sprintf "%s must not be more than %i chars" fieldName maxLen
            |> Error 
        | _ -> ctor str |> Some |> Ok

    //let create/ Create a constrained integer using the constructor provided
    /// Return Error if input is less than minVal or more than maxVal        
    let createInt fieldName ctor minVal maxVal i =
        if i < minVal then
            sprintf "%s Must not be less than %i" fieldName minVal
            |> Error
        elif i > maxVal then
            sprintf "%s: must not be greater than %i" fieldName maxVal
            |> Error
        else   Ok (ctor i)


    /// Create a constrained decimal using the constructor provided
    /// Return Error if input is less than minVal or more than maxVal
    let createDecimal fieldName ctor minVal maxVal i =
        if i < minVal then
            sprintf "%s Must not be less than %M" fieldName minVal
            |> Error
        elif i > maxVal then
            sprintf "%s: must not be greater than %M" fieldName maxVal
            |> Error
        else   Ok (ctor i)

    open System.Text.RegularExpressions
    let (|IsMatch|_|) pattern str =
        if Regex.IsMatch (str,pattern) then Some str else None

    /// Create a constrained string using the constructor provided
    /// Return Error if input is null. empty, or does not match the regex pattern
    let createLike fieldName ctor pattern str =
        match str with
        | NullOrEmpty _ -> 
            sprintf "%s: Must not be null or empty" fieldName |> Error
        | IsMatch pattern _ -> 
            Ok (ctor str)
        | _ -> 
            sprintf "%s: '%s' must match the pattern '%s" fieldName str pattern
            |> Error

// =====================================================================
// Simple types and constrained types related to the  orderTaking domain
// =====================================================================

/// Constrained to be 50 chars or less, not null
type String50 = private String50 of string

/// An email addres
type EmailAddress = private EmailAddress of string

/// Costumer´s VIP status
type VipStatus = 
    | Normal
    | VIP

/// A Zip code
type ZipCode = private ZipCode of String

/// A US 2 letter state code
type UsStateCode = private UsStateCode of string

//TODO: think about change the inner OrderId to use Guid 
/// An Id for Orders. Constrained to be a non-empty string < 10
type OrderId = private OrderId of string


//TODO: think about change the inner OrderId to use Guid 
//TODO: this type is using methods instead of module functions because I want to know if there
//will any diference on the workflow composition but should be removed from here so it does not 
//disturb the domain vision
/// An Id for OrderLines. Constrained to be a non-empty string < 10 chars
type OrderLineId = private OrderLineId of string


/// The codes for Widgets start with a "W" and then four digits
type WidgetCode = private WidgetCode of string

/// The codes for Gizmos start with a "G" and then three digits. 
type GizmoCode = private GizmoCode of string


type ProductCode = 
    | Widget of WidgetCode
    | Gizmo of GizmoCode

/// Constrained to be a integer between 1 and 1000
type UnitQuantity = private UnitQuantity of int

/// Constrained to be a decimal between 0.05 and 100.0
type KilogramQuantity = private KilogramQuantity of decimal

type OrderQuantity = 
    | Unit of UnitQuantity
    | Kilogram of KilogramQuantity


/// Constrained to be a decimal between 0.0 and 1000.0
type Price = private Price of decimal

/// Constrained to be a decimal between 0.0 and 1000.0
type BillingAmount = private BillingAmount of decimal





type PdfAttachment = {
    Name : string
    Bytes: Byte[]
}

type PromotionCode = PromotionCode of string


//======================================
// Reusable constructors and getters for constrained types
//======================================

/// Useful functions for constrained types 

module String50 =
    /// Return the value inside a String50
    let value (String50 str) = str

    /// Create an String50 from a string
    /// Return Error fi input is null, empty, or lenght > 50
    //TODO: think about remove the fieldName from this function
    let create fieldName str =
        ConstrainedTypes.createString fieldName String50 50 str

    /// Create an String50 from a string
    /// Return None if input is null, empty.
    /// Return Error if lenght > maxLen
    /// Return Some if the punt is valid
    let createOption fieldName str =
        ConstrainedTypes.createStringOption fieldName String50 50 str

type String50 with 
    member  this.value = String50.value this

module EmailAddress =
    /// Return the string value inside an EmailAdress
    let value (EmailAddress str) = str

    /// Create an EmailAddres from a string
    /// Return Error if input is null, empty, or doesn´t have an "@" int it
    let create fieldName str = 
        let pattern = ".+@.+" 
        ConstrainedTypes.createLike fieldName EmailAddress pattern str

type EmailAddress with
    member this.value = EmailAddress.value this

module VipStatus =
    
    //TODO: think aboute standardize this helper to value
    let toString status = 
        match status with
        | Normal -> "Normal"
        | VIP -> "VIP"

    //TODO: think aboute standardize this helper to create
    let fromString fieldName str = 
        match str with
        | "Normal" | "normal" -> Ok Normal 
        | "vip" | "VIP" -> Ok VIP
        | _ ->  sprintf "%s: Must be one of 'normal', 'VIP'" fieldName |> Error

//TODO: think about applying this stile to all types            
type VipStatus with
    member this.value = VipStatus.toString this
    
    

module ZipCode = 
    /// Return the string value inside a ZipCode
    let value (ZipCode str) = str

    /// Create a ZipCode from a string
    /// Return Error if input is null, empty, or doesn´t have 5 digits
    let create fieldName str = 
        let pattern = "\d{5}"
        ConstrainedTypes.createLike fieldName ZipCode pattern str

type ZipCode with
    member this.value = ZipCode.value this

module UsStateCode = 

    /// Return the string value inside a UsStateCode
    let value (UsStateCode str) = str

    /// Create a UsStateCode from a string
    /// Return Error if input is null, empty, or desn´t have 2 letters
    let create fieldName str = 
        let pattern = "^(A[KLRZ]|C[AOT]|D[CE]|FL|GA|HI|I[ADLN]|K[SY]|LA|M[ADEINOST]|N[CDEHJMVY]|O[HKR]|P[AR]|RI|S[CD]|T[NX]|UT|V[AIT]|W[AIVY])$"
        ConstrainedTypes.createLike fieldName UsStateCode pattern str


type UsStateCode with
    member this.value = UsStateCode.value this

module OrderId = 
    /// Return the string value inside an OrderId
    let value (OrderId str) = str

    /// Create an OrderId from a string
    /// Return Error if input is null, empty, or length > 50
    let create fieldName str =
        ConstrainedTypes.createString fieldName OrderId 50 str
    
type OrderId with
    member this.value = OrderId.value this


module WidgetCode = 
    
    /// Return the string value inside a WidgetCode
    let value (WidgetCode code) = code

    /// Create an WidgetCode from a string
    /// Return Error if input is null. empty, or not mathcing patter
    let create fieldName code = 
        let pattern = "W\d{4}"
        ConstrainedTypes.createLike fieldName WidgetCode pattern code

type WidgetCode with
    member this.value = WidgetCode.value this

module GizmoCode = 

    /// Return the string value inside a GizmoCode
    let value (GizmoCode code) = code

    /// Create an Gizmocode from a string
    /// Return Error if input is null, empty, or not matching pattern
    let create fieldName code = 
        let pattern = "G\d{3}"
        ConstrainedTypes.createLike fieldName GizmoCode pattern code 


type GizmoCode with
    member this.value = GizmoCode.value this

module ProductCode =
    /// Return the string value inside a ProductCode 
    let value productCode = 
        match productCode with
        | Widget (WidgetCode wc) -> wc
        | Gizmo (GizmoCode gc) -> gc


    /// Create an ProductCode from a string
    /// Return Error if input not mathing pattern
    let create fieldName code = 
        match code with
        | ConstrainedTypes.NullOrEmpty _ -> 
            sprintf "%s: Must not be null or empty" fieldName |> Error 
        
        | ConstrainedTypes.StartsWith "W" code ->
            WidgetCode.create fieldName code |> Result.map Widget
        
        | ConstrainedTypes.StartsWith "G" code ->   
            GizmoCode.create fieldName code |> Result.map Gizmo
            
        | _ -> sprintf "%s: Format no recognized '%s'" fieldName code |> Error
    


type ProductCode with
    member this.value = ProductCode.value this


module OrderLineId = 
    let  create fieldName str =  ConstrainedTypes.createString fieldName OrderLineId 10 str
    let value (OrderLineId id) =  id

type OrderLineId with 
    member this.value = OrderLineId.value this
    

module KilogramQuantity =

    /// Return the value inside a KilogramQuantity
    let value (KilogramQuantity v) = v

    /// Create a KilogramQuatnity from a decimal.
    /// Return Error if input is not a decimal between 0.05 and 100.0
    let create fieldName v = 
        ConstrainedTypes.createDecimal fieldName KilogramQuantity 0.05M 100M v


type KilogramQuantity with
    member this.value = KilogramQuantity.value this


module UnitQuantity =
    
    /// Return the value inside a UnitQuantity
    let value (UnitQuantity v) = v

    /// Create a Unitquantity from a int
    /// Return erro if input is not an integer between 1 and 1000
    let create fieldName v = 
        ConstrainedTypes.createInt fieldName UnitQuantity 1 1000 v


type UnitQuantity with
    member this.value = UnitQuantity.value this



module OrderQuantity = 

    /// Return the value inside a OrderQuantity
    let value qty = 
        match qty with
        | Unit uq -> uq |> UnitQuantity.value |> decimal
        | Kilogram kq -> KilogramQuantity.value kq 


type OrderQuantity with
    member this.value = OrderQuantity.value this


module Price = 

    /// Return the value inside a Price
    let value (Price v) = v

    /// Create a Price from a decimal.
    /// Return Error if input is not a decimal between 0.0 and 1000.00
    let create v = ConstrainedTypes.createDecimal "Price" Price 0.0M 1000.00M v

    // /// Create a price from a decimal.
    // /// throw an exception if out of bounds. This should only be used if you know de value is valid.
    // let unsafeCreate v = 
    //     match create v with
    //     | Ok price -> price
    //     | Error err -> failwithf "not expecting Price to be out of bounds: %s" err
        
    // /// Multiply a Price by a decimal qty.
    // /// Return Error if new price is out of bounds.
    // let multiply qty (Price p) =
    //     create (qty * p)
    

type Price with
    member this.value = Price.value this



module BillingAmount =
    /// Return a value wrapped in a BillingAmount
    let value (BillingAmount v) = v

    /// Create a BillingAmount from a decimal.
    /// Return Error if input is not a decimal between 0.0 and 10000.00
    let create v =
        ConstrainedTypes.createDecimal "BillingAmount" BillingAmount 0.0M  10000.00M v 

    
    /// Sum a list of prices to make a billing amount
    /// Return Error if total is out of bounds
    let sumPrices prices = 
        prices |> List.map Price.value |> List.sum |> create 


type BillingAmount with
    member this.value = BillingAmount.value this

