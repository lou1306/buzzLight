﻿[<AutoOpen>]
module Types
open Types
open Link

type Modality =
    | Always
    | Finally

type Quantifier =
    | All
    | Exists

type Property<'a> = {
        name:string
        predicate:BExpr<'a * string option, string>
        modality:Modality
        quantifiers: Map<string, string * Quantifier>
    }
    
type ComponentDef<'a> = { 
    name: string
    iface: Set<Var>
    lstig: string list
    processes: Map<string, Process<'a>>
}

type SystemDef<'a> = {
    environment: Set<Var>
    stigmergies: Map<string,Stigmergy<'a>>
    components: Map<string, ComponentDef<'a>>
    processes: Map<string, Process<'a>>
    spawn: Map<string, int*int>
    properties: Map<string, Property<'a>>
} with
    member this.SpawnedComps = 
        this.components
        |> Map.filter (fun n _ -> this.spawn.ContainsKey n)