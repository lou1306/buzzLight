﻿module internal System
open FParsec
open Common
open Types
open Link
open Expressions
open Properties

let pspawn = 
    ws ((ws IDENTIFIER) .>>. pint32) |> sepbycommas |> betweenBrackets

let mapToLinkTerm prop = fun _ -> 
    match prop with
    | PropertyTerm.ConstTerm(v) -> Reply(T (ConstTerm v))
    | PropertyTerm.KeyRef(k,c) -> 
        match c with 
        | "c1" -> Reply(T (KeyRefC1 k))
        | "c2" -> Reply(T (KeyRefC2 k))
        | s -> 
            sprintf "Invalid link identifier %s" s 
            |> fun msg -> Reply(Error, ErrorMessageList(Message msg))

let plinkexpr, private plinkexprRef = createParserForwardedToRef()

let plinkterm = 
    let pterm = ppropTerm >>= mapToLinkTerm
    let pparen = betweenParen plinkexpr
    let pabs = (skipString "abs") >>. (betweenParen (ws plinkexpr)) |> ws |>> Abs
    choice [pabs; pparen; attempt pterm]


do plinkexprRef := 
    maybeTuple2 (ws plinkterm) ((ws parithmop) .>>. (ws plinkexpr)) 
        (fun (t1, (op, t2)) -> Arithm(t1, op, t2))


let plink, private plinkRef = createParserForwardedToRef()

do plinkRef :=
    let pneg = NEG >>. plink |>> Neg
    let pcompare =
        tuple3 plinkexpr (ws pcompareop) plinkexpr |>> Compare
    choice [
        attempt pneg;
        attempt pcompare;
        stringReturn "true" True;
    ]


let psys = 
    ws (skipString "system")
    >>. ws (
        betweenBraces (
            spaces >>. manyComments
            >>. tuple3
                (ws (setDef "environment" KEYNAME id) .>> manyComments)
                (ws (skipString "spawn") >>. (ws EQ) >>. ws (pMap pspawn) .>> manyComments)
                (ws (skipString "link") >>. (ws EQ) >>. ws plink .>> manyComments)))
