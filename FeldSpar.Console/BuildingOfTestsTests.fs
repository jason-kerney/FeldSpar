namespace FeldSpar.Console.Tests
open System
open FeldSpar.Console.Helpers
open FeldSpar.Console.Helpers.Data
open FeldSpar.Framework
open FeldSpar.Framework.TestSummaryUtilities
open FeldSpar.Framework.Engine
open FeldSpar.Framework.Verification

module BuildingOfTestsTests =
    let ``Find All Tests through Reflection`` = 
        Test((fun env ->
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
            ))
    let ``Test that a failing test shows as a failure`` = 
        Test((fun env ->
                let failDescription = "A Test That will fail"
                let ``A Test That will fail`` = 
                    Test((fun env -> failResult "Expected Failure"))

                let resultSummary = 
                    let _, test = ``A Test That will fail`` |> createTestFromTemplate ignore failDescription
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
                                |> runAsTests
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

                let _, case = ``A test that throws an exception`` |> createTestFromTemplate ignore "A test that throws an exception"

                let summary = case()
                let result = summary.TestResults

                let regex = System.Text.RegularExpressions.Regex(@"(?<=at FeldSpar\.Console\.Tests\.BuildingOfTestsTests\.A test that throws an exception@).*\s+.*", Text.RegularExpressions.RegexOptions.Multiline)

                let resultString = result |> sprintf "%A"
                let mtch = regex.Match(resultString)
                let goodLength = mtch.Index + 1

//                let cleaned = resultString
                let cleaned = resultString.Substring(0, goodLength) + " ..."

                verify
                    {
                        let! meetsStandard = (cleaned) |> checkStandardsAgainstStringAndReportsWith<ApprovalTests.Reporters.BeyondCompareReporter> env
                        return Success
                    }
            ))
        