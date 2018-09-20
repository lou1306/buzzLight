﻿module internal Properties
open FParsec
open Types
open Expressions

let propertyRef p =
    pipe3
        (ws KEYNAME)
        (opt (betweenBrackets p))
        (ws (skipString "of") >>. (ws KEYNAME))
        (fun k offset y -> {var=(k, y); offset=offset})

let pquantifier =
        pipe3
            (ws <| choice [
                stringReturn "forall" All;
                stringReturn "exists" Exists
            ])
            (ws IDENTIFIER)
            (ws KEYNAME)
            (fun a b c -> c, (b, a))

let pproperty = 
    let propertyLink = 
        (ws (skipString "id")) >>.
        (ws (skipString "of")) >>.
        (ws KEYNAME)

    let pbaseprop = makeBExprParser (makeExprParser propertyRef propertyLink)
    let pmodality = 
        choice [
            stringReturn "finally" Finally;
            stringReturn "always" Always]
        |> ws
    pipe4 
        (ws IDENTIFIER .>> (ws EQ))
        pmodality
        ((sepEndBy1 pquantifier (ws (skipChar ','))) >>= toMap)
        pbaseprop
        (fun n m qs pred -> n, {
            name=n;
            modality=m;
            quantifiers=qs;
            predicate=pred
            })

let pproperties = 
    pproperty |> many >>= toMap
    |> (>>.) spaces
    |> ws
    |> betweenBraces
    |> (>>.) (ws (skipString "check"))