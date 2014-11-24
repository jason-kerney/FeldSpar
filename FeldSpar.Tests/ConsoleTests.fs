﻿namespace FeldSpar.Console.Tests
open FeldSpar.Console.Helpers.Data
open FeldSpar.Framework
open FeldSpar.Framework.Verification
open FeldSpar.Framework.TestSummaryUtilities
open FeldSpar.Framework.Engine
open FeldSpar.Console
open System.Text

module ConsoleTests = 
    let testSummaries = 
        [
            ("My Summary 1", 
                ([ 
                    { TestDescription = "Fake test 3"; TestCanonicalizedName = "Fake_Test"; TestResults = Failure(GeneralFailure("this is a failing test")); };
                    { TestDescription = "Fake test 1"; TestCanonicalizedName = "Fake_Test"; TestResults = Success; };
                    { TestDescription = "Fake test 2"; TestCanonicalizedName = "Fake_Test"; TestResults = Failure(Ignored("this is an ignored test")); };
                ] |> Seq.ofList)
            );
            ("My Summary 2", 
                ([ 
                    { TestDescription = "Faker test 3"; TestCanonicalizedName = "Fake_Test"; TestResults = Failure(GeneralFailure("this is a failing test")); };
                    { TestDescription = "Faker test 1"; TestCanonicalizedName = "Fake_Test"; TestResults = Success; };
                    { TestDescription = "Faker test 2"; TestCanonicalizedName = "Fake_Test"; TestResults = Failure(Ignored("this is an ignored test")); };
                ] |> Seq.ofList)
            );
        ]

    let ``Test all Verbosity levels`` = 
        Test(
            fun env -> 
                vebosityLevels |> checkAllAgainstStandard env
        )

    let ``Compare Verbosity only excepts specific modifiers`` =
        Test(
            fun env ->
                let values = ["Max"; "Results"; "Errors"; "Detail"; "maxx"; "max"; "results"; "errors"; "detail"; "MAX"; "RESULTS"; "ERRORS"; "DETAIL"]
                let max = (values |> List.map (fun f -> f.Length) |> List.max) + 1
                let results = values |> List.map (fun f -> sprintf "'%s'%s%s a verbosity setting" f (String.replicate (max - (f.Length)) " ") (match compareVerbosity f with true -> "is" | false -> "isn't"))

                results |> checkAllAgainstStandard env
        )

    let ``saveResults calls a saver after converting results to JSON`` =
        Test(
            fun env ->
                let sb = new StringBuilder()
                let saver : string -> string -> unit =
                    fun path data -> sb.Append(sprintf "Path: '%s'\nValues ->\n%s" path data) |> ignore

                let path = @"Path:\Somewhere"

                saveResults saver path testSummaries
                sb.ToString() |> checkAgainstStringStandard env
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
                
                !result |> checkAgainstStringStandard env
        )

    