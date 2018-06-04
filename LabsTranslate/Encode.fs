﻿module Encode
open Types
open Base
open Templates
open Expressions
open Properties
open Link

/// A Node is composed by a program counter identifier, an entry/exit point,
/// and some contents: either an action or a pair of parallel nodes.
/// Nodes can be augmented by guards.


type Node = 
| Basic of parent:Set<pcCondition> * entry:pcCondition * Action * exit:pcCondition * lbl:string
| Guarded of string * Node
| Goto of parent:Set<pcCondition> * entry:pcCondition * exit:pcCondition * lbl:string
| Stop of parent:Set<pcCondition> * entry:pcCondition
with 
    member this.lbl = 
       match this with
       | Basic(lbl=l;entry=e)
       | Goto(lbl=l;entry=e) -> sprintf "%s_%i" l e.value
       | Stop(entry=e) -> sprintf "Stop_%i" e.value
       | Guarded(_,n) -> n.lbl

let baseVisit (procs:Map<string, Process>) counter mapping rootName =
    let pccount = makeCounter(-1)
    let rec visit name rootEntry cnt parent entry exit p lbl =
        let vs = visit name rootEntry cnt
        let parentUnion = Set.union parent

        match p with
        | Base(a) -> 
            Set.singleton <| Basic(parent, entry, a, exit, lbl), Set.singleton <| exit
        | Name(s) when s = name ->
            Set.singleton <| Goto(parent, entry, rootEntry, lbl), Set.empty
        | Name(s) -> visit name rootEntry cnt parent entry exit procs.[s] (lbl)
        | Await(b, p) -> 
            let pnodes, pexits = (vs parent entry exit p lbl)
            pnodes
            |> Set.map (fun n -> Guarded((assume (translateBExpr mapping b)), n))
            |> fun nodes -> nodes, pexits
        | Choice(p, q) -> 
            let pnodes, pexits = (vs parent entry exit p (lbl+"_L"))
            let qnodes, qexits = (vs parent entry exit q (lbl+"_R"))
            (Set.union pnodes qnodes), (Set.union pexits qexits)
        | Seq(p, q) ->
            let k = {pc=entry.pc;value=cnt()}
            let pnodes, pexits = (vs parent entry k p lbl)
            let qnodes, qexits = (vs (parentUnion pexits) k exit q lbl)
            (Set.union pnodes qnodes), qexits
        | Par(p, q) ->
            let leftPc, rightPc = pccount(), pccount()
            let lCount, rCount = makeCounter(-1), makeCounter(-1)
            let newPar = parent.Add entry
            let pnodes, pexits = visit name rootEntry lCount newPar {pc=leftPc;value=lCount()} {pc=leftPc;value=lCount()} p (lbl+"_L")
            let qnodes, qexits = visit name rootEntry rCount newPar {pc=rightPc;value=rCount()} {pc=rightPc;value=rCount()} q (lbl+"_R")
            (Set.union pnodes qnodes), (Set.union pexits qexits).Add(entry)
        | Skip -> Set.singleton <| Goto(parent, entry, exit, lbl), Set.singleton <| exit
        | Nil -> Set.singleton <| Stop(parent, entry), Set.empty

    let pc = pccount()
    let entry = {pc=pc;value=counter()}
    (visit rootName entry counter Set.empty entry {pc=pc;value=counter()} (procs.[rootName] ^. Nil) rootName
    |> fst), entry.value

let encode (sys, mapping) = 
    let spawnedComps = 
        sys.components
        |> Map.filter (fun n _ -> sys.spawn.ContainsKey n)
    let counter = makeCounter(-1)
       
    let trees = 
        spawnedComps
        |> Map.map (fun _ def -> (def, Map.merge sys.processes def.processes))
        |> Map.map (fun _ (def, procs) -> baseVisit procs counter mapping def.behavior)

    Result.Ok(sys, trees, mapping)

let makeTuples comps mapping =
    let lstigKeys = Map.filter (fun k info -> info.location = L) mapping

    let tupleExtrema key =
        let filtered (m:Map<Key, 'a>) =
            lstigKeys
            |> Map.filter (fun k _ -> m.ContainsKey k)

        comps
        |> Seq.map (fun c -> 
            c.lstig
            |> Seq.filter (fun m -> m.ContainsKey key)
            |> Seq.map (fun m -> 
                if m.Count > 1
                then (findMinIndex L (filtered m), findMaxIndex L (filtered m))
                else (mapping.[key].index, mapping.[key].index)))
        |> Seq.concat
        |> Seq.head

    lstigKeys
    |> Map.map (fun k info ->
        let extrema = tupleExtrema k
        (sprintf """
tupleStart[%i] = %i;
tupleEnd[%i] = %i;
""" info.index (fst extrema) info.index (snd extrema)))
    |> Map.values
    |> String.concat ""

let initPc sys trees =
    trees
    |> Map.map (fun n (_, entry) -> 
        let minI, maxI = sys.spawn.[n]
        sprintf "pc[i][0] = %i;" entry
        |> forLoop minI maxI)
    |> Map.values
    |> String.concat "\n"

let translateHeader ((sys,trees, mapping:KeyMapping), bound) =

    let maxPc =
        let rec getPc = function
            | Goto(entry=e; parent=p)
            | Stop(entry=e; parent=p)
            | Basic(entry=e; parent=p) -> 
                if p.IsEmpty then e.pc
                else
                    (p |> Set.map (fun x -> x.pc) |> Set.maxElement)
                    |> max e.pc
            | Guarded(_, n) -> (getPc n)
        trees
        |> Map.values
        |> Seq.map fst
        |> Set.unionMany
        |> Set.map (getPc)
        |> Set.maxElement
        |> (+) 1

    let maxcomps = 
        Map.fold (fun state k (_, cmax) -> max state cmax) 0 sys.spawn

    printfn "#define BOUND %i" bound
    printfn "#define MAXCOMPONENTS %i" maxcomps
    printfn "#define MAXPC %i" maxPc
    printfn "#define MAXKEYI %i" ((findMaxIndex I mapping) + 1)
    printfn "#define MAXKEYL %i" ((findMaxIndex L mapping) + 1)
    printfn "#define MAXKEYE %i" ((findMaxIndex E mapping) + 1)
    printfn "%s" baseHeader
    printfn "%s" (encodeLink mapping sys.link)
    printfn "%s" systemFunctions
    Result.Ok(sys, trees, mapping)

let translateInitSim (sys,trees, mapping:KeyMapping) =
    let makeInits i initMap = 
        initMap
        |> Map.map (fun k v -> initSimulate i mapping.[k] v)
        |> Map.values
        |> String.concat "\n"
    sys.spawn
    |> Map.map (fun x (minI, maxI) -> 
        seq [minI..maxI-1]
        |> Seq.map (fun i -> 
            (makeInits i sys.components.[x].iface) + "\n" +
            (sys.components.[x].lstig
             |> Seq.map (makeInits i)
             |> String.concat "\n"))
        |> String.concat "\n")
    |> Map.values
    |> String.concat "\n"
    |> 
        if not sys.environment.IsEmpty then 
            (+) (makeInits 0 sys.environment)
        else id
    |> (+) (initPc sys trees)
    |> baseInit
    |> (+) (makeTuples (Map.values sys.components) mapping)
    |> (indent 4)
    |> (cvoid "init" "")
    |> printfn "%s"

    Result.Ok(sys, trees, mapping)

let translateInit (sys,trees, mapping:KeyMapping) =
    let makeInits initMap = 
        initMap
        |> Map.map (fun k v -> init mapping.[k] v)
        |> Map.values
        |> String.concat "\n"

    sys.spawn
    |> Map.map (fun x range -> range, (makeInits sys.components.[x].iface))
    |> Map.map (fun x (r, inits) -> 
        let lstigsinit = 
            sys.components.[x].lstig
            |> Seq.map makeInits
            |> String.concat "\n"
        (r, inits+ "\n" + lstigsinit))
    |> Map.fold (fun str _ ((rangeStart, rangeEnd), inits) -> 
        (str + (forLoop rangeStart rangeEnd inits))) ""
    |> 
        if not sys.environment.IsEmpty then
            (+) (makeInits sys.environment)
        else id
    |> (+) (initPc sys trees) 
    |> baseInit
    |> (+) (makeTuples (Map.values sys.components) mapping)
    |> (indent 4)
    |> (cvoid "init" "")
    |> printfn "%s"

    Result.Ok(sys, trees, mapping)

let translateAll (sys, trees, mapping:KeyMapping) =

    let encodeAction (a:Action) = 
        let template, k, e = 
            match a with
            | AttrUpdate(k,e) -> attr,k,e
            | LStigUpdate(k,e) -> lstig,k,e
            | EnvWrite(k,e) -> env,k,e
        let info = getInfoOrFail mapping k
        (template (translateExpr mapping e)(info.index)) +
        (updateKq <| getLstigKeys mapping e)

    let entrychecks parent entry =
        Set.filter (fun e -> e.pc = entry.pc) parent
        |> fun x -> if x.IsEmpty then Set.add entry parent else parent
        |> Set.map entrypoint
        |> String.concat "\n"

    let rec translateNode guards n = 
        let rec inner = function
        | Basic(parent, entry, a, exit, lbl) ->
            (sprintf "// %s\n   " (a.ToString()) +
                (entrychecks parent entry) + 
                guards +
                (encodeAction a) +
                (exitpoint exit))
        | Guarded(s, node) -> inner node
        | Goto(parent, entry, exit, lbl) ->  
            (guards + (entrychecks parent entry) +
                resetPcs +
                (exitpoint exit))
        | Stop(parent, entry) -> 
            (guards + (entrychecks parent entry) +
                ("term[tid] = 1;"))

        match n with
        | Guarded(s, node) ->  
            translateNode (s+guards) node
        | _ ->
            cvoid n.lbl "int tid" (indent 4 (inner n))

    trees
    |> Map.map (fun x (y, _) -> y |> Set.map (translateNode ""))
    |> Map.toSeq
    |> Seq.map snd
    |> Seq.map (String.concat "\n")
    |> Set.ofSeq
    |> String.concat "\n"
    |> printfn "%s"

    Result.Ok(sys, trees, mapping)

let translateMain typeofInterleaving (sys, trees:Map<string, Set<Node> * 'a>, mapping) =

    cvoid "monitor" "" (indent 4 (translateAlwaysProperties sys mapping))
    |> printfn "%s"

    let schedule = 
        let nodes = 
            trees
            |> Map.values 
            |> Seq.map fst
            |> Seq.concat
            |> List.ofSeq
        nodes
        |> List.mapi (fun i n ->
            let ifproc = sprintf "if (nondet_bool()) %s(choice[__LABS_step]);" n.lbl
            if i = 0 then ifproc
            else if i = nodes.Length-1 then sprintf "else %s(choice[__LABS_step]);" n.lbl
            else sprintf "else %s" ifproc 
        )
        |> String.concat "\n"

    printfn "%s"
        (tmain 
            typeofInterleaving
            schedule
            (translateFinallyProperties sys mapping))

    Result.Ok(sys, trees, mapping)
