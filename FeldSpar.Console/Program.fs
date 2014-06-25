namespace FeldSpar.Console
open System
open FeldSpar.Framework
open FeldSpar.Framework.TestSummaryUtilities
open FeldSpar.Framework.Engine
open FeldSpar.Framework.Verification.ApprovalsSupport;

open FeldSpar.Console.Helpers.Data
open FeldSpar.Console.Tests.BuildingOfTestsTests
open FeldSpar.Console.Tests.IsolationTests
open FeldSpar.Console.Tests.FilteringTests
open FeldSpar.Console.Tests.StandardsVerificationTests
open ApprovalTests;

open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns

module Program =
    let getConsoleColor status =
        match status with
        | Found(_) -> ConsoleColor.Gray
        | Running(_) -> ConsoleColor.Blue
        | Finished(_, result) ->
            match result with
            | Success -> ConsoleColor.Green
            | Failure(ExceptionFailure(_)) -> ConsoleColor.Magenta
            | Failure(Ignored(_)) -> ConsoleColor.DarkRed
            | _ -> ConsoleColor.Red

    let reportConsoleColorForResult status = 
        let oColor = Console.ForegroundColor
        let nColor = getConsoleColor status
        do Console.ForegroundColor <- nColor

        match status with
        | Found(token) -> 
            printfn "\t\tFound: '%s'" token.Name
        | Running(token) ->
            printfn "\t\tRunning: '%s'" token.Name
        | Finished(token, result) -> 
            printfn "\t\tFinished: '%s'" token.Name

        do Console.ForegroundColor <- oColor

    [<EntryPoint>]
    let public main argv = 
        (*
        let theories =
            let searchFilter = (typeof<Theory<_>>.GetGenericTypeDefinition ())

            let mi = typeof<Theory<_>>.Assembly.GetExportedTypes() 
                        |> Seq.map(fun t -> t.GetMethods ()) 
                        |> Seq.concat 
                        |> Seq.filter (fun m -> m.Name = "convertTheoryToTests") 
                        |> Seq.head

            printfn "%A" fu

            testAssembly.GetExportedTypes()
            |> Seq.map(fun t -> t.GetProperties(Reflection.BindingFlags.Public ||| Reflection.BindingFlags.Static))
            |> Seq.concat
            |> Seq.filter(fun t -> t.PropertyType.IsGenericType)
            |> Seq.filter(fun t -> t.PropertyType.GetGenericTypeDefinition () = searchFilter)
            |> Seq.map(fun t -> 
                        let g = t.PropertyType.GetGenericArguments() 
                        let genericC = mi.MakeGenericMethod(g)
                        genericC.Invoke(null, [|t.GetValue(null); t.Name|])
            )
            |> Seq.iter(fun v -> printfn "%A" v)

        //*)            
        //(*
        printfn "Running Tests"

        let formatResults result = 
            sprintf "\t\t%A" result

        let printReports results =
            let oColor = Console.ForegroundColor
            Console.ForegroundColor <- System.ConsoleColor.Red
            
            results |> Seq.iter (fun report -> printfn "%s" report)
            
            Console.ForegroundColor <- oColor

        let seperateResults (results: string seq) = 
            results |> Seq.map (
                                    fun result ->
                                        let s = ""
                                        let sep = s.PadLeft(24, '_')
                                        result + Environment.NewLine + sep + Environment.NewLine
                                )

        let tests = testAssembly |> runTestsAndReport reportConsoleColorForResult

        let failedTests = tests
                            |> reduceToFailures 
                            |> List.ofSeq

        printfn "Displaying Results (%d Failed of %d)" (failedTests |> List.length) (tests |> List.length)

        failedTests 
            |> reportResults 
            |> seperateResults
            |> printReports

        printfn "Done!"

        //*)

        Console.ReadKey true |> ignore
        0
