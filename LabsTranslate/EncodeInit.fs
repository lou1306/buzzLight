﻿module internal EncodeInit
open Types
open Base
open Templates
open Liquid

let initVarSim (mapping:KeyMapping) (var:Var) init =
    let baseIndex = snd mapping.[var.name]
    let initValue = 
        match init with
        | Choose(l) -> l.Item (rng.Next l.Length)
        | Range(minI, maxI) -> rng.Next(minI, maxI)
        |> string
    match var.vartype with
    | Scalar -> assign var initValue baseIndex
    | Array(s) ->
        seq [baseIndex..baseIndex+s-1]
        |> Seq.map (assign var initValue)
        |> String.concat "\n"

let initVar (mapping:KeyMapping) (var:Var) init =
    let baseIndex = snd mapping.[var.name]
    let cVarName = sprintf "guess%i%A" baseIndex var.location
    let cVarDef = def cVarName init

    let cVarAssume =
        match init with
        | Choose l when l.Length = 1 -> 
            sprintf "%s = %i;\n" cVarName l.Head
        | Choose l ->
            l
            |> Seq.map (sprintf "(%s == %i)" cVarName)
            |> String.concat " || "
            |> assume
        | Range(minI, maxI) -> //assumeIntRange index minI maxI
            sprintf "%s >= %i && %s < %i" cVarName minI cVarName maxI |> assume

    cVarDef +
    cVarAssume + (
        match var.vartype with
        | Scalar -> assign var cVarName baseIndex
        | Array(s) ->
            seq [baseIndex..(baseIndex+s-1)]
            |> Seq.map (assign var cVarName)
            |> String.concat "\n")

/// Renders the init() section using the given initialization function.
let translateInit initFn (sys, trees, mapping) =
    let initPc sys trees =
        trees
        |> Map.map (fun n (_, entry) -> 
            let minI, maxI = sys.spawn.[n]
            sprintf "pc[i][0] = %i;" entry
            |> forLoop minI maxI)
        |> Map.values
        |> String.concat "\n"

    let initMap m = 
        m
        |> Map.map (initFn mapping)
        |> Map.values
        |> String.concat "\n"

    let initAll =
        sys.spawn
        |> Map.map (fun x range -> 
            let ifaceinit = sys.components.[x].iface |> initMap
            let lstigsinit = 
                sys.components.[x].lstig
                |> Seq.map (initMap)
                |> String.concat "\n"
            (range, ifaceinit + "\n" + lstigsinit))
        |> Map.fold (fun str _ ((rangeStart, rangeEnd), inits) -> 
            (str + (forLoop rangeStart rangeEnd inits))) "" //FIXME
    let makeTuples comps (mapping:KeyMapping) =
        /// Finds the min and max indexes of the given tuple.
        let extrema (tup:Map<Var,Init>) =
            let indexes = 
                tup
                |> Map.toSeq
                |> Seq.map (fun (v, _) -> snd mapping.[v.name])
            (Seq.min indexes, Seq.max indexes)

        let doLiquid (tup: Map<Var,Init>) =
            let extr = extrema tup
            tup 
            |> Map.map (fun v _ -> Dict [
                "index", Int (snd mapping.[v.name])
                "start", Int (fst extr)
                "end", Int (snd extr)
                ])
            |> Map.values

        comps
        |> Seq.map (fun c -> c.lstig)
        |> Seq.map (List.map doLiquid)
        |> Seq.map Seq.concat
        |> Seq.concat
        
    [
        "initenv", sys.environment |> initMap |> indent 4 |> Str;
        "initvars", initAll |> indent 4 |> Str;
        "initpcs", (initPc sys trees) |> indent 4 |> Str;
        "tuples", Lst (makeTuples (Map.values sys.components) mapping)
    ]
    |> renderFile "templates/init.c"    
    |> Result.bind (fun () -> Result.Ok(sys, trees, mapping))