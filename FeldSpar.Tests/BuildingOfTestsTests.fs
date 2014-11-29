namespace FeldSpar.Console.Tests
open System
open FeldSpar.Console.Helpers
open FeldSpar.Console.Helpers.Data
open FeldSpar.Framework
open FeldSpar.Framework.TestSummaryUtilities
open FeldSpar.Framework.Engine
open FeldSpar.Framework.Verification
open FeldSpar.Framework.Verification.ChecksClean
open FeldSpar.Framework.Verification.ApprovalsSupport

module BuildingOfTestsTests =
    let ``Can Build Report from Execution Summaries`` =
        Test(fun env ->
            let summaries = (
                "internal tests",
                [
                    { 
                        TestName = "Summary One"; 
                        TestCanonicalizedName = "SummaryOne";
                        TestResults = Success;
                    };
                    { 
                        TestName = "Summary Two"; 
                        TestCanonicalizedName = "SummaryTwo";
                        TestResults = Failure(GeneralFailure("Something unknown happened"));
                    };
                    { 
                        TestName = "Summary Three"; 
                        TestCanonicalizedName = "SummaryThree";
                        TestResults = Success;
                    };
                    { 
                        TestName = "Summary Four"; 
                        TestCanonicalizedName = "SummaryThree";
                        TestResults = 5 |> expectsToBe 4;
                    };
                ])

            let report = summaries |> buildOutputReport
            report |> checkAgainstStandardObjectAsCleanedString env
        )

    let ``Report exports to JSON`` =
        Test(fun env ->
            let summaries = (
                "internal tests",
                [
                    { 
                        TestName = "Summary One"; 
                        TestCanonicalizedName = "SummaryOne";
                        TestResults = Success;
                    };
                    { 
                        TestName = "Summary Two"; 
                        TestCanonicalizedName = "SummaryTwo";
                        TestResults = Failure(GeneralFailure("Something unknown happened"));
                    };
                    { 
                        TestName = "Summary Three"; 
                        TestCanonicalizedName = "SummaryThree";
                        TestResults = Success;
                    };
                    { 
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

                let testTemplatesa = findTests true (env.AssemblyPath) |> Seq.sortBy(fun (description, _) -> description) |> Seq.map(fun (description, _) -> "(" + description + ")")
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
                let failingTestName = "A Test That will fail"
                //let ``A Test That will fail`` = 
                //    Test((fun _ -> failResult "Expected Failure"))

                //let tstEnv = createEnvironment { Reporters = []} (env.AssemblyPath) (Data.testFeldSparAssembly) failingTestName

                let ``A Test That will fail`` = 
                    {
                        Environment=createEnvironment { Reporters = []} (env.AssemblyPath) (Data.testFeldSparAssembly) failingTestName;
                        TestTemplate=(fun _ -> failResult "Expected Failure");
                    }

                let resultSummary = 
                    let _, test = ``A Test That will fail`` |> createTestFromTemplate ignore
                    test()

                verify
                    {
                        let! desriptionIsCorrect = resultSummary.TestName |> expectsToBe failingTestName
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

                let underTest = createEnvironment { Reporters=[] } (testFeldSparAssembly.Location) testFeldSparAssembly testDescription

                underTest.CanonicalizedName |> checkAgainstStringStandardCleaned env
            ))

    let ``An exception thrown in a test should report exception failure`` =
        Test((fun env ->
                let ex = IndexOutOfRangeException("The exception was out of range")

                let testName = "A test that throws an exception"

                let testEnvironment = (createEnvironment { Reporters = [] } (env.AssemblyPath) (env.Assembly) testName)

                let template = { Environment=testEnvironment; TestTemplate=(fun _ -> raise ex) }

                let _, case = template |> createTestFromTemplate ignore

                let summary = case()
                let result = summary.TestResults

                let regex = System.Text.RegularExpressions.Regex(@"(?<=at FeldSpar\.Console\.Tests\.BuildingOfTestsTests\.template).*\s+.*", Text.RegularExpressions.RegexOptions.Multiline)

                let resultString = result |> sprintf "%A"
                let mtch = regex.Match(resultString)
                let goodLength = mtch.Index + 1
                //let goodLength = resultString.Length

                let cleaned = resultString.Substring(0, goodLength) + " ..."

                (cleaned) |> checkAgainstStringStandardCleaned env
            ))
        