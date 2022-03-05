
namespace global

open System



[<AutoOpen>]
module internal Utils = 
    let throwNotImplemented () = failwith "not implemented"    
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

        member this.Zero() = this.Return()

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

    /// apply a monadic function to an AsyncResult value
    let bind  (f: 'a -> AsyncResult<'b,'c>) (xAsyncResult: AsyncResult<'a,'c>) : AsyncResult<_,_> =
        async {
            let! xResult = xAsyncResult
            match xResult with
            | Ok x -> return! ( f x )
            | Error err -> return ( Error err)
        }

    let mapError f (xAsyncresult: Async<Result<'c,'a>>) (*: AsyncResult<_,_>*) = 
        xAsyncresult |> Async.map ( Result.mapError f)

    let ofResult xResult = 
        async.Return xResult

        //Async.map (Result.mapError f) xAsyncresult

[<AutoOpenAttribute>]
module AsyncResultComputationExpression = 
    
    type AsyncResultbuilder() =
        member _.Bind(x,f) = AsyncResult.bind f x
        member _.Return(x)  = AsyncResult.retn x

        member _.Zero() = AsyncResult.retn ()

    let asyncResult = AsyncResultbuilder()