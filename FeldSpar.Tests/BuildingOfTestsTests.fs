﻿namespace FeldSpar.Console.Tests
open System
open System.Text.RegularExpressions
open FeldSpar.Console.Helpers
open FeldSpar.Console.Helpers.Data
open FeldSpar.Framework
open FeldSpar.Framework.TestSummaryUtilities
open FeldSpar.Framework.Engine
open FeldSpar.Framework.Verification
open FeldSpar.Framework.Verification.ChecksClean
open FeldSpar.Framework.Verification.ApprovalsSupport

module ``Building of tests should`` =
    let private summaries = (
        "internal tests",
        [
            { 
                TestContainerName = "Can Build Report from Execution Summaries";
                TestName = "Summary One"; 
                TestCanonicalizedName = "SummaryOne";
                TestResults = Success;
            };
            { 
                TestContainerName = "Can Build Report from Execution Summaries";
                TestName = "Summary Two"; 
                TestCanonicalizedName = "SummaryTwo";
                TestResults = Failure(GeneralFailure("Something unknown happened"));
            };
            { 
                TestContainerName = "Can Build Report from Execution Summaries";
                TestName = "Summary Three"; 
                TestCanonicalizedName = "SummaryThree";
                TestResults = Success;
            };
            { 
                TestContainerName = "Can Build Report from Execution Summaries";
                TestName = "Summary Four"; 
                TestCanonicalizedName = "SummaryThree";
                TestResults = 5 |> expectsToBe 4;
            };
        ])

    let ``have structured summaries`` =
        Test(fun env ->
            summaries |> checkAgainstStandardObjectAsCleanedString env
        ) 

    let ``build a report from the Execution Summaries`` =
        Test(fun env ->
            let report = summaries |> buildOutputReport
            report |> checkAgainstStandardObjectAsCleanedString env
        )

    let ``get a representitive environment`` = 
        Test(fun env ->
            let sut = { env with GoldStandardPath = "...\\FeldSpar.Tests\\"; AssemblyPath = "...\\FeldSpar.Tests\\bin\\..."; Reporters = [] }
            let env = { env with CanonicalizedName = env.CanonicalizedName }
            sut |> checkAgainstStandardObjectAsCleanedString env
        )

    let ``build a report from execution summaries that are sorted naturally`` =
        Test(fun env ->
            let summaries = (
                "internal tests",
                [
                    { 
                        TestContainerName = "Can Build Report from Execution Summaries Sorted by numeric Values";
                        TestName = "Summary 3"; 
                        TestCanonicalizedName = "Summary3";
                        TestResults = Success;
                    };
                    { 
                        TestContainerName = "Can Build Report from Execution Summaries Sorted by numeric Values";
                        TestName = "Summary 4"; 
                        TestCanonicalizedName = "Summary4";
                        TestResults = Failure(GeneralFailure("Something unknown happened"));
                    };
                    { 
                        TestContainerName = "Can Build Report from Execution Summaries Sorted by numeric Values";
                        TestName = "Summary 11"; 
                        TestCanonicalizedName = "Summary11";
                        TestResults = Success;
                    };
                    { 
                        TestContainerName = "Can Build Report from Execution Summaries Sorted by numeric Values";
                        TestName = "Summary 12"; 
                        TestCanonicalizedName = "Summary12";
                        TestResults = 5 |> expectsToBe 4;
                    };
                ])

            summaries 
                |> buildOutputReport 
                |> checkAgainstStandardObjectAsCleanedString env
        )

    let ``Report exports to JSON`` =
        Test(fun env ->
            let summaries = (
                "internal tests",
                [
                    { 
                        TestContainerName = "Report exports to JSON";
                        TestName = "Summary One"; 
                        TestCanonicalizedName = "SummaryOne";
                        TestResults = Success;
                    };
                    { 
                        TestContainerName = "Report exports to JSON";
                        TestName = "Summary Two"; 
                        TestCanonicalizedName = "SummaryTwo";
                        TestResults = Failure(GeneralFailure("Something unknown happened"));
                    };
                    { 
                        TestContainerName = "Report exports to JSON";
                        TestName = "Summary Three"; 
                        TestCanonicalizedName = "SummaryThree";
                        TestResults = Success;
                    };
                    { 
                        TestContainerName = "Report exports to JSON";
                        TestName = "Summary Four"; 
                        TestCanonicalizedName = "SummaryThree";
                        TestResults = 5 |> expectsToBe 4;
                    };
                ])

            let report = summaries |> buildOutputReport |> FeldSpar.Framework.TestSummaryUtilities.JSONFormat
            report |> checkAgainstStringStandardCleaned env
        )

    let ``Can Create multiple Tests From one Theory Test`` =
        Test(fun env ->
                let theory = Theory({
                                        Data = seq { for i in 1..4 do yield i};
                                        Base = {
                                                  UnitDescription = (fun n -> sprintf "testing %d" n);
                                                  UnitTest = fun n _ -> (n % 2) |> expectsToBe 0 |> withFailComment (sprintf "Number was not even 2 mod %d <> 0" n)
                                               }
                                    })

                let results =  "testing theory"
                                |> convertTheoryToTests theory "Can Create multiple Tests From one Theory Test"
                                |> Array.map (
                                    (fun { TestName = description; Test = Test(test) } -> (description, env |> test))
                                    >>
                                    (fun (description, result) ->
                                        let resultString = 
                                            match result with
                                            | Success -> "Success"
                                            | Failure(failType) -> sprintf "%A" failType

                                        sprintf "%s -> %s" description resultString
                                    ))

                let result = String.Join("\n", results) + "\n"

                result |> checkAgainstStringStandardCleaned env
            )

    let ``Find All Tests through Reflection`` = 
        Test((fun env ->
                let join : string list -> string = (fun (arry) -> 
                                                    let rec append (value: string list) (acc, cnt) =
                                                        match value with
                                                        | [] -> acc
                                                        | head::tail ->
                                                            let ns = cnt.ToString()
                                                            let pad = arry.Length.ToString ()
                                                            let pad = pad.Length
                                                            let ns = ns.PadLeft(pad, '0')
                                                            (sprintf "%sTest[%s] is (%s)%s" acc ns head "\n", cnt + 1) |> append tail

                                                    ("", 0) |> append arry
                                                )

                let testTemplatesa = 
                    findTests IgnoreAssemblyConfiguration (env |> loadToken) 
                        |> Seq.sortBy(fun { Container = container; TestName = description; TestCase =  _ } -> container + description) 
                        |> Seq.map(fun { Container = container; TestName = description; TestCase = _} -> "(" + container + " " + description + ")")
                let testTemplatesb = testTemplatesa |> Seq.toList
                let testTemplates = testTemplatesb |> join

                verify
                    {
                        let! testsMeetStandards = testTemplates |> checkAgainstStringStandardCleaned env 
                        return Success
                    }
            ))

    let ``Test that a failing test shows as a failure`` = 
        Test((fun env ->
                let failDescription = "A Test That will fail"
                let ``A Test That will fail`` = 
                    Test((fun env -> failResult "Expected Failure"))

                let ``A Test That will fail`` =
                    {
                        TestContainerName = "Test that a failing test shows as a failure";
                        TestName = "A Test That will fail";
                        Test = ``A Test That will fail``;
                    }

                let config : AssemblyConfiguration = { Reporters = []}

                let resultSummary = 
                    let { TestName = _; TestCase = test } = ``A Test That will fail`` |> createTestFromTemplate config ignore (env |> loadToken)
                    test()

                verify
                    {
                        let! desriptionIsCorrect = resultSummary.TestName |> expectsToBe failDescription
                        let! testFailedCorrectly = resultSummary.TestResults |> expectsToBe (failResult "Expected Failure") |> withFailComment "Test did not fail correctly expected"
                        return Success
                    }
            ))

    let ``A test that succeeds`` =  Test((fun env -> Success ))

    let ``Testing that CanoicalizationOfStrings Works`` = 
        Test((fun env ->
                let stringUnderTest = "a@\t\t\tc\r\nd`~ 1234567890!@#$%^&*()=+[{]}\\|;:'\",<.>/?+-_"
                let expected = "acd_1234567890.-_"

                let actual = Formatters.Basic.CanonicalizeString stringUnderTest

                match actual with
                    | "acd_1234567890.-_" -> Success
                    | _ -> Failure(ExpectationFailure("did not clean the string correctly"))
            ))

    let ``The environment of a test should canonicalize the description correctly into the name`` =
        Test((fun env ->
                let testDescription = "Ca@n0n1cliz3 \t\r\n\t\tThis<>/?!#$%^&*()+-*;'\"|`~"
                let expected = (Formatters.Basic.CanonicalizeString testDescription)

                let ``Can0n1cliz3 \t\r\n\t\tThis<>/?!#$%^&*()+-*;'\"|`~`` = 
                    Test((fun env ->
                            let actual = env.CanonicalizedName
                            actual |> expectsToBe expected
                        ))

                let info = 
                    {
                        TestContainerName = "The environment of a test should canonicalize the description correctly into the name"
                        TestName = testDescription;
                        Test = ``Can0n1cliz3 \t\r\n\t\tThis<>/?!#$%^&*()+-*;'\"|`~``;
                    }

                verify
                    {
                        let! testRanCorrectly =(
                            [info] 
                                |> runAsTests (env |> loadToken)
                                |> reduceToFailures
                                |> Seq.isEmpty 
                                |> isTrue (ExpectationFailure("test Failed to have correct Name")))

                        return Success
                    }
            ))

    let ``An exception thrown in a test should report exception failure`` =
        Test((fun env ->
                let envNew = { env with CanonicalizedName= env.CanonicalizedName + "." + buildType }
                let ex = IndexOutOfRangeException("The exception was out of range")
                let ``A test that throws an exception`` =  Test((fun env -> raise ex))

                let ``A test that throws an exception`` = 
                    {
                        TestContainerName = "An exception thrown in a test should report exception failure";
                        TestName = "A test that throws an exception";
                        Test = ``A test that throws an exception``;
                    }

                let { TestName = _; TestCase = case } = ``A test that throws an exception`` |> createTestFromTemplate { Reporters = [] } ignore (envNew |> loadToken)

                let summary = case()
                let result = summary.TestResults

                let regex = System.Text.RegularExpressions.Regex(@"(?<=at FeldSpar\.Console\.Tests\.Building Of Tests Is Correct\.A test that throws an exception).*\s+.*", Text.RegularExpressions.RegexOptions.Multiline)

                let resultString = result |> sprintf "%A"
                let mtch = regex.Match(resultString)
                let goodLength = mtch.Index + 1

                let cleaned = resultString.Substring(0, goodLength) + " ..." |> (fun s -> s.Replace("\r\n","\n"))

                verify
                    {
                        let! meetsStandard = (cleaned) |> checkAgainstStringStandardCleaned envNew
                        return Success
                    }
            ))
        