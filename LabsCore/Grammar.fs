
module LabsCore.Grammar
open Tokens
open FParsec
open FSharpPlus.Lens

type Node<'a> = {
    Name: string
    Pos: Position
    Def: 'a
}
let inline _name x =
    let getter {Name=n} = n
    let setter {Pos=p; Def=d} n' = {Name=n'; Pos=p; Def=d}
    lens getter setter x
let inline _def x =
    let getter {Def=d} = d
    let setter {Name=n; Pos=p} d' = {Name=n; Pos=p; Def=d'}
    lens getter setter x
let inline _pos x =
    let getter {Pos=p} = p
    let setter {Name=n; Def=d} p' = {Name=n; Pos=p'; Def=d}
    lens getter setter x

let inline byName v = (^T : (member Name : string) v)

type Location =
    | I 
    | L of name:string * tupleIndex: int
    | E
    override this.ToString() =
        match this with 
            | I -> "Interface" | E -> "Environment"
            | L(n, _) -> sprintf "Stigmergy (%s)" n 

type ArithmOp =
    | Plus | Minus
    | Times | Div | Mod
    | Min | Max
    override this.ToString() = 
        match this with
        | Plus -> tPLUS | Minus -> tMINUS
        | Times -> tMUL | Div -> tDIV | Mod -> tMOD
        | Min -> tMIN | Max -> tMAX

type UnaryOp = 
    | Abs | UnaryMinus
    override this.ToString() =
        match this with
        | Abs -> tABS
        | UnaryMinus -> tMINUS

type LeafExpr<'b> =
    | Id of 'b
    | Const of int
    | Extern of string
    override this.ToString() = 
        match this with
        | Id _ -> tID
        | Const v -> string v
        | Extern s -> "_" + s
type Expr<'a, 'b> =
    | Leaf of LeafExpr<'b>
    | Ref of Ref<'a, 'b>
    | Unary of UnaryOp * Expr<'a, 'b>
    | Arithm of Expr<'a, 'b> * ArithmOp * Expr<'a, 'b>
    override this.ToString() = 
        match this with
        | Leaf l -> string l
        | Ref r -> string r
        | Unary(op, e) -> 
            let s = match op with Abs -> tABS | UnaryMinus -> tMINUS in sprintf "%s(%O)" s e
        | Arithm(e1, op, e2) ->
            match op with
            | Min | Max -> sprintf "%O(%O, %O)" op e1 e2 
            | _ -> sprintf "%O %O %O" e1 op e2

and Ref<'a, 'b> = 
    {Var:'a; Offset: Expr<'a, 'b> option}
    override this.ToString() = 
        match this.Offset with
        | Some e -> sprintf "%O[%O]" this.Var e
        | None -> this.Var.ToString()

type CmpOp = 
    | Equal
    | Greater
    | Less
    | Leq
    | Geq
    | Neq
    override this.ToString() = 
        match this with
        | Less -> "<"
        | Equal -> "=="
        | Greater -> ">"
        | Leq -> "<="
        | Geq -> ">="
        | Neq -> "!="

type Bop =
    | Conj
    | Disj
    override this.ToString() = 
        match this with Conj -> tCONJ | Disj -> tDISJ

///<summmary>Boolean expressions.</summary>
type BExpr<'a, 'b> =
    | BLeaf of bool
    | Compare of Expr<'a, 'b> * CmpOp * Expr<'a, 'b>
    | Neg of BExpr<'a, 'b>
    | Compound of Bop * BExpr<'a, 'b> list
    override this.ToString() =
        match this with
        | BLeaf true -> tTRUE | BLeaf false -> tFALSE
        | Neg b -> sprintf "%s(%O)" tNEG b
        | Compare(e1, op, e2) -> sprintf "(%O) %O (%O)" e1 op e2
        | Compound(op, b) -> List.map (sprintf "%O") b |> String.concat (sprintf " %O " op)

type Action<'a> = {
    ActionType: Location
    Updates: (Ref<'a, unit> * Expr<'a, unit>) list
    }
    with 
        override this.ToString() = 
            (match this.ActionType with
            | I -> sprintf "%s <- %s"
            | L _ -> sprintf "%O <~ %O"
            | E -> sprintf "%O <-- %O")
                (this.Updates |> List.map (string << fst) |> String.concat ",")
                (this.Updates  |> List.map (string << snd) |> String.concat ",")


/// Initialization values
type Init =
     | Choose of Expr<unit,unit> list
     | Range of Expr<unit,unit> * Expr<unit,unit>
     | Undef
     override this.ToString() =
        match this with
        | Choose l -> l |> List.map (sprintf "%O") |> String.concat "," |> sprintf "[%s]"
        | Range(min, max) -> sprintf "%O..%O" min max
        | Undef -> "undef"


type Stmt<'a> = 
    | Nil 
    | Skip
    | Act of 'a Action
    | Name of string
with
    override this.ToString() =
        match this with
        | Nil -> "0"
        | Skip -> "√"
        | Act a -> string a
        | Name s -> s

type Composition =
        | Seq
        | Choice
        | Par
        
type Process<'a> =
    | BaseProcess of Node<Stmt<'a>>
    | Guard of Node<BExpr<'a, unit> * Process<'a>>
    | Comp of Composition * Process<'a> list