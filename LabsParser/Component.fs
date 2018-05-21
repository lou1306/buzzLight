﻿module Components
open FParsec
open Processes
open Common

let pkeys str = 
    ws (skipString str) 
    >>. (ws EQ) 
    >>. (ws (betweenBrackets (sepbycommas (ws pinit)) >>= toMap))

let pcomp = 
    (ws (skipString "comp"))
    >>. ws IDENTIFIER
    .>>. betweenBraces (
        spaces
        >>. tuple4
            (pkeys "interface")
            (pkeys "stigmergy")
            ((ws (skipString "behavior")) >>. (ws EQ) >>. (ws IDENTIFIER))
            processes)
    |>> (fun (n, (i,l,p,procs)) -> //, //(i,l,p,procs)) -> 
        (n, {name=n; iface=i; lstig=l; behavior=p; processes=procs}))