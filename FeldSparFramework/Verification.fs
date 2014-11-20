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

    let isTrue failure success =
        if success
        then Success
        else Failure(failure)

    let isFalse failure success =
        !success |> isTrue failure

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

    let expectsToBe expected message actual =
        (fun a b -> a = b) |> expectationCheck expected message actual

    let expectsNotToBe expected message actual =
        (fun a b -> a <> b) |> expectationCheck  expected message actual

    let expectsToBeTrue message actual =
        actual |> expectsToBe true message

    let expectsToBeFalse message actual =
        actual |> expectsToBe false message

    let isNull message actual =
        actual |> expectsToBe null message

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

    let checkAgainstStringStandard env result =
        let result = result |> removeCarageReturns
        let approver = getStringFileApprover env result
        checkAgainstStandard env approver

    let checkAgainstStandardObjectAsString env result =
        checkAgainstStringStandard env (sprintf "%A" result)

    let checkAgainstStandardBinary  env extentionWithoutDot results =
        let reporter = getReporter env
        let approver = getBinaryFileApprover env extentionWithoutDot results
        checkStandardsAndReport env reporter approver

    let checkAgainstStandardStream env extentionWithoutDot results =
        let reporter = getReporter env
        let approver = getStreamFileApprover env extentionWithoutDot results
        checkStandardsAndReport env reporter approver

    let checkAllAgainstStandardBy env (converter:'a -> string) (results:'a seq) =
        let result =
            results
            |> Seq.map converter
            |> Seq.map removeCarageReturns
            |> joinWith "\n"

        result |> checkAgainstStringStandard env

    let checkAllAgainstStandard env (results:'a seq) =
        let result =
            results
            |> Seq.map (fun o -> o.ToString() |> removeCarageReturns)
            |> joinWith "\n"

        result |> checkAgainstStringStandard env

    type Validation () =
        member this.Bind(m, f) = 
            match m with
            | Success -> f(m)
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