namespace FeldSpar.Console.Tests
open FeldSpar.Console.Helpers.Data
open FeldSpar.Framework
open FeldSpar.Framework.Verification
open FeldSpar.Framework.Verification.ChecksClean
open FeldSpar.Framework.TestSummaryUtilities
open FeldSpar.Framework.Engine
open FeldSpar.Console
open System.Text

module ConsoleTests = 
    let summary1 = 
        ("My Summary 1", 
            ([ 
                { TestName = "Fake test 3"; TestCanonicalizedName = "Fake_Test"; TestResults = Failure(GeneralFailure("this is a failing test")); };
                { TestName = "Fake test 1"; TestCanonicalizedName = "Fake_Test"; TestResults = Success; };
                { TestName = "Fake test 2"; TestCanonicalizedName = "Fake_Test"; TestResults = Failure(Ignored("this is an ignored test")); };
            ] |> Seq.ofList)
        )

    let summary2 =
        ("My Summary 2", 
            ([ 
                { TestName = "Faker test 3"; TestCanonicalizedName = "Fake_Test"; TestResults = Failure(GeneralFailure("this is a failing test")); };
                { TestName = "Faker test 1"; TestCanonicalizedName = "Fake_Test"; TestResults = Success; };
                { TestName = "Faker test 2"; TestCanonicalizedName = "Fake_Test"; TestResults = Failure(Ignored("this is an ignored test")); };
            ] |> Seq.ofList)
        )

    let testSummaries = 
        [
            summary1;
            summary2;
        ]

    let ``Test all Verbosity levels`` = 
        Test(
            fun env -> 
                vebosityLevels |> checkAllAgainstStandardCleaned env
        )

    let ``Compare Verbosity only excepts specific modifiers`` =
        Test(
            fun env ->
                let values = ["Max"; "Results"; "Errors"; "Detail"; "maxx"; "max"; "results"; "errors"; "detail"; "MAX"; "RESULTS"; "ERRORS"; "DETAIL"]
                let max = (values |> List.map (fun f -> f.Length) |> List.max) + 1
                let results = values |> List.map (fun value -> sprintf "'%s'%s%s a verbosity setting" value (String.replicate (max - (value.Length)) " ") (match compareVerbosity value with true -> "is" | false -> "isn't"))

                results |> checkAllAgainstStandardCleaned env
        )

    let ``saveResults calls a saver after converting results to JSON`` =
        Test(
            fun env ->
                let sb = new StringBuilder()
                let saver : string -> string -> unit =
                    fun path data -> sb.Append(sprintf "Path: '%s'\nValues ->\n%s" path data) |> ignore

                let path = @"Path:\Somewhere"

                saveResults saver path testSummaries
                sb.ToString() |> checkAgainstStringStandardCleaned env
        )

    let ``maybeSaveResults dose not call saver if no path is given`` =
        Test(
            fun _ ->
                let result = ref Success
                let saver : string -> (string * 'a list) list -> unit =
                    fun _ _ -> result := Failure(GeneralFailure("Should not have called saver"))

                let path : string option = None

                maybeSaveResults path saver []

                !result
        )

    let ``maybeSaveResults dose call saver if path is given`` =
        Test(
            fun env ->
                let result = ref ""
                let saver : string -> (string * 'a list) list -> unit = 
                    fun path results -> 
                        let r = (sprintf "'%s'\n%A" path results)
                        result := r
                
                let path = Some("My:\Path")
                
                let test = 
                    testSummaries |> List.map (fun (assembly, summaries) -> (assembly, summaries |> List.ofSeq))
                
                maybeSaveResults path saver test
                
                !result |> checkAgainstStringStandardCleaned env
        )

    let ``runTestsAndSaveResults runs the tests, saves the test results, and returns the test results`` =
        Test(
            fun env ->
                let sb = new StringBuilder()
                let saver:(string * #seq<ExecutionSummary>) list -> unit = 
                    fun results -> sb.Append(sprintf "%A\n" results) |> ignore
                let runner = 
                    fun (token:IToken) -> 
                        sb.Append(sprintf "Running: %s\n" token.AssemblyPath) |> ignore
                        summary1
                
                let token = [{ new IToken with
                                        member this.AssemblyPath = @"My:\Assmembly\Path";
                                        member this.AssemblyName = "";
                                        member this.Assembly = null;
                                        member this.GetExportedTypes() = null;
                              }]

                let results = runTestsAndSaveResults saver runner token

                verify 
                    {
                        let! r = sb.ToString() |> checkAgainstStringStandardCleaned env
                        let! r = results |> expectsToBe [summary1]
                        return Success
                    }
        )

    