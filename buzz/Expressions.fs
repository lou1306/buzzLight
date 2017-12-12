﻿namespace Buzz
open System
open Buzz.LStig
    module Expressions =

        /// Denotational semantic of expressions: Evaluation
        let rec eval : Expr -> Interface -> LStig -> Val option = function
            | Const(c) -> (fun i l -> Some c)
            | RandomPoint(fromX, fromY, toX, toY) ->
                fun i l -> Some <| P(rng.Next(fromX, toX+1), rng.Next(fromY, toY+1))
            | L(k) -> 
                fun i l ->
                if l.[k].IsSome
                then Some <| fst l.[k].Value
                else failwith << sprintf "%s not tound" <| k.ToString()
            | I(k) -> (fun i l -> Some i.[k])
            | Sum(e1, e2) ->
                fun i l -> 
                match (eval e1 i l, eval e2 i l) with
                | (Some(x1), Some(x2)) -> x1 + x2
                | _ -> None

        /// Denotational semantic of expressions: Stigmergy keys
        let rec keys : Expr -> Set<Key> = function
        | L(k) -> Set.singleton k
        | Sum(e1, e2) ->Set.union (keys e1) (keys e2)
        | _ -> Set.empty