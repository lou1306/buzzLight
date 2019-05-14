namespace Frontend
open FSharpPlus.Lens
open LabsCore
open Types
open Externs
open Message
open Outcome
open LTS
open LabsCore

type Mapping = {
    map: Map<string, Var<int> * int>
    nextI: int
    nextL: int
    nextE: int
}
with
    static member empty = {map=Map.empty; nextI=0; nextE=0; nextL=0}
    member this.Item with get(key:string) = this.map.[key]
    member this.TryFind key = this.map.TryFind key
    member this.mapvar (var:Var<int>) = 
        if this.map.ContainsKey var.name then this
        else
            let updateMap index = Map.add var.name (var, index) this.map
            let updateNext = (+) (match var.vartype with | Scalar -> 1 | Array n -> n)
            match var.location with
            | Location.I -> {this with map= updateMap this.nextI; nextI = updateNext this.nextI} 
            | Location.E -> {this with map= updateMap this.nextE; nextE = updateNext this.nextE}            
            | Location.L _ -> {this with map = updateMap this.nextL; nextL = updateNext this.nextL}

type AgentTable = {
    lts: TransitionSystem
    processes: Map<string, Process<Var<int>*int>>
    init: ExitCond
    variables: Var<int> list
    lstig: Set<string>
}
with
    static member empty =
        {
            lts=Set.empty
            processes=Map.empty
            init=Map.empty
            variables=[]
            lstig=Set.empty
        }
        
type SymbolTable = {
    spawn: Map<string, int*int>
    agents: Map<string, AgentTable>
    stigmergies: Map<string, Link<Var<int>*int>>
    processes : Map<string, Process<Var<int>*int>>
    variables: Map<string, Var<int>>
    m: Mapping
    guards: Map<Node<Stmt<Var<int>*int>>, Set<BExpr<Var<int>*int, unit>>>
}
with
    static member empty =
        {
            spawn = Map.empty
            agents = Map.empty
            stigmergies = Map.empty
            processes = Map.empty
            variables = Map.empty
            m = Mapping.empty
            guards = Map.empty
        }

module internal SymbolTable = 
    let mapVar (v:Var<_>) table =
        zero {table with m = table.m.mapvar v}

    let private processVar externs vardef =
        let toVarInt var =
            {
                location=var.location;
                name=var.name;
                vartype=match var.vartype with Scalar -> Scalar | Array e -> Array (Expr.evalCexprNoId e);
                init=var.init
            }
        
        map (Var.replaceExterns externs) vardef
        |> map toVarInt
        |> fun x ->
            match x.def.vartype with
            | Array s when s <= 0 -> 
                raise (LabsException {what=NonPositiveArraySize vardef.name; where=[vardef.pos]})
            | _ -> x
            
    let tryAddVar externs (vardef:Node<Var<Expr<unit, unit>>>) table =
        if table.variables.ContainsKey vardef.name then
            raise (LabsException {what=Generic (sprintf "Unexpected operation on variable %s" vardef.name); where=[vardef.pos]})
        else zero {table with variables = table.variables.Add(vardef.name, (processVar externs vardef).def)}
    
    /// Basic function to retrieve the mapping of variable named k
    let findString table k =
        //TODO add correct position
        table.m.TryFind k
        |> function Some x -> x | None -> raise (LabsException ({what=UndefRef k; where=[]})) 
    
    let toVarRef f r o =
        {var=f r.var; offset=o}
    let toVarExpr f e = 
        Expr.map id (toVarRef f) e
    let toVarBExpr f b =
        BExpr.map (BLeaf) (toVarExpr f) b
    
    let toVarProcess table proc =
        let toVarBase f b =    
            match b.def with 
            | Act a ->
                let newupdates =
                    a.updates
                    |> List.map (fun (r, e) -> 
                        toVarRef f r (Option.map (toVarExpr f) r.offset), toVarExpr f e)
                Act {actionType=a.actionType; updates=newupdates}
            | Nil -> Nil
            | Skip -> Skip
            | Name s -> Name s
            |> fun def' -> {def=def'; pos=b.pos; name=b.name}
        Process.map ((toVarBase (findString table)) >> BaseProcess) (toVarBExpr (findString table)) proc
    
    let setGuards proc =
        let base_ (guards, acc) b = (guards, Map.add b guards acc)
        let guard_ (guards, acc) g = (Set.add g guards, acc)
        let comp_ typ recurse (guards, acc) (l:List<_>) =
            if l.IsEmpty then (guards, acc) else
            match typ with
            | Seq ->
                // Guards only affect the first process in a sequence
                let (_, acc') = recurse (guards, acc) l.Head
                recurse (Set.empty, acc') (Comp(Seq, l.Tail)) 
            | _ ->
                Seq.map (recurse (guards, acc)) l
                |> Seq.reduce(fun (_, a1) (_, a2) -> Set.empty, Map.union a2 a1)
        Process.fold base_ guard_ comp_ (Set.empty, Map.empty) proc                    
        |> snd
    
    let tryAddProcess externs (p: Node<Process<_>>) table =
        let p' = map (Process.simplify >> toVarProcess table >> Process.replaceExterns externs) p
        zero {table with processes=Map.add p.name p'.def table.processes; guards=Map.union table.guards (setGuards p'.def)}
    
    let tryAddStigmergy externs (s: Node<Stigmergy<string>>) table =
        let link = map (BExpr.replaceExterns externs >> toVarBExpr (fun (x,y) -> findString table x, y)) s.def.link
        zero {table with stigmergies=table.stigmergies.Add(s.name, link.def)}
    
    let tryAddIface externs (a:Node<Agent>) table =
        let iface = List.map (processVar externs) a.def.iface |> List.map (fun x -> x.def)
        fold mapVar iface table
        <~> fun t -> zero {t with agents = table.agents.Add(a.name, {AgentTable.empty with variables=iface})}
    
    let tryAddAgent externs (a:Node<Agent>) (table, state) =
        map (Process.simplify >> toVarProcess table >> Process.replaceExterns externs)
        |> fun f -> List.map f a.def.processes
        |> List.map (fun node -> node.name, node.def)
        |> (Map.ofList >> zero)
        <~> fun p ->
            let allProcesses = Map.union p table.processes
//            try
            let p' = (Map.add "Behavior" (Process.expand allProcesses "Behavior") allProcesses)
            
            let (lts, acc), initCond = LTS.makeTransitions state p'.["Behavior"]
            Set.map (fun t -> eprintfn "<%A %s %A>" t.entry t.action.name t.exit) lts |> ignore
            let lts' = LTS.removeNames p' lts
            Set.map (fun t -> eprintfn "<<%A %s %A>>" t.entry t.action.name t.exit) lts' |> ignore
            let guards = Map.union table.guards (setGuards p'.["Behavior"])
            let agent = {table.agents.[a.name] with processes=p'; lts=lts'; init=initCond; lstig=a.def.lstig |> Set.ofList}
            zero ({table with agents = table.agents.Add(a.name, agent); guards=guards}, (Set.empty, acc))
//            with err -> wrap (table, state) [] [{what=UndefProcess err.Message; where=[a.pos]}] //TODO add correct position        
    
    let makeSpawnRanges externs spawn table =
        let makeRanges mp =
            mp 
            |> Map.fold (fun (c, m) name d -> let c' = c + d.def in (c', (Map.add name (c, c') m) )) (0, Map.empty) 
            |> snd
        
        let spawn' =
            spawn
            |> List.map (fun (d:Node<_>) -> d.name, d)
            |> Map.ofList
            |> Map.mapValues (map (Expr.replaceExterns externs >> Expr.evalCexprNoId ))
                     
        let valid, others = Map.partition (fun _ d -> d.def > 0) spawn'
        let zeroes, negatives = Map.partition (fun _ d -> d.def = 0) others 
        let warnings =
            zeroes |> Map.mapValues (fun d -> {what=SpawnZero d.name; where=[d.pos]}) |> Map.values
        let errors =
            negatives |> Map.mapValues (fun d -> {what=NegativeSpawn d.name; where=[d.pos]}) |> Map.values

        wrap {table with SymbolTable.spawn=makeRanges valid} (List.ofSeq warnings) (List.ofSeq errors)                     