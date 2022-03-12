# FSharpDDD
  This repo contains the result of some exercises stretching lessons from the excelent book Domain Driven Desing Made Functional from Scott Wlaschin

  Scott wrote an excelent book about how f# is an awsome language for modeling typical business dailly job,
due to type inference that gives an great balance between flexibility and compile time safety, .net eco-system great maturity, 
and f#'s great balance between conciseness and redability that levereges it´s functional first aproach.

But the code in the book repo (https://github.com/swlaschin/DomainModelingMadeFunctional) was written in .net framework and also didn´t compile.

  So I took the adventure to write it in a outside-in manner writting the public api first with all the related types and then the  implementation 
letting the compiller complain about the COMPUTER EXPRESSION's and (functor/monad)'s lackness. 

  this gave me a great vision on f#'s function composition and computation expression, in fact f# lacks high kinded types, but in most cases it takes 
really few strokes to writte something lik 'Asynk.map'.

this repo isn´t exactly a port from the original repo, since a lot has changed in the language and I have my personal biasses as well. 
Anyway here are some of the conce well streeched here

* Type-Driven Development 
* Functions and Type composition
* Property-Based-Testing (Used the test the Computation-Expresssions implmentations)
* Computations-Expressions (kind of F# way to Monads, altough not as restrictive)
* Ubiquotous Language
* Monads and Applicativs (the validations were all applicatives and the rest of the workflow as implemented in Monadic way) 
* Buonded Contexts
* Partial Application and DI

