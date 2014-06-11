namespace FeldSpar.Framework.Verification

open System
open ApprovalsSupport
open FeldSpar.Framework
open FeldSpar.Framework.TestResultUtilities
open ApprovalTests.Core

[<AutoOpen>]
module Checks =
    let isTrue failure success =
        if success
        then Success
        else Failure(failure)

    let expectsToBe expected message actual =
        if expected = actual
        then Success
        else
            let failureMessage = 
                try
                    sprintf message expected actual
                with
                    e -> raise e
                    
            Failure(ExpectationFailure(failureMessage))

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

    let private checkStandards<'a when 'a :> IApprovalFailureReporter> env (approver:IApprovalApprover) =
        let reporter = getReporter<'a> () 
        checkStandardsAndReport env reporter approver

    let checkStandardsAgainstStringAndReportsWith<'a when 'a :> IApprovalFailureReporter> env result =
        let approver = getStringFileApprover env result
        checkStandards<'a> env approver

    let checkStandardsAgainstObjectAsStringAndReportWith<'a when 'a :> IApprovalFailureReporter> env result =
        checkStandardsAgainstStringAndReportsWith env (sprintf "%A" result)

    let checkStandardsAgainstBinaryAndReportsWith<'a when 'a :> IApprovalFailureReporter> env extentionWithoutDot results =
        let approver = getBinaryFileApprover env extentionWithoutDot results
        checkStandards<'a> env approver

    let checkStandardsAgainstStreamAndReportsWith<'a when 'a :> IApprovalFailureReporter> env extentionWithoutDot results =
        let approver = getStreamFileApprover env extentionWithoutDot results
        checkStandards<'a> env approver

    (* Does not work not sure why
    let checkStandardsAgainstString env result =
        let approver = getStringFileApprover env result
        let reporter = getConfiguredReporter ()

        checkStandardsAndReport env reporter approver

    let checkStandardsAgainstObjectAsString env result =
        let approver = getStringFileApprover env (sprintf "%A" result)
        let reporter = getConfiguredReporter ()

        checkStandardsAndReport env reporter approver

    let checkStandardsAgainstBinary env extentionWithoutDot results =
        let approver = getBinaryFileApprover env extentionWithoutDot results
        let reporter = getConfiguredReporter ()

        checkStandardsAndReport env reporter approver

    let checkStandardsAgainstStream env extentionWithoutDot results =
        let approver = getBinaryFileApprover env extentionWithoutDot results
        let reporter = getConfiguredReporter ()

        checkStandardsAndReport env reporter approver
    //*)

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