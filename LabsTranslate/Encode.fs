﻿module internal Encode
open Types
open Link
open Base
open Templates
open Expressions
open Properties
open Liquid

type NodeType = Goto | Stop

type Node = {
    nodeType:NodeType
    action : Action<Var * int> option
    label:string
    guards:Set<BExpr<Var * int, unit>>
    entry: Set<int*int>
    exit: Set<int*int>
} with 
    member this.lbl = 
        let pc = 
            this.entry
            |> Set.map (fun (pc, v) -> sprintf "%i_%i" pc v)
            |> String.concat "_"
        match this.nodeType with
        | Goto _ -> sprintf "%s_%s" this.label pc
        | Stop _ -> sprintf "Stop_%A" pc

let rec getEntryPoints (procs:Map<string, Process<_,_>>) counter name =
    let pccount = makeCounter -1
    let rec visit visited pc cnt parent p =
        let v = visit visited pc cnt parent
        match p with
        | Nil -> Map.empty
        | Skip _ 
        | Base _ -> [p, Set.add (pc, cnt()) parent] |> Map.ofList
        | Seq(p1, p2) //-> Map.merge (visit pc cnt parent p1) (visit pc cnt Set.empty p2)
        | Choice(p1, p2) -> Map.mergeIfDisjoint (v p1) (v p2)
        | Par(p1, p2) -> 
            let leftPc, leftCnt = pccount(), makeCounter -1
            let rightPc, rightCnt = pccount(), makeCounter -1
            let par = parent.Add (pc, cnt())
            Map.merge (visit visited leftPc leftCnt par p1) (visit visited rightPc rightCnt par p2)
            // Add entry point for join node
            |> Map.add p ([leftPc, leftCnt(); rightPc, rightCnt()] |> Set.ofList |> Set.union parent)
        | Await(_,p) -> v p
        | Name (s, _) when Set.contains s visited -> Map.empty
        | Name (s, _) ->
            visit (Set.add s visited) pc cnt parent procs.[s]

    let entryMap = visit (Set.singleton name) (pccount()) counter Set.empty procs.[name]

    let rec entryOf p =
        match p with
        | Nil -> Set.empty
        | Skip _
        | Base _ -> 
            entryMap.[p]
        | Await(_, p)
        | Seq(p, _) -> entryOf p
        | Choice(p1, p2)
        | Par(p1, p2) ->
            Set.union (entryOf p1) (entryOf p2)
        | Name (s, _) -> entryOf procs.[s]

    let rec joinOf proc = 
        match proc with
        | Par(_,_) ->
            entryMap.[proc]
        | _ -> Set.empty

    entryOf, joinOf

let encodeProcess (procs:Map<string, Process<_,_>>) counter rootName =
    let entryOf, joinOf = getEntryPoints procs counter rootName

    let rec visit visited guards p =
        let node = 
            {
                action=None
                guards=guards
                entry=Set.empty
                nodeType=Goto
                exit=Set.empty; label=rootName
            }
        match p with
        | Nil -> Set.empty
        | Skip _ -> Set.singleton {node with entry=entryOf p}
        | Base (a, _) -> 
            {node with action=Some a; entry=entryOf p} |> Set.singleton
        | Seq(p1, p2) ->
            let p1exit = entryOf p2
            let visitP2 = visit visited Set.empty p2
            visit visited guards p1
            |> Set.map (fun n -> if n.exit = Set.empty then {n with exit=p1exit} else n)
            |> Set.union visitP2
        | Choice(p1, p2) ->
            visit visited guards p1 
            |> Set.union (visit visited guards p2)
        | Par(p1, p2) ->
            let joinEntry = joinOf p
            let mapToJoin n =
                if n.exit = Set.empty then
                    // n must only set its own program counter
                    let pcs = n.entry |> Set.map fst
                    let ex = Set.filter (fun (p, _) -> pcs.Contains p) joinEntry
                    {n with exit=ex}
                else n
            let leftVisit = 
                visit visited guards p1 |> Set.map mapToJoin
            let rightVisit =
                visit visited guards p2 |> Set.map mapToJoin
            leftVisit
            |> Set.union rightVisit
            |> Set.add {node with entry=joinEntry}
            
        | Name (s, _) when Map.containsKey s visited ->
            Set.empty
        | Name (s, _) ->
            let e = entryOf procs.[s]
            let newVisited = Map.add s e visited
            visit newVisited guards procs.[s]
         | Await(b, p) ->
             visit visited (Set.add b guards) p

    visit Map.empty Set.empty procs.[rootName], (entryOf procs.[rootName])

let encode (sys:SystemDef<_>) = 
    if sys.SpawnedComps.IsEmpty then failwith "No components have been spawned!"
    let counter = makeCounter -1

    sys.SpawnedComps
    |> Map.mapValues (fun def -> (def, Map.merge sys.processes def.processes))
    |> Map.mapValues (fun (_, procs) -> encodeProcess procs counter "Behavior")
    |> fun x -> Ok (sys, x)

let translateHeader isSimulation ((sys, trees, mapping:KeyMapping, maxI, maxL, maxE), bound) =

    let (tupleStart, tupleEnd), maxTuple =
        /// Finds the min and max indexes of the given tuple.
        let extrema (tup:Set<Var>) =
            let indexes = 
                tup
                |> Set.map (fun v -> mapping.[v.name])
            let endIndexes = 
                tup
                |> Set.map (fun v ->
                    match v.vartype with
                    | Scalar -> 0
                    | Array n -> n - 1 
                    |> (+) mapping.[v.name])

            (Seq.min indexes, Seq.max endIndexes)
        let makeTuple (tup: Set<Var>) =
            let min, max = extrema tup
            List.replicate (max-min+1) (min, max), max-min

        sys.stigmergies
        |> Map.values
        |> Seq.map (fun s -> s.vars)
        |> Seq.collect (List.map (makeTuple))
        |> Seq.reduce (fun (l1,s1) (l2,s2) -> List.append l1 l2, max s1 s2)
        |> fun (lst, size) -> List.unzip lst, size+1

    // Find the number of PCs used in the program
    let maxPc =
        let getPc node = 
            node.entry |> Set.map fst |> Set.maxElement

        trees
        |> Map.values
        |> Seq.collect fst
        |> Seq.map getPc
        |> Seq.max
        |> (+) 1

    let maxcomps = 
        Map.fold (fun state _ (_, cmax) -> max state cmax) 0 sys.spawn
        
    let bitwidth num = 
        System.Math.Log(float num, 2.) |> int |> (+) 1

    let defines = 
        [
            "BOUND", bound; 
            "MAXCOMPONENTS", maxcomps;
            "MAXPC", maxPc;
            "MAXKEYI", maxI
            "MAXKEYL", maxL
            "MAXKEYE", maxE
            "MAXTUPLE", maxTuple
        ]
        |> (if isSimulation then fun l -> ("SIMULATION", 1)::l else id)
        |> List.map (fun (a,b) -> Dict ["name", Str a; "value", Int b])

    let typedefs =
        [
            "TYPEOFVALUES", "short"
            "TYPEOFPC", "unsigned char"
            "TYPEOFTIME", "unsigned char" 
            "TYPEOFAGENTID", sprintf "unsigned __CPROVER_bitvector[%i]" (bitwidth maxcomps)
            "TYPEOFKEYIID", sprintf "unsigned __CPROVER_bitvector[%i]" (bitwidth maxI)
            "TYPEOFKEYLID", sprintf "unsigned __CPROVER_bitvector[%i]" (bitwidth maxL)
            "TYPEOFKEYEID", sprintf "unsigned __CPROVER_bitvector[%i]" (bitwidth maxE)
        ]
        |> List.map (fun (a,b) -> Dict ["name", Str a; "value", Str b])

    let links =
        let makeLink (s:Stigmergy<Var*int>) = 
            let names =
                s.vars
                |> Set.unionMany
                |> Set.map (fun v -> v.name)
            let m = mapping |> Map.filter (fun k _ -> names.Contains k)
            if m.IsEmpty then None else
            Dict [
                "start", Int (Map.values m |> Seq.min)
                "end", Int (Map.values m |> Seq.max)
                "link", s.link |> linkExpr.BExprTranslator (sprintf "Stigmergy %s" s.name) |> Str
            ] |> Some

        sys.stigmergies
        |> Map.values
        |> Seq.choose makeLink

    [
        "defines", Lst defines
        "typedefs", Lst typedefs
        "links", Lst links
        "tupleStart", tupleStart |> Seq.map (Str << string) |> Lst
        "tupleEnd", tupleEnd |> Seq.map (Str << string) |> Lst
    ]
    |> renderFile "templates/header.c"

let translateGuard = procExpr.BExprTranslator


let liquidGuards tid node = 
    node.guards 
    |> Seq.map ((customProcExpr tid).BExprTranslator node.label)
    |> Seq.map Str

let liquidEntry tid node = 
    let translatePc = sprintf "(pc[%s][%i] == %i)" tid
    node.entry 
    |> Set.map (fun (a, b) -> translatePc a b)
    |> Seq.map Str

let translateCanProceed trees =
    let liquidAll tid n = 
        liquidEntry tid n
        |> Seq.append (liquidGuards tid n)
        |> Lst

    trees
    |> Map.values
    |> Seq.collect fst
    |> Seq.map (liquidAll "tid")
    |> Seq.distinct
    |> fun x -> seq ["guards", Lst x]
    |> renderFile "templates/canProceed.c"

let translateAll (trees:Map<'b, (Set<Node> * 'c)>) =
    let doOffset = function
        | Some e -> procExpr.ExprTranslator "" e |> Str
        | None -> Int 0

    let liquid guards a =
        let template =
            match a.actionType with
            | I -> "attr"
            | L _ -> "lstig"
            | E -> "env"

        let rec guardkeys = function
            | False | True -> Set.empty
            | Neg b -> guardkeys b
            | Compound(b1,_,b2) -> guardkeys b1 |> Set.union (guardkeys b2)
            | Compare(e1,_,e2) -> (getLstigVars e1) |> Set.union (getLstigVars e2)
            

        let qrykeys =
            a.updates
            |> List.map (getLstigVars << snd)
            |> Set.unionMany
            |> Set.union (guards |> Set.map guardkeys |> Set.unionMany)
            |> Seq.map (Int << snd)
            |> Lst

        let liquidAssignment (k:Ref<Var*int, unit>, expr) =
            let size = match (fst k.var).vartype with Array s -> s | _ -> 0
            seq [
                "key",  Int (snd k.var)
                "offset", doOffset k.offset
                "size", Int size
                "expr", procExpr.ExprTranslator (string a) expr |> Str
            ]
            |> Dict

        let g = 
            if Set.isEmpty guards then ""
            else 
                guards
                |> Set.map (sprintf "%O")
                |> String.concat " && "
                |> fun s -> s + " -> " 
        [
            "labs", Str (string a |> (+) g)
            "type", Str template
            "qrykeys", qrykeys
            "assignments", a.updates |> Seq.map liquidAssignment |> Lst
        ]

    let newTranslate (n:Node) = 

        let template = 
            match n.nodeType with
            //| Basic a -> transition, liquid a n.guards
            | Goto -> goto
            | Stop -> stop

        let liquidPcs pcset =
            pcset |> Set.toList |> List.groupBy fst
            |> Seq.map (fun (pc, vals) -> Dict[ "pc", Int pc; "values", Lst (List.map (Int << snd) vals) ]) 


        n.action
        |> Option.map (liquid n.guards)
        |> Option.defaultValue List.empty
        |> List.append [
            "label", Str n.lbl
            "entrypoints", liquidPcs n.entry |> Lst
            "exitpoints", liquidPcs n.exit |> Lst
            "guards",
                n.guards
                |> Set.map (procExpr.BExprTranslator n.label)
                |> Seq.map Str
                |> Lst
        ]
        |> strRender
        |> fun x -> Result.bind x template |> Result.mapError (failwith)

    trees
    |> Map.values
    |> Seq.collect fst
    |> Set.ofSeq
    |> Seq.map newTranslate
    |> Seq.reduce (fun r1 r2 -> r1 >+> r2 |> Result.map (fun (s1:string, s2) -> s1 + s2) )

let translateMain fair (sys:SystemDef<Var*int>, trees:Map<string, Set<Node> * 'a>) =
    let nodes = 
        trees
        |> Map.values 
        |> Seq.collect fst
    let finallyP, alwaysP =
        sys.properties
        |> Map.partition (
            fun _ {modality=m} -> 
                match m with Finally -> true | Always -> false)
    let translateProperties props =
        props
        |> Map.mapValues (translateProp sys)
        |> Map.values
        |> String.concat "\n"


    let scheduleElement (n:Node) =
        Dict [
            "name", Str n.lbl
            "entry", liquidEntry "firstAgent" n |> Lst
            "guards", liquidGuards "firstAgent" n |> Lst
        ]

    [
        "firstagent", if sys.spawn.Count = 1 then Int 0 else Int -1
        "fair", Bool fair;
        "schedule", nodes |> Seq.map scheduleElement |> Lst
        "alwaysasserts",
            alwaysP
            |> translateProperties
            |> indent 4 |> Str
        "finallyasserts",  
            finallyP
            |> translateProperties
            |> indent 4 |> Str
    ]
    |> renderFile "templates/main.c"
