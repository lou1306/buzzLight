﻿module Checker.Transform
open Checks
open SymbolTable
open Outcome
open Message
open Externs
open LabsCore

let private makeSpawnRanges externs spawn table =
    let makeRanges mp =
        mp 
        |> Map.fold (fun (c, m) name d -> let c' = c + d.def in (c', (Map.add name (c, c') m) )) (0, Map.empty) 
        |> snd
    
    let spawn' =
        spawn
        |> List.map (fun (d:Node<_>) -> d.name, d)
        |> Map.ofList
        |> Map.mapValues (map (Expr.replaceExterns externs >> Expr.evalConstExpr))
                 
    let valid, others = Map.partition (fun _ d -> d.def > 0) spawn'
    let zeroes, negatives = Map.partition (fun _ d -> d.def = 0) others 
    let warnings =
        zeroes |> Map.mapValues (fun d -> {what=SpawnZero d.name; where=[d.pos]}) |> Map.values
    let errors =
        negatives |> Map.mapValues (fun d -> {what=NegativeSpawn d.name; where=[d.pos]}) |> Map.values
    
    wrap {table with spawn=makeRanges valid} (List.ofSeq warnings) (List.ofSeq errors)

// Duplicate attributes in different agents are legal.
let private envAndLstigVars sys lstigs =
    List.concat (List.map (fun x -> x.def.vars |> Set.unionMany |> Set.toList) lstigs)
    |> List.append sys.def.environment

let check (sys, lstigs, agents', properties) =
    let vars = envAndLstigVars sys lstigs
    
    let undefSpawned =
        sys.def.spawn
        |> List.filter (fun d -> not <| List.exists (fun (a:Node<_>) -> a.name = d.name) agents')
        |> List.map (fun d -> {what=UndefAgent d.name; where=[d.pos]})
        |> wrap () []
    
    zero ()
    (* check for duplicate definitions *)
    <??> dupNames sys.def.spawn
    <??> dupNames agents'
    <??> dupNames lstigs
    <??> dupNames sys.def.processes
    <??> dupNames vars
    <?> fold (checkAgent vars) agents'
    
    (* Check for undefined agents in spawn section *)
    <??> undefSpawned
    

let run externs (sys, lstigs, agents', properties) =
    let vars = envAndLstigVars sys lstigs
    let (agents: Node<Agent> list) =
        let spawned = List.map (fun (d: Node<_>) -> d.name) sys.def.spawn |> Set.ofList
        List.filter (fun a -> Set.contains a.def.name spawned) agents'
    
    zero (SymbolTable.empty)
    <??> check (sys, lstigs, agents', properties)
    (* map non-interface variables *)
    <~> fold (tryAddVar externs) vars
    <~> fold mapVar vars
    
    (* map attributes; add stigmergies, global processes, agents*)
    <~> fold (fun a t -> fold mapVar a.def.iface t) agents'
    <~> fold (tryAddStigmergy externs) lstigs
    <~> fold (tryAddProcess externs) sys.def.processes
    <~> fold (tryAddAgent externs) agents

    <~> (makeSpawnRanges externs) sys.def.spawn