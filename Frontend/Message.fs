module Frontend.Message
open Types

type Position = FParsec.Position

type Err =
    | Parser of string
    | Duplicate of string
    | UndefProcess of string
    | UndefRef of string
    | UndefBehavior of string
    | UndefAgent of string
    | NoValueForExtern of string
    | NegativeSpawn of string
    | NonPositiveArraySize of string
    | Codegen of string
    | Generic of string
    | CLI of string

type Warn =
    | SpawnZero of string
    | Unused of string //TODO

type Message<'a> =
    {
        what: 'a
        where: Position list
    }

let private _pprintWithPos header msg where =
    let pos = where |> List.map string |> String.concat "; "
    sprintf "[%s] %s at %s" header msg pos
    
let pprintErr (m:Message<Err>) =
    match m.what with
        | Parser s -> sprintf "Parser failed: %s" s
        | Duplicate s -> sprintf "Duplicate definitions for '%s'" s
        | UndefProcess s -> sprintf "Process '%s' was not defined" s
        | UndefAgent s -> sprintf "Agent '%s' was not defined" s
        | UndefBehavior s -> sprintf "Behavior of agent '%s' was not defined" s
        | UndefRef s -> sprintf "Identifier '%s' was not defined" s
        | NegativeSpawn s -> sprintf "Cannot spawn a negative number of agents '%s'" s
        | NoValueForExtern s -> sprintf "No value was given for extern parameter '%s'" s
        | NonPositiveArraySize s -> sprintf "Array '%s' must have positive size" s
        | Codegen s -> sprintf "Code generation failed: %s" s
        | CLI s -> sprintf "Parsing of the command line failed: %s" s
        | Generic s -> s
    |> fun msg -> _pprintWithPos "ERROR" msg m.where

let pprintWarn (m:Message<Warn>) =
    match m.what with
        | SpawnZero s -> sprintf "Agent '%s' has spawn size 0 and will not be spawned." s
        | Unused s -> sprintf "Unused: '%s'" s
    |> fun msg -> _pprintWithPos "WARNING" msg m.where

exception LabsException of Message<Err>

let map f (d:Node<_>) =
    try {pos=d.pos; name=d.name; def=f d.def}
    with :? LabsException as e -> raise (LabsException {e.Data0 with where=[d.pos]})
    
