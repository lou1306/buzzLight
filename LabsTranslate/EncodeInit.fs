﻿module internal EncodeInit
open Types
open Base
open Templates
open Liquid

let initVar (mapping:KeyMapping) tid (var:Var) =
    let baseIndex = mapping.[var.name]
    let cVarName s = "_" + (translateLocation var.location tid s)

    let initAssumption i =
        let v = cVarName (string i)
        match var.init with
        | Undef -> sprintf "%s == undef_value" v |> assume
        | Choose l ->
            l
            |> Seq.map (sprintf "(%s == %i)" v)
            |> String.concat " || "
            |> assume
        | Range(minI, maxI) -> //assumeIntRange index minI maxI
            sprintf "%s >= %i && %s < %i" v minI v maxI |> assume
        + match var.location with 
            | L _ -> sprintf "Ltstamp[%s][%i] = j++;\n" tid i
            | _ -> ""

    match var.vartype with
    | Scalar -> initAssumption baseIndex
    | Array s ->
        seq [baseIndex..(baseIndex+s-1)]
        |> Seq.map initAssumption
        |> String.concat ""

let translateInit (sys, trees, mapping:KeyMapping) =

    let initPc sys trees =
        trees
        |> Map.map (fun n (_, entry) -> 
            let minI, maxI = sys.spawn.[n]
            seq [
                "start", Int minI
                "end", Int maxI
                "pc", Int entry
            ] |> Dict)
        |> Map.values

    let initMap tid m = 
        m
        |> Set.map (initVar mapping tid)
        |> String.concat ""

    let initRange x (min, max) = 
        let initlstig name i =
            sys.stigmergies.[name].vars
            |> List.map (initMap i)
            |> String.concat "\n"
        sys.components.[x].lstig
        |> List.map (fun name -> List.map ((initlstig name) << string) [min..max-1])
        |> List.map (String.concat "")
        |> fun x -> x
        |> List.append
            (List.map (fun i -> (initMap (string i) sys.components.[x].iface)) [min..max-1])
        |> String.concat ""

    let initAll = 
        sys.spawn
        |> Map.map initRange
        |> Map.values
        |> String.concat "\n"
        
    [
        "initenv", sys.environment |> initMap "" |> indent 4 |> Str
        "initvars", initAll |> indent 4 |> Str
        "initpcs", (initPc sys trees) |> Lst
    ]
    |> renderFile "templates/init.c"    
