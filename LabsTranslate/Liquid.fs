﻿module internal Liquid

open Base
open DotLiquid

type LiquidDict =
    (string * LiquidVal) seq
and LiquidVal = 
    | Str of string
    | Int of int
    | Bool of bool
    | Lst of LiquidVal seq
    | Dict of LiquidDict

let render vals (template:Template) =
    let rec hashval = function
        | Int(i) -> box i
        | Bool(b) -> box b
        | Str(s) -> box s
        | Lst(l) -> l |> Seq.map hashval |> box
        | Dict(x) -> hashdict x |> box
        and hashdict x = 
            Seq.map (fun (k,v) -> k, (hashval v)) x
            |> dict
            |> Hash.FromDictionary
    let render = template.Render (hashdict vals)
    if template.Errors.Count = 0 then 
        Result.Ok (printfn "%s" render)
    else 
        template.Errors
        |> Seq.map (fun x -> x.Message)
        |> String.concat "\n"
        |> sprintf "Code generation failed with the following message:\n%s"
        |> Result.Error

let parse path =
    readFile(path)
    |> Result.map Template.Parse

///<summmary>Opens a template file and renders it using the specified
///local variables.</summary>
let renderFile path (vals:LiquidDict) =
    parse path
    |> Result.bind (render vals)

// Reusable templates, we only parse them once
let goto = parse "templates/goto.c"
let transition = parse "templates/transition.c"
let stop = parse "templates/stop.c"