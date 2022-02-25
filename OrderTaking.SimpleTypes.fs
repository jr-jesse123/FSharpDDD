namespace OrderTaking.Common

// Simple types and constrained types related to the  orderTaking domain

open System

/// Constrained to be 50 chars or less, not null
type String50 = private String50 of string

/// An email addres
type EmailAdrres = private EmailAddress of string

/// Costumer´s VIP status
type VipStatus = 
    | Normal
    | VIP

/// A Zip code
type ZipCode = private ZipCode of String50

/// A US 2 letter state code
type UsStateCode = private UsStateCode of string

//TODO: think about change the inner OrderId to use Guid 
/// An Id for Orders. Constrained to be a non-empty string < 10
type OrderId = private OrderId of string

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
module ConstrainedTypes = 
    let (|NullOrEmpty|_|)  str = 
        if String.IsNullOrEmpty str then Some str else None
    let (|TooLong|_|) maxLen (str: string) =
        if str.Length > maxLen then Some str else None
   
    
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

module String50 =
    /// Return the value inside a String50
    let value (String50 str) = str

    /// Create an String50 from a string
    /// Return Error fi input is null, empty, or lenght > 50
    let create fieldName str =
        ConstrainedTypes.createString fieldName String50 50 str

    /// Create an String50 from a string
    /// Return None if input is null, empty.
    /// Return Error if lenght > maxLen
    /// Return Some if the punt is valid
    let createOption fieldName str =
        ConstrainedTypes.createStringOption fieldName String50 50 str


module EmailAdrres =
    /// Return the string value inside an EmailAdress
    let value (EmailAddress str) = str

    /// Create an EmailAddres from a string
    /// Return Error if input is null, empty, or doesn´t have an "@" int it
    let create fieldName str = 
        let pattern = ".+@.+" 
        ConstrainedTypes.createLike fieldName EmailAddress pattern str


module VipStatus =
    
    let toString status = 
        match status with
        | Normal -> "Normal"
        | VIP -> "VIP"

    let fromString fieldName str = 
        match str with
        | "Normal" | "normal" -> Ok Normal 
        | "vip" | "VIP" -> Ok VIP
        | _ ->  sprintf "%s: Must be one of 'normal', 'VIP'" fieldName |> Error
            

    

