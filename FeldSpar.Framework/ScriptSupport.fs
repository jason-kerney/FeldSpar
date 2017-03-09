namespace FeldSpar.Framework

open FeldSpar.Framework.ConsoleRunner
open FeldSpar.Framework.Engine
open FeldSpar.Framework.Formatters.Basic
open System
open System.Reflection

module ScriptSupport = 
    /// <summary>
    /// Current Data type representing tests. This may change and should not be used directly
    /// </summary>
    type ScriptTestData =
        {
            Name: string
            Case : Test
        }  
        
    /// <summary>
    /// Sets up a test in a scripting environment
    /// </summary>
    /// <param name="testFunction">the test code</param>
    /// <param name="name">the name of the test</param>
    let testedWith testFunction name = 
        {
            Name = name
            Case = Test(testFunction)
        }

    /// <summary>
    /// Converts a script test into TestInformation
    /// </summary>
    /// <param name="data">the script test data</param>
    let asTest (data : ScriptTestData) = 
        {
            TestContainerName = "Script"
            TestName = data.Name
            Test = data.Case
        }

    /// <summary>
    /// Converts a list of ScriptTestData into a list of TestInformations
    /// </summary>
    /// <param name="data">the list of script test data</param>
    let asTests (data : ScriptTestData list) =
        data |> List.map asTest |> List.toArray

    /// <summary>
    /// Shows the failing test results in the console with coloring as the tests execute
    /// </summary>
    /// <param name="results">the current status representing the state of test execution</param>
    let displayResultsInConsole results =
        let foreColor = Console.ForegroundColor
        let backColor = Console.BackgroundColor
        do Console.ForegroundColor <- getConsoleColor results
        do Console.BackgroundColor <- ConsoleColor.White

        let join (textLines : string list) = String.Join ("\n", textLines)
        let split (text : string) = text.Split ([|"\n"; "\r"|], StringSplitOptions.RemoveEmptyEntries) |> Array.toList
        let rec indentLines count textLines =
            if count <= 0 then textLines
            else
                textLines 
                    |> List.map (sprintf "\t%s")
                    |> indentLines (count - 1)
    
        let indent =
            split
            >> indentLines 2
            >> join

        let report testName =
            printResult ""
            >> indent
            >> printfn "%s\n%s" testName

        match results with
        | Finished ({TestName = testName}, testResult) ->
            match testResult with
            | Failure(reason) -> 
                testResult |> report testName
            | _ -> ()
        | _ -> ()
        
        do Console.ForegroundColor <- foreColor
        do Console.BackgroundColor <- backColor

    /// <summary>
    /// Runs the scripted tests
    /// </summary>
    /// <param name="config">the config to use when generating the test Environment</param>
    /// <param name="executionReporter">the reporter to use to report test execution</param>
    /// <param name="tests">the scripted tests</param>
    let runTests config executionReporter tests =
        let assembly = Assembly.GetExecutingAssembly ()

        let assemblyPath =
            let trace = System.Diagnostics.StackTrace(true)
            let frame = trace.GetFrames ()  |> Seq.filter (fun frame -> (frame.GetMethod ()).DeclaringType.Assembly = assembly) |> Seq.head
        
            frame.GetFileName ()

        let token = { new IToken with
                        member this.AssemblyPath = assemblyPath
                        member this.AssemblyName = System.IO.Path.GetFileName assemblyPath
                        member this.Assembly = assembly
                        member this.IsDebugging = false
                        member this.GetExportedTypes () = [||]
                    }


        tests
            |> asTests
            |> shuffleTests
            |> buildTestPlan config executionReporter token
            |> List.map(
                    fun { TestName = _; TestCase = test } -> 
                        let result = test()
                        result.TestResults |> fileFinishedReport result.TestName executionReporter
                        result
                    )

    /// <summary>
    /// Runs the scripted tests with a predefined console output
    /// </summary>
    /// <param name="config">the config to use when generating the test environment</param>
    /// <param name="tests">the scripted tests</param>
    let runTestsAndReport config tests =
        tests
        |> runTests config displayResultsInConsole
        |> fun summary -> 
            let total = summary |> List.length
            let failed = 
                summary 
                |> List.filter (fun s -> match s.TestResults with | Failure(_) -> true | _ -> false)
                |> List.length
        
            let makePlural cnt = if cnt = 1 then String.Empty else "s"

            let totalPlural = makePlural total
            let failPlural = makePlural failed
            printfn "%d failed test%s out of %d test%s" failed failPlural total totalPlural

            summary