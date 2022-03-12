
namespace global

open System
open Microsoft.VisualBasic



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
    let applyM fResult xResult = 
        match fResult, xResult with
        | Ok f , Ok x -> Ok (f x)
        | Ok _ , Error err //-> Error err
        | Error err, Ok _ -> Error err
        | Error e1, Error e2 ->  Error e1

    let sequence aListOfResults = 
        let (<*>) = applyM
        let (<!>) = map
        let cons head tail = head::tail
        
        let consR headR tailR = cons <!> headR <*> tailR
        let initialValue = Ok [] // empty list inside Result

        List.foldBack consR aListOfResults initialValue

    let isOk = function |Ok _ -> true |Error _ -> false


    let bindReturn fResult xResult = 
        xResult |> Result.map fResult
    

    let mergesources result1 result2 = 
        match result1, result2 with
        | Ok ok1, Ok ok2 -> Ok (ok1,ok2) // compiler will automatically de-tuple these - very cool!
        | Error errs1, Ok _ -> Error errs1
        | Ok _, Error errs2 -> Error errs2
        | Error errs1, Error errs2 -> Error (errs1 @ errs2)  // accumulate errors


[<AutoOpen>]
module ResultComputationExpression  = 

    type ResultBuilder() =

        member _.Bind(x,f) = 
            match x with 
            | Ok x -> f x
            | Error err -> Error err


        member _.Return x = Ok x

        member this.Zero() = this.Return()

        member _.BindReturn(result, f) =
               Result.bindReturn f result

        member _.MergeSources(result1, result2) =
           Result.mergesources result1 result2

    let result = new ResultBuilder()


type Validation<'Success,'Failure> = Result<'Success,'Failure list>

/// functions for the 'Validation' type (mostly applicative)
[<RequireQualifiedAccess>]
module Validation= 
    /// Alias for Result.Map
    let amp = Result.map

    /// Appli a Validation<fn> to a Validation<x> applicativelly
    let applyA (fV:Validation<_,_>) (xV:Validation<_,_>) : Validation<_,_> =
        match fV, xV with
        | Ok f, Ok x -> Ok (f x)
        | Error errs1, Ok _ -> Error errs1
        | Ok _, Error errs2 -> Error errs2
        | Error errs1, Error errs2 -> Error (errs1 @ errs2)
    

    /// combine a list of Validation, applicatively
    let sequence (aListOfValidations:Validation<_,_> list) =
        let (<*>) = applyA
        let (<!>) = Result.map
        let cons head tail = head :: tail
        let consR headR tailR = cons <!> headR <*> tailR
        let initialValue = Ok []

        List.foldBack consR aListOfValidations initialValue


    let ofresult xR =
        xR |> Result.mapError List.singleton

    let toResult (xV: Validation<_,_>) : Result<_,_> =
        xV 

[<RequireQualifiedAccess>]
module Async = 
    let retn x = async.Return x

    let map f xA = 
        async {
            let! x = xA
            return f x
        }

    let bind f xA = async.Bind(xA,f)

  

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

    let map f (x:AsyncResult<_,_>) =
        //x |> f |> Result.map |> Async.map 
        let map = Result.map >> Async.map
        map f x
        
        //Async.map (Result.map f) x

    let mapError f (xAsyncresult: Async<Result<'c,'a>>) (*: AsyncResult<_,_>*) = 
        xAsyncresult |> Async.map ( Result.mapError f)

    let ofResult xResult = 
        async.Return xResult

        
    /// Apply an AsyncResult function to an AsyuncResult value, monadically
    let applyM (fAsyncResult: AsyncResult<_,_>) (xAsyncResult: AsyncResult<_,_>) : AsyncResult<_,_> =
        fAsyncResult |> Async.bind   (fun fResult ->
        xAsyncResult |> Async.map (fun xResult -> Result.applyM fResult xResult))

    /// Apply an AsyncResult function to an AsyncResult value, applicatively
    let applyA (fAsyncResult : AsyncResult<_,_>) (xAsyncResult: AsyncResult<_,_>) : AsyncResult<_,_> =
        fAsyncResult |> Async.bind (fun fResult -> 
        xAsyncResult |> Async.map (fun xResult -> Validation.applyA fResult xResult)
        )

[<AutoOpenAttribute>]
module AsyncResultComputationExpression = 
    
    type AsyncResultbuilder() =
        member _.Bind(x,f) = AsyncResult.bind f x
        member _.Return(x)  = AsyncResult.retn x

        member _.Zero() = AsyncResult.retn ()

        member _.BindReturn(result, f) =
            result |> AsyncResult.map f

        member _.MergeSources(xAR,yAR) =
          let (<*>) = AsyncResult.applyA
          let (<!>) = AsyncResult.map
          let totuple x1 x2 = (x1,x2)

          totuple <!> xAR <*> yAR


        //member _.MergeSources(x:AsyncResult<_,_>,y:AsyncResult<_,_>) =
        //        let 
        //        AsyncResult.applyA 

    let asyncResult = AsyncResultbuilder()