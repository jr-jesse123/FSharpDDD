namespace OrderTaking.PlaceOrder



open System
open OrderTaking.Common 
open OrderTaking.PlaceOrder.InternalTypes
open System

module internal PricingModule = 
    
    let createPricingMethod (promotionCode : string) =
        match promotionCode with
        | null -> Standard
        | codeStr -> Promotion (PromotionCode promotionCode)

    let getPricingFunction 
            (standardPrices : GetStandardPrices ) 
            (promoPrices : GetPromotionPrices) 
            : GetPricingFunction =
        
        // standardPrice chace
        let getStandardPrice : GetProductPrice =
            standardPrices()

        // the promotional pricing function
        let getPromotionPrice promotionCode : GetProductPrice =
            let getPromotionPrice = promoPrices promotionCode 
            
            // return the lookup function
            fun productCode -> 
                match getPromotionPrice productCode with
                // found
                | Some price -> price
                // not found
                | None -> getStandardPrice productCode


        fun pricingMethod ->
            match pricingMethod with
            | Standard -> 
                getStandardPrice

            | Promotion promotionCode ->
                getPromotionPrice promotionCode