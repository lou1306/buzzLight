module Frontend.LTS
open LabsCore
open Types
open FSharpPlus.Lens

type ExecPoint = Map<int, int>
type EntryCond = Map<int,int>
type ExitCond = Map<int,Set<int>>
type Transition = { entry: EntryCond; siblings: Set<int>; last: bool; exit: ExitCond; action: Node<Stmt<int*Location>>}
type TransitionSystem = Set<Transition>

type State = {
    lts: TransitionSystem
    initCond: ExitCond
}

module internal ExitCond =
    let ofEntryConds s =
        Seq.fold (
            Map.fold (fun (st:Map<_,_>) k v ->
                match st.TryFind k with
                | Some oldset -> Map.add k (Set.add v oldset) st
                | None -> Map.add k (Set.singleton v) st
            )    
        ) Map.empty s
        
    let remove (entry:EntryCond) (exit:ExitCond) =
        Map.fold (fun ex pc v -> Map.add pc (Set.remove v ex.[pc]) ex) exit entry
    
/// Returns the disjunction of two exit points.
let (<||>) e1 e2 =
    Map.unionWith (fun _ s1 s2 -> Set.union s1 s2) e1 e2

let (|-) (entry:EntryCond) (exit:ExitCond) =
    Map.toSeq entry
    |> Seq.forall (fun (pc, v) ->
        match exit.TryFind pc with
        | Some s -> Set.contains v s || (Set.contains -1 s && v <> 0)
        | None -> true)
    
module ExecPoint =      
    let empty = [(0, 0)] |> Map.ofList
    
    let freshpc k v =
        match Map.tryFind k v with
        | Some x -> let x' = x+1 in (Map.add k (x') v), x'
        | None -> (Map.add k (1) v), 1
    
    let newpc n v =
        let maxpc = Map.keys v |> Seq.max
        let newpcs = Set.ofSeq {maxpc+1 .. maxpc + n}
        newpcs
        |> Set.map (fun n -> n, 1)
        |> Map.ofSeq
        |> fun v' -> Map.union v v', newpcs

let removeNames (procs:Map<string,Process<_>>) lts =
    let removeName (l:TransitionSystem) tr =
        let name =
            match tr.action.def with
            | Name s -> s
            | _ -> failwith "Something wrong happened"
        eprintfn "<<%s>> <<%A>>" name (Map.keys procs)
        let inits = Process.initial procs.[name]
        let newexit =
            l
            |> Set.filter (fun t -> inits.Contains t.action)
            |> Set.map (fun t -> t.entry)
            |> ExitCond.ofEntryConds
        
        let affected, others =
            l
            |> Set.remove tr
            |> Set.partition (fun t -> tr.entry |- t.exit)  
        
        affected
        |> Set.map (fun t -> {t with exit= (ExitCond.remove tr.entry t.exit) <||> newexit })
        |> Set.union others

    lts
    |> Set.filter (fun t -> match t.action.def with Name _ -> true | _ -> false)
    |> Seq.fold removeName lts
    
let makeTransitions state proc =
    let mutable (initCond:ExitCond) = Map.empty
    let initials = Process.initial proc
    
    let base_ acc b =
        let lts, (k, v, parent, exit) = acc
        let v', vk = ExecPoint.freshpc k v
        let entry = (Map.add k vk parent)
        (* If b is an initial action of procs, add its entry condition to initCond *)
        initCond <- (initCond) <||> (if initials.Contains b then ExitCond.ofEntryConds [entry] else Map.empty)
        let lts' = (Set.add {entry=entry; exit=exit; action=b; siblings=Set.empty; last=false}) lts
        lts', (setl _2 v' (snd acc))
    let rec comp_ typ recurse acc l =
        let lts = fst acc
        match typ with
        | Seq ->
            let p = List.last l
            let lts', acc' = recurse (Set.empty, snd acc) p
            let exit': Map<int, Set<int>> =
                Set.filter (fun t -> (Process.initial p).Contains t.action) lts
                |> Set.map (fun t -> t.entry)
                |> ExitCond.ofEntryConds
            let acc'' = setl _4 exit' acc' 
            match l with
            | []
            | _::[] -> lts', acc''
            (* recurse on l without its last element*)
            | _ -> comp_ Seq recurse (Set.union lts lts', acc'') ((List.rev << List.tail << List.rev) l)
        | Choice ->
            Seq.fold recurse acc l
        | Par ->
            let lts, (k, v, parent, exit) = acc
            let v', fresh = ExecPoint.freshpc k v
            let v'', newpcs = ExecPoint.newpc (l.Length) v'
            
            let childRecurse (l, a) child =
                let siblings = Set.filter ((<>) (a^._1)) newpcs
                let lts', acc' = recurse (Set.empty, a) child
                let last, others = Set.partition (fun t -> (Process.final child).Contains t.action) lts'
                Set.map (
                    fun t ->
                        let t' = {t with siblings=siblings}
                        Set.singleton t' |> Set.add {t' with last=true}
                    ) last
                |> Set.unionMany
                |> Set.union others
                |> fun x -> Set.union l x, acc'
            
            let acc' = setl _2 v'' (snd acc) |> over _3 (Map.add fresh k) (* update parent *)
            
            let children = Seq.zip newpcs l
            Seq.fold (fun (l, a) (k', p) -> childRecurse (l, (setl _1 k' a)) p) (Set.empty, acc') children
                
    Process.fold base_ (fun i _ -> i) comp_ state proc, initCond
    