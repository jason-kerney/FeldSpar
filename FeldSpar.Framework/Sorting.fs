namespace FeldSpar.Framework.Sorting

open System
open FeldSpar.Framework.TryParser

type Chunck = 
    | Numberic of int
    | Text of string
    
type Selector = Unknown | Number | Character


type ChunckAccumulator =
    {
        Pieces : char List;
        Type : Selector;
    }

module Sorters = 
    let chunkIt txt =
        let text = txt |> Seq.toList

        let clasify c =
            let (|IsNumber|_|) c =
                if (Char.IsNumber c) then Some(Number)
                else None
                
            let (|IsCharacter|_|) c = 
                match c with
                | IsNumber i -> None
                | _ -> Some(Character)
            
            match c with 
            | IsCharacter i -> i
            | IsNumber i -> i
            | _ -> Unknown
        
        let getChunck acc = 
            let asString (clist:char List) = System.String.Concat(clist |> Array.ofList)
        
            let parse s = 
                let (|Integer|_|) (str: string) =
                   let mutable intvalue = 0
                   if System.Int32.TryParse(str, &intvalue) then Some(intvalue)
                   else None
               
                match s with
                | Integer i -> Numberic(i)
                | _ -> Text(s)
        
            acc.Pieces |> List.rev |> asString |> parse

        let makeAccum p = { Pieces = p::[]; Type = clasify p }

        let rec chunkIt txt (current:ChunckAccumulator, acc) = 
            match txt with
            | [] -> (current |> getChunck)::acc
            | head::tail when (clasify head) = current.Type ->
                ({ current with Pieces = head::current.Pieces }, acc) |> chunkIt tail
            | head::tail when current.Type = Unknown ->
                (makeAccum head, acc) |> chunkIt tail
            | head::tail ->
                (head |> makeAccum, (current |> getChunck)::acc) |> chunkIt tail
    
        chunkIt text ({ Pieces = []; Type = Unknown }, []) |> List.rev

    let natualCompare l r = 
        let left  = l |> chunkIt
        let right = r |> chunkIt
    
        match (left, right) with
        | (a, b) when a = b -> 0
        | (a, b) when a < b -> -1
        | _ -> 1 

    let naturalSortBy (projection: 'a -> string) (items: 'a seq) : 'a seq =
        items
        |> Seq.toList
        |> List.map (fun item -> (item |> projection, item))
        |> List.sortWith (fun (keyA, _) (keyB, _) -> natualCompare keyA keyB)
        |> List.map (fun (_, item) -> item)
        |> List.toSeq
