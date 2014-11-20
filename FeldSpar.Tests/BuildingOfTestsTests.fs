namespace FeldSpar.Console.Tests
open System
open FeldSpar.Console.Helpers
open FeldSpar.Console.Helpers.Data
open FeldSpar.Framework
open FeldSpar.Framework.TestSummaryUtilities
open FeldSpar.Framework.Engine
open FeldSpar.Framework.Verification
open FeldSpar.Framework.Verification.ApprovalsSupport

module BuildingOfTestsTests =
    let ``Can Build Report from Execution Summaries`` =
        Test(fun env ->
            let summaries = (
                "internal tests",
                [
                    { 
                        TestDescription = "Summary One"; 
                        TestCanonicalizedName = "SummaryOne";
                        TestResults = Success;
                    };
                    { 
                        TestDescription = "Summary Two"; 
                        TestCanonicalizedName = "SummaryTwo";
                        TestResults = Failure(GeneralFailure("Something unknown happened"));
                    };
                    { 
                        TestDescription = "Summary Three"; 
                        TestCanonicalizedName = "SummaryThree";
                        TestResults = Success;
                    };
                    { 
                        TestDescription = "Summary Four"; 
                        TestCanonicalizedName = "SummaryThree";
                        TestResults = 5 |> expectsToBe 4 "%d <> %d";
                    };
                ])

            let report = summaries |> buildOutputReport
            report |> checkAgainstStandardObjectAsString env
        )

    let ``Report exports to JSON`` =
        Test(fun env ->
            let summaries = (
                "internal tests",
                [
                    { 
                        TestDescription = "Summary One"; 
                        TestCanonicalizedName = "SummaryOne";
                        TestResults = Success;
                    };
                    { 
                        TestDescription = "Summary Two"; 
                        TestCanonicalizedName = "SummaryTwo";
                        TestResults = Failure(GeneralFailure("Something unknown happened"));
                    };
                    { 
                        TestDescription = "Summary Three"; 
                        TestCanonicalizedName = "SummaryThree";
                        TestResults = Success;
                    };
                    { 
                        TestDescription = "Summary Four"; 
                        TestCanonicalizedName = "SummaryThree";
                        TestResults = 5 |> expectsToBe 4 "%d <> %d";
                    };
                ])

            let report = summaries |> buildOutputReport |> FeldSpar.Framework.TestSummaryUtilities.JSONFormat
            report |> checkAgainstStringStandard env
        )

    let ``Can Create multiple Tests From one Theory Test`` =
        Test(fun env ->
                let theory = Theory({
                                        Data = seq { for i in 1..4 do yield i};
                                        Base = {
                                                       UnitDescription = (fun n -> sprintf "testing %d" n);
                                                       UnitTest = (fun n _ -> (n % 2) |> expectsToBe 0 "number was not even. n mod 2 = %d when it should have been n mod 2 = %d")
                                                    }
                                    })

                let results =  "testing theory"
                                |> convertTheoryToTests theory
                                |> Array.map (fun (description, Test(test)) -> (description, env |> test))
                                |> Array.map (fun (description, result) ->
                                                let resultString = 
                                                    match result with
                                                    | Success -> "Success"
                                                    | Failure(failType) -> sprintf "%A" failType

                                                sprintf "%s -> %s" description resultString
                                              )

                let result = String.Join("\n", results) + "\n"

                result |> checkAgainstStringStandard env
            )

    let ``Find All Tests through Reflection`` = 
        ITest((fun env ->
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

                let testTemplatesa = findTests (env.AssemblyPath) |> Seq.sortBy(fun (description, _) -> description) |> Seq.map(fun (description, _) -> "(" + description + ")")
                let testTemplatesb = testTemplatesa |> Seq.toList
                let testTemplates = testTemplatesb |> join

                verify
                    {
                        let! testsMeetStandards = testTemplates |> checkAgainstStringStandard env 
                        return Success
                    }
            ))

    let ``Test that a failing test shows as a failure`` = 
        Test((fun env ->
                let failDescription = "A Test That will fail"
                let ``A Test That will fail`` = 
                    Test((fun env -> failResult "Expected Failure"))

                let config : AssemblyConfiguration = { Reporters = []}

                let resultSummary = 
                    let _, test = ``A Test That will fail`` |> createTestFromTemplate config ignore failDescription (env.AssemblyPath) (Data.testFeldSparAssembly)
                    test()

                verify
                    {
                        let! desriptionIsCorrect = resultSummary.TestDescription |> expectsToBe failDescription "Incorrect description expected '%s' but got '%s'"
                        let! testFailedCorrectly = resultSummary.TestResults |> expectsToBe (failResult "Expected Failure") "Test did not fail correctly expected %A but got %A"
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
                            actual |> expectsToBe expected "Name was not set correctly. Expected '%s' but got '%s'"
                        ))

                verify
                    {
                        let! testRanCorrectly =(
                            [("Can0n1cliz3 \t\r\n\t\tThis<>/?!#$%^&*()+-*;'\"|`~", ``Can0n1cliz3 \t\r\n\t\tThis<>/?!#$%^&*()+-*;'\"|`~``)] 
                                |> runAsTests (env.AssemblyPath)
                                |> reduceToFailures
                                |> Seq.isEmpty 
                                |> isTrue (ExpectationFailure("test Failed to have correct Name")))

                        return Success
                    }
            ))

    let ``An exception thrown in a test should report exception failure`` =
        Test((fun env ->
                let ex = IndexOutOfRangeException("The exception was out of range")
                let ``A test that throws an exception`` =  Test((fun env -> raise ex))

                let _, case = ``A test that throws an exception`` |> createTestFromTemplate { Reporters = [] } ignore "A test that throws an exception" (env.AssemblyPath) (Data.testFeldSparAssembly)

                let summary = case()
                let result = summary.TestResults

                let regex = System.Text.RegularExpressions.Regex(@"(?<=at FeldSpar\.Console\.Tests\.BuildingOfTestsTests\.A test that throws an exception).*\s+.*", Text.RegularExpressions.RegexOptions.Multiline)

                let resultString = result |> sprintf "%A"
                let mtch = regex.Match(resultString)
                let goodLength = mtch.Index + 1

                let cleaned = resultString.Substring(0, goodLength) + " ..."

                verify
                    {
                        let! meetsStandard = (cleaned) |> checkAgainstStringStandard env
                        return Success
                    }
            ))
        