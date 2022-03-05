
namespace global

open System

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


[<RequireQualifiedAccess>]
module Async = 
    let retn x = async.Return x

    let map f xA = 
        async {
            let! x = xA
            return f x
        }

type AsyncResult<'Success,'Failure> = Async<Result<'Success, 'Failure>>

module AsyncResult =
    let retn x : AsyncResult<'Success,'Failure> =   Async.retn (Ok x)
