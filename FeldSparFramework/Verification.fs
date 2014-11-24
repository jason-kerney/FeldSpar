namespace FeldSpar.Framework.Verification

open System
open ApprovalsSupport
open FeldSpar.Framework
open FeldSpar.Framework.TestResultUtilities
open FeldSpar.Framework.TestSummaryUtilities
open ApprovalTests.Core

[<AutoOpen>]
module Checks =
    let removeCarageReturns (s:string) =
        s.Replace("\r\n", "\n").Replace('\r', '\n')

    /// <summary>
    /// Tests a boolean for true
    /// </summary>
    /// <param name="failure">the failure type used to fail if the test was not true</param>
    /// <param name="test">the boolean being tested for true</param>
    /// <returns>A Success if the boolean is true otherwise a failure using the failure type provided</returns>
    let isTrue failure test =
        if test
        then Success
        else Failure(failure)

    /// <summary>
    /// Tests a boolean for false
    /// </summary>
    /// <param name="failure">the failure type used to fail if the test was true</param>
    /// <param name="test">the boolean being tested for false</param>
    /// <returns>A Success if the boolean is false otherwise a failure using the failure type provided</returns>
    let isFalse failure test =
        !test |> isTrue failure

    let private expectationCheck expected message actual check =
        if check expected actual
        then Success
        else
            let failureMessage = 
                try
                    sprintf message expected actual
                with
                    e -> raise e
                    
            Failure(ExpectationFailure(failureMessage))

    /// <summary>
    /// Tests a given value to determine if it is equal to the given value
    /// </summary>
    /// <param name="expected">the value that is expected</param>
    /// <param name="message">the message to return on failure</param>
    /// <param name="actual">the value being tested</param>
    /// <returns>Success if both are equal otherwise an Failure of type ExpectationFailure</returns>
    let expectsToBe expected message actual =
        (fun a b -> a = b) |> expectationCheck expected message actual

    /// <summary>
    /// Tests a given value to determine if it is not equal to the given value
    /// </summary>
    /// <param name="expected">the value that is not expected</param>
    /// <param name="message">the message to return on failure</param>
    /// <param name="actual">the value being tested</param>
    /// <returns>Success if both are not equal otherwise an Failure of type ExpectationFailure</returns>
    let expectsNotToBe expected message actual =
        (fun a b -> a <> b) |> expectationCheck  expected message actual

    /// <summary>
    /// Tests a given boolean to determine if it is true
    /// </summary>
    /// <param name="message">the message to return on failure</param>
    /// <param name="actual">The boolean being tested</param>
    /// <returns>Success if value is true otherwise an Failure of type ExpectationFailure</returns>
    let expectsToBeTrue message actual =
        actual |> expectsToBe true message

    /// <summary>
    /// Tests a given boolean to determine if it is false
    /// </summary>
    /// <param name="message">the message to return on failure</param>
    /// <param name="actual">The boolean being tested</param>
    /// <returns>Success if value is false otherwise an Failure of type ExpectationFailure</returns>
    let expectsToBeFalse message actual =
        actual |> expectsToBe false message

    /// <summary>
    /// Tests a given value to determine if it is null
    /// </summary>
    /// <param name="message">the message to return on failure</param>
    /// <param name="actual">The value being tested</param>
    /// <returns>Success if value is null otherwise an Failure of type ExpectationFailure</returns>
    let isNull message actual =
        actual |> expectsToBe null message

    /// <summary>
    /// Tests a given value to determine if it is not null
    /// </summary>
    /// <param name="message">the message to return on failure</param>
    /// <param name="actual">The value being tested</param>
    /// <returns>Success if value is not null otherwise an Failure of type ExpectationFailure</returns>  
    let isNotNull message actual =
        actual |> expectsNotToBe null message

    let private checkStandardsAndReport env (reporter:IApprovalFailureReporter) (approver:IApprovalApprover) =
        if(approver.Approve ())
        then
            do approver.CleanUpAfterSucess(reporter) 
            Success
        else 
            do approver.ReportFailure (reporter)
                                                                
            match reporter with
            | :? IReporterWithApprovalPower as approvalReporter -> 
                if approvalReporter.ApprovedWhenReported ()
                then do approver.CleanUpAfterSucess(reporter)
            | _ -> ()

            Failure(StandardNotMet)

    let private checkAgainstStandard env (approver:IApprovalApprover) =
        let reporter = getReporter env
        checkStandardsAndReport env reporter approver

    /// <summary>
    /// Gold standard testing. Compares the given string against a saved file to determine if they match.
    /// </summary>
    /// <param name="env">Information about the current test environment and current test.</param>
    /// <param name="test">The string being tested</param>
    /// <returns>Success if the string matches the file otherwise an Failure of type StandardNotMet</returns>  
    let checkAgainstStringStandard env test =
        let approver = getStringFileApprover env test
        checkAgainstStandard env approver

    /// <summary>
    /// Gold standard testing. Compares the given object against a saved file to determine if they match by calling 'ToString' on the object.
    /// </summary>
    /// <param name="env">Information about the current test environment and current test.</param>
    /// <param name="test">The string being tested</param>
    /// <returns>Success if the string matches the file otherwise an Failure of type StandardNotMet</returns>  
    let checkAgainstStandardObjectAsString env test =
        checkAgainstStringStandard env (sprintf "%A" test)

    /// <summary>
    /// Gold standard testing. Compares the given binary array against a saved binary file to determine if they match.
    /// </summary>
    /// <param name="env">Information about the current test environment and current test.</param>
    /// <param name="extentionWithoutDot">The file extension associated with this binary type</param>
    /// <param name="test">The binary array being tested</param>
    /// <returns>Success if the binary array matches the file otherwise an Failure of type StandardNotMet</returns>  
    let checkAgainstStandardBinary  env extentionWithoutDot test =
        let reporter = getReporter env
        let approver = getBinaryFileApprover env extentionWithoutDot test
        checkStandardsAndReport env reporter approver

    /// <summary>
    /// Gold standard testing. Compares the given binary stream against a saved binary file to determine if they match.
    /// </summary>
    /// <param name="env">Information about the current test environment and current test.</param>
    /// <param name="extentionWithoutDot">The file extension associated with this binary type</param>
    /// <param name="test">The binary stream being tested</param>
    /// <returns>Success if the binary stream matches the file otherwise an Failure of type StandardNotMet</returns>  
    let checkAgainstStandardStream env extentionWithoutDot test =
        let reporter = getReporter env
        let approver = getStreamFileApprover env extentionWithoutDot test
        checkStandardsAndReport env reporter approver

    /// <summary>
    /// Gold standard testing. Compares the given items against a saved binary file to determine if they match by converting them to strings.
    /// </summary>
    /// <param name="env">Information about the current test environment and current test.</param>
    /// <param name="converter">A method of converting an item to a string</param>
    /// <param name="tests">The sequence of items being tested</param>
    let checkAllAgainstStandardBy env (converter:'a -> string) (tests:'a seq) =
        let result =
            tests
            |> Seq.map converter
            |> joinWith "\n"

        result |> checkAgainstStringStandard env

    /// <summary>
    /// Gold standard testing. Compares the given items against a saved binary file to determine if they match by calling 'ToSting' on each item.
    /// </summary>
    /// <param name="env">Information about the current test environment and current test.</param>
    /// <param name="tests">The sequence of items being tested</param>
    let checkAllAgainstStandard env (tests:'a seq) =
        checkAllAgainstStandardBy env (fun item -> item.ToString()) tests

    type Validation () =
        member this.Bind(testResult, f) = 
            match testResult with
            | Success -> f(testResult)
            | failure -> failure

        member this.Zero () =
            indeterminateTest

        member this.Delay(f) = f()
        member this.Combine(m, f) =
            match m with
            | Success -> f(m)
            | failure -> failure

        member this.Return(result) = result
        member this.ReturnFor(result) = result

    let verify = Validation()