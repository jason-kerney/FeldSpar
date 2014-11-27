namespace FeldSpar.Framework.Verification

open System
open ApprovalsSupport
open FeldSpar.Framework
open FeldSpar.Framework.TestResultUtilities
open FeldSpar.Framework.TestSummaryUtilities
open ApprovalTests.Core

module ChecksClean =
    let removeCarageReturns (s:string) =
        s.Replace("\r\n", "\n").Replace('\r', '\n')

    let cleanNothing item = item

    let checkStandardsAndReport (env:TestEnvironment) (reporter:IApprovalFailureReporter) (approver:IApprovalApprover) =
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

            Failure(StandardNotMet((getPath env) + env.CanonicalizedName + ".approved"))

    let checkAgainstStandard env (approver:IApprovalApprover) =
        let reporter = getReporter env
        checkStandardsAndReport env reporter approver

    let checkAgainstStringStandardWithCleaner env (cleaner:string -> string) test =
        let test = test |> cleaner
        let approver = getStringFileApprover env test
        checkAgainstStandard env approver

    let checkAgainstStringStandardCleaned env test =
        checkAgainstStringStandardWithCleaner env removeCarageReturns test

    let checkAgainstStandardObjectAsStringWithCleaner env cleaner test =
        checkAgainstStringStandardWithCleaner env cleaner (sprintf "%A" test)

    let checkAgainstStandardObjectAsCleanedString env test =
        checkAgainstStandardObjectAsStringWithCleaner env removeCarageReturns (sprintf "%A" test)

    let checkAllAgainstStandardWithCleanerBy env (converter:'a -> string) cleaner tests =
        let result =
            tests
            |> Seq.map converter
            |> joinWith "\n"

        result |> checkAgainstStringStandardWithCleaner env cleaner

    let checkAllAgainstStandardWithCleandBy env converter tests =
        checkAllAgainstStandardWithCleanerBy env converter removeCarageReturns tests

    let checkAllAgainstStandardWithCleaner env cleaner tests =
        checkAllAgainstStandardWithCleanerBy env (fun item -> item.ToString()) cleaner tests

    let checkAllAgainstStandardCleaned env tests =
        checkAllAgainstStandardWithCleaner env removeCarageReturns tests