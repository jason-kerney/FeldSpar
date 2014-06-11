namespace FeldSpar.Console.Tests
open System
open FeldSpar.Console.Helpers
open FeldSpar.Console.Helpers.Data
open FeldSpar.Framework
open FeldSpar.Framework.TestSummaryUtilities
open FeldSpar.Framework.Engine
open FeldSpar.Framework.Verification

module BuildingOfTestsTests =
    let testFindTests = 
        Test({
                Description = "Find All Tests through Reflection";
                UnitTest = (fun env ->

                                let join : string list -> string = (fun (arry) -> 
                                                                    let rec append (value: string list) (acc, cnt) =
                                                                        match value with
                                                                        | [] -> acc
                                                                        | head::tail ->
                                                                            (sprintf "%sTest[%d] is (%s)%s" acc cnt head System.Environment.NewLine, cnt + 1) |> append tail

                                                                    ("", 0) |> append arry
                                                                )

                                let testTemplatesa = findTests (assembly) |> Seq.map(fun (description, _) -> "(" + description + ")")
                                let testTemplatesb = testTemplatesa |> Seq.toList
                                let testTemplates = testTemplatesb |> join

                                verify
                                    {
                                        let! testsMeetStandards = checkStandardsAgainstStringAndReportsWith<ApprovalTests.Reporters.BeyondCompareReporter> env testTemplates
                                        return Success
                                    }
                           )
            })
    let testFailure = 
        Test({
                Description = "Test that a failing test shows as a failure"
                UnitTest = (fun env ->
                                let failDescription = "A Test That will fail"
                                let failingTest = Test({
                                                            Description = failDescription;
                                                            UnitTest = (fun env -> failResult "Expected Failure")
                                                        })

                                let resultSummary = 
                                    let _, test = failingTest |> createTestFromTemplate ignore
                                    test()

                                verify
                                    {
                                        let! desriptionIsCorrect = resultSummary.TestDescription |> expectsToBe failDescription "Incorrect description expected '%s' but got '%s'"
                                        let! testFailedCorrectly = resultSummary.TestResults |> expectsToBe (failResult "Expected Failure") "Test did not fail correctly expected %A but got %A"
                                        return Success
                                    }
                            )
            })

    let testSuccess = 
        Test({
                Description = "A test that succeeds";
                UnitTest = (fun env -> Success )
            })

    let testCanonicalizeString = 
        Test({
                Description = "Testing that CanoicalizationOfStrings Work";
                UnitTest = (fun env ->
                                let stringUnderTest = "a@\t\t\tc\r\nd`~ 1234567890!@#$%^&*()=+[{]}\\|;:'\",<.>/?+-_"
                                let expected = "acd_1234567890.-_"

                                let actual = Formatters.Basic.CanonicalizeString stringUnderTest

                                match actual with
                                    | "acd_1234567890.-_" -> Success
                                    | _ -> Failure(ExpectationFailure("did not clean the string correctly"))
                            )
            })

    let testEnvironmentHasCanonicalizedName =
        Test({
                Description = "The environment of a test should canonicalize the description correctly into the name";
                UnitTest = (fun env ->
                                let testDescription = "Ca@n0n1cliz3 \t\r\n\t\tThis<>/?!#$%^&*()+-*;'\"|`~"
                                let expected = (Formatters.Basic.CanonicalizeString testDescription)

                                let test = 
                                    Test({
                                            Description = testDescription;
                                            UnitTest = (fun env ->
                                                            let actual = env.CanonicalizedName
                                                            actual |> expectsToBe expected "Name was not set correctly. Expected '%s' but got '%s'"
                                                        )
                                        })

                                verify
                                    {
                                        let! testRanCorrectly =(
                                            [test] 
                                                |> FeldSpar.Console.Helpers.Data.runTests 
                                                |> reduceToFailures
                                                |> Seq.isEmpty 
                                                |> isTrue (ExpectationFailure("test Failed to have correct Name")))

                                        return Success
                                    }
                            )
            })

    let testExeptionTestReturnsExptionFailure =
        Test({
                Description = "An exception thrown in a test should report exception failure"
                UnitTest = (fun env ->
                                let ex = IndexOutOfRangeException("The exception was out of range")
                                let throwingTest = 
                                    Test({
                                            Description = "A test that throws an exception"
                                            UnitTest = (fun env -> raise ex)
                                        })

                                let _, case = throwingTest |> createTestFromTemplate ignore

                                let summary = case()
                                let result = summary.TestResults

                                let regex = System.Text.RegularExpressions.Regex(@"(?<=FeldSpar\.Console\.Tests\.BuildingOfTestsTests\.throwingTest\@).*\s+.*", Text.RegularExpressions.RegexOptions.Multiline)

                                let resultString = result |> sprintf "%A"
                                let mtch = regex.Match(resultString)
                                let goodLength = mtch.Index + 1

                                let cleaned = resultString.Substring(0, goodLength) + " ..."

                                verify
                                    {
                                        let! meetsStandard = (cleaned) |> checkStandardsAgainstStringAndReportsWith<ApprovalTests.Reporters.BeyondCompareReporter> env
                                        return Success
                                    }
                            )
            })
        