﻿namespace LabsCore
open Types

module Expr =

    let rec fold fleaf fref acc expr = 
        let recurse = fold fleaf fref
        match expr with
        | Leaf l -> fleaf acc l
        | Arithm(e1, _, e2) ->
            Seq.fold recurse acc [e1; e2]
        | Unary(_, e) -> recurse acc e
        | Ref r ->
            let newacc = fref acc r
            r.offset 
            |> Option.map (recurse newacc)
            |> Option.defaultValue newacc

    let rec cata fleaf farithm funary fref expr = 
        let recurse = cata fleaf farithm funary fref
        match expr with
        | Leaf l -> fleaf l
        | Arithm(e1, op, e2) -> farithm op (recurse e1) (recurse e2)
        | Unary(op, e) -> funary op (recurse e)
        | Ref r -> fref r.var (Option.map recurse r.offset)
    
    let rec map fleaf fref expr =
        let recurse = map fleaf fref
        match expr with
        | Leaf l -> Leaf(fleaf l)
        | Arithm(e1, op, e2) -> Arithm(recurse e1, op, recurse e2)
        | Ref r -> 
            let newOffset = r.offset |> Option.map recurse
            Ref(fref r newOffset)
        | Unary(u, e) -> Unary(u, recurse e)

    
    let getVars expr =
        fold (fun a _ -> a) (fun a r -> Set.add r.var a) Set.empty expr
        
    let evalConstExpr idfun expr =
        let leaf_ = function
            | Const i -> i
            | Id x -> idfun x
            | Extern a -> failwithf "Not a constexpr: %s" a (* THIS SHOULD NEVER MATCH *)
        let arithm_ = function
            | Plus -> (+)
            | Minus -> (-)
            | Times -> (*)
            | Div -> (/)
            | Mod -> (%)
            | Max -> max
            | Min -> min
        let unary_ = function
            | UnaryMinus -> fun x -> -x
            | Abs -> abs
        cata leaf_ arithm_ unary_ (fun _ _ -> failwith "Not a constexpr") expr
    
    let evalCexprNoId expr = evalConstExpr (fun _ -> failwith "id not allowed here.") expr
        
module BExpr = 
    let rec map fleaf fexpr bexpr =
        let recurse = map fleaf fexpr
        match bexpr with
        | BLeaf b -> fleaf b
        | Neg b -> Neg(recurse b)
        | Compound(op, b) -> Compound(op, List.map recurse b)
        | Compare(e1, op, e2) -> 
            Compare(fexpr e1, op, fexpr e2)
    let rec cata fleaf fneg fcompare fcompound bexpr = 
        let recurse = cata fleaf fneg fcompare fcompound
        match bexpr with
        | BLeaf b -> fleaf b 
        | Neg b -> fneg (recurse b)
        | Compare(e1, op, e2) -> fcompare op e1 e2
        | Compound(op, b) -> fcompound op (List.map recurse b)