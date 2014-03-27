﻿namespace MathNet.Symbolics

open System
open System.Numerics
open MathNet.Numerics
open MathNet.Symbolics


[<AutoOpen>]
module Core =

    let symbol name = Identifier (Symbol name)
    let undefined = Identifier Undefined
    let infinity = Identifier Infinity
    let number (x:int) = Number (Integer (BigInteger(x)))
    let zero = Expression.Zero
    let one = Expression.One
    let two = Expression.Two
    let minusOne = Expression.MinusOne

    let add (x:Expression) (y:Expression) = x + y
    let subtract (x:Expression) (y:Expression) = x - y
    let negate (x:Expression) = -x
    let plus (x:Expression) = +x
    let sum (xs:Expression list) = if xs.Length = 0 then zero else List.reduce (+) xs
    let sumSeq (xs:Expression seq) = Seq.fold (+) zero xs
    let multiply (x:Expression) (y:Expression) = x * y
    let divide (x:Expression) (y:Expression) = x / y
    let invert (x:Expression) = Expression.Invert(x)
    let product (xs:Expression list) = if xs.Length = 0 then one else List.reduce (*) xs
    let productSeq (xs:Expression seq) = Seq.fold (*) one xs
    let pow (x:Expression) (y:Expression) = x ** y

    let apply (f:Function) (x:Expression) = Function (f, x)
    let applyN (f:Function) (xs:Expression list) = FunctionN (f, xs)


module Functions =

    let abs x = apply Abs x
    let ln x = apply Ln x
    let exp x = apply Exp x
    let sin x = apply Sin x
    let cos x = apply Cos x
    let tan x = apply Tan x


module Numbers =

    let max2 a b =
        match a, b with
        | a, b | b, a when a = undefined -> a
        | a, b | b, a when a = infinity -> a
        | a, b | b, a when a = -infinity -> b
        | Number a, Number b -> Number (Number.Max(a, b))
        | _ -> failwith "number expected"

    let min2 a b =
        match a, b with
        | a, b | b, a when a = undefined -> a
        | a, b | b, a when a = infinity -> b
        | a, b | b, a when a = -infinity -> a
        | Number a, Number b -> Number (Number.Min(a, b))
        | _ -> failwith "number expected"

    let max ax = List.reduce max2 ax
    let min ax = List.reduce min2 ax


module Elementary =

    open System.Collections.Generic

    let numberOfOperands = function
        | Sum ax | Product ax -> List.length ax
        | Power _ -> 2
        | Function _ -> 1
        | FunctionN (_, xs) -> List.length xs
        | Number _ | Identifier _ -> 0

    let operand i = function
        | Sum ax | Product ax | FunctionN (_, ax) -> List.nth ax i
        | Power (r, _) when i = 0 -> r
        | Power (_, p) when i = 1 -> p
        | Function (_, x) when i = 0 -> x
        | Number _ | Identifier _ -> failwith "numbers and identifiers have no operands"
        | _ -> failwith "no such operand"

    let rec freeOf symbol x =
        if symbol = x then false else
        match x with
        | Sum ax | Product ax | FunctionN (_, ax) -> List.forall (freeOf symbol) ax
        | Power (r, p) -> freeOf symbol r && freeOf symbol p
        | Function (_, x) -> freeOf symbol x
        | Number _ | Identifier _ -> true

    let rec freeOfSet (symbols: Set<Expression>) x =
        if symbols.Contains(x) then false else
        match x with
        | Sum ax | Product ax | FunctionN (_, ax) -> List.forall (freeOfSet symbols) ax
        | Power (r, p) -> freeOfSet symbols r && freeOfSet symbols p
        | Function (_, x) -> freeOfSet symbols x
        | Number _ | Identifier _ -> true

    let rec map f = function
        | Sum ax -> sum <| List.map f ax
        | Product ax -> product <| List.map f ax
        | Power (r, p) -> (f r) ** (f p)
        | Function (fn, x) -> apply fn (f x)
        | FunctionN (fn, xs) -> applyN fn (List.map f xs)
        | _ as x -> x

    let rec substitute y r x =
        if y = x then r else
        match x with
        | Sum ax -> sum <| List.map (substitute y r) ax
        | Product ax -> product <| List.map (substitute y r) ax
        | Power (radix, p) -> (substitute y r radix) ** (substitute y r p)
        | Function (fn, x) -> apply fn (substitute y r x)
        | FunctionN (fn, xs) -> applyN fn (List.map (substitute y r) xs)
        | Number _ | Identifier _ -> x

    let rec numerator = function
        | Product ax -> product <| List.map numerator ax
        | Power (r, Number (Integer n)) when n < BigInteger.Zero -> one
        | z -> z

    let rec denominator = function
        | Product ax -> product <| List.map denominator ax
        | Power (r, (Number (Integer n) as p)) when n < BigInteger.Zero -> r ** -p
        | _ -> one
