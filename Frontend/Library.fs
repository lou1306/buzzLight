﻿module Frontend.Frontend
open Frontend.Checks
open Types
open Frontend.SymbolTable
open Frontend.Outcome
open Frontend.Message
open Frontend.LTS
open LabsCore

// Duplicate attributes in different agents are legal.
let private envAndLstigVars sys lstigs =
    List.concat (List.map (fun x -> x.def.vars |> Set.unionMany |> Set.toList) lstigs)
    |> List.append sys.def.environment

let check (sys, lstigs, agents', _) =
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
    
    zero (Frontend.SymbolTable.empty)
    <??> check (sys, lstigs, agents', properties)
    (* map non-interface variables *)
    <~> fold (tryAddVar externs) vars
    <~> fun x -> fold mapVar (Map.values x.variables |> Seq.filter (isEnvVar)) x
    <~> fun x ->
            (* Ensure that variables in the same tuple get contiguous indices *)
            Map.values x.variables
            |> Seq.filter (isLstigVar)
            |> Seq.groupBy (fun v -> v.location)
            |> Seq.map snd
            |> Seq.fold (fun x' s -> x' <~> fold mapVar s) (zero x)
    
    (* map attributes; add stigmergies, global processes, agents*)
    <~> fold (tryAddIface externs) agents
    <~> fold (tryAddStigmergy externs) lstigs
    <~> fold (tryAddProcess externs) sys.def.processes
    <~> fun x ->
        fold (tryAddAgent externs) agents (x, (Set.empty, (0, ExecPoint.empty, Map.empty, Map.empty)))
    <~> (fst >> zero)
    <~> (makeSpawnRanges externs) sys.def.spawn
    (* properties can only be added after spawn *)
    <~> fold (tryAddProperty externs) properties

/// Turn a variable initializer into a list of BExpr
/// (multiple BExprs are returned when v is an array).
let initBExprs idfn (v:Var<_>, i: int) =
    let map_ r =
        let leaf_ l = match l with | Id _ -> idfn | _ -> l
        Expr.map leaf_ (fun _ o -> {r with offset=o})
    let refs =
        let r = {var=(v, i); offset = None}
        match v.vartype with
        | Scalar -> [r]
        | Array s -> List.map (fun i -> {r with offset = Some (Leaf (Const i))}) [0 .. s-1]
    match v.init with
    | Undef -> List.map (fun r -> Compare(Ref r, Equal, Leaf(Extern "undef_value"))) refs
    | Choose l ->
        let choice r =
            List.map (map_ r >> (fun e -> Compare(Ref r, Equal, e))) l
            |> fun l -> Compound(Disj, l)
        List.map choice refs
    | Range (start_, end_) -> 
        let between r = Compound(Conj, [Compare(Ref r, Geq, map_ r start_); Compare(Ref r, Less, map_ r end_)])
        List.map (fun r -> between r) refs
