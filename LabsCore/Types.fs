module Types

type Location = 
    | I 
    | L of name:string 
    | E
    override this.ToString() =
        match this with 
            | I -> "Interface" | E -> "Environment"
            | L n -> sprintf "Stigmergy (%s)" n 


type ArithmOp =
    | Plus
    | Minus
    | Times
    | Div
    | Mod
    override this.ToString() = 
        match this with
        | Plus -> tPLUS | Minus -> tMINUS | Times -> tMUL | Div -> tDIV | Mod -> tMOD 

type UnaryOp = 
    | Abs
    | UnaryMinus
    override this.ToString() =
        match this with
        | Abs -> "__abs"
        | UnaryMinus -> "-"
type Expr<'a, 'b> =
    | Id of 'b
    | Const of int
    | Ref of Ref<'a, 'b>
    | Unary of UnaryOp * Expr<'a, 'b>
    | Arithm of Expr<'a, 'b> * ArithmOp * Expr<'a, 'b>
    override this.ToString() = 
        match this with
        | Id _ -> "id"
        | Const v -> string v
        | Ref r -> string r
        | Unary(op, e) -> 
            let s = match op with Abs -> tABS | UnaryMinus -> tMINUS in sprintf "%s(%O)" s e
        | Arithm(e1, op, e2) -> sprintf "%O %O %O" e1 op e2

    member this.visit fn compose =
        let rec visit x =
            match x with
            | Id _ 
            | Const _ 
            | Ref _ -> fn x
            | Abs e -> visit e
            | Arithm (e1, _, e2) -> compose (visit e1) (visit e2)
        visit this

and Ref<'a, 'b> = 
    {var:'a; offset: Expr<'a, 'b> option}
    override this.ToString() = 
        match this.offset with
        | Some e -> sprintf "%O[%O]" this.var e
        | None -> this.var.ToString()

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
    | True
    | False
    | Compare of Expr<'a, 'b> * CmpOp * Expr<'a, 'b>
    | Neg of BExpr<'a, 'b>
    | Compound of BExpr<'a, 'b> * Bop * BExpr<'a, 'b>
    override this.ToString() =
        match this with
        | True -> tTRUE | False -> tFALSE
        | Neg b -> sprintf "%s(%O)" tNEG b
        | Compare(e1, op, e2) -> sprintf "(%O) %O (%O)" e1 op e2
        | Compound(b1, op, b2) -> sprintf "(%O) %O (%O)" b1 op b2

type Action<'a> = {
    actionType: Location
    updates: (Ref<'a, unit> * Expr<'a, unit>) list
    }
    with 
        override this.ToString() = 
            (match this.actionType with
            | I -> sprintf "%s <- %s"
            | L _ -> sprintf "%O <~ %O"
            | E -> sprintf "%O <-- %O")
                (this.updates |> List.map (string << fst) |> String.concat ",")
                (this.updates  |> List.map (string << snd) |> String.concat ",")

type VarType = 
    | Scalar
    | Array of size:int
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


type Var = {
    name:string
    vartype:VarType
    location:Location
    init:Init
}
with override this.ToString() = this.name
type Composition =
    | Seq
    | Choice
    | Par

type Stmt<'a, 'b> = 
    | Nil 
    | Skip
    | Act of 'a Action
    | Name of string
    | Paren of Process<'a, 'b>

and Base<'a, 'b> =
    {
        stmt : Stmt<'a, 'b>
        pos : 'b
    }

and Process<'a, 'b> =
    | BaseProcess of Base<'a, 'b>
    | Guard of BExpr<'a, unit> * Process<'a, 'b> * 'b
    | Comp of Composition * Process<'a, 'b> list
