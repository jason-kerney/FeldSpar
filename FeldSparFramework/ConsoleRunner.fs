namespace FeldSpar.Framework
open FeldSpar.Framework.Engine
open FeldSpar.Framework.TestSummaryUtilities
open System

module ConsoleRunner =
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

    let getAssemblyName (testAssembly : Reflection.Assembly) =
        testAssembly.FullName.Split([|','|]).[0]
        
    let runAndReport testAssembly = 
        let name = testAssembly |> getAssemblyName 
        let tests = testAssembly |> runTestsAndReport reportConsoleColorForResult
        
        let failedTests = tests
                            |> reduceToFailures 
                            |> List.ofSeq

        printfn "Displaying Results (%d Failed of %d)" (failedTests |> List.length) (tests |> List.length)

        failedTests 
            |> reportResults 
            |> seperateResults
            |> printReports

        (name, tests)
