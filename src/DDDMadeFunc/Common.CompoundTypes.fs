namespace OrderTaking.Common

open System

// ========================================================
// Common compound types used though the OrderTaking domain
// 
// Includes: customers, addresses, etc.
// Plus common erros.
// ========================================================

// ========================================================
// Customer related types
// ========================================================


type PersonalName = {
    FirstName: String50
    LastName: String50
}

type CustomerInfo = {
    Name: PersonalName
    EmailAddress: EmailAddress
    VipStatus: VipStatus
}

// ====================
// Address-related
// ====================

type Address = {
    AddressLine1 : String50
    AddressLine2 : String50 option
    AddressLine3 : String50 option
    AddressLine4 : String50 option
    City: String50
    ZipCode: ZipCode
    State: UsStateCode
    Country: String50
}

