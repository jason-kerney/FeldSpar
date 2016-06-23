namespace FeldSpar.Console.Tests
open FeldSpar.Console.Helpers
open FeldSpar.Console.Helpers.Data
open FeldSpar.Framework
open FeldSpar.Framework.TestSummaryUtilities
open FeldSpar.Framework.Engine
open FeldSpar.Framework.Verification
open FeldSpar.Framework.Verification.ChecksClean
open FeldSpar.Framework.Verification.ApprovalsSupport

module DotNetFrameworkTests =
    let ``Can Detect the correct version`` =
        Test(fun _ ->
                         
                let expectedVersion =
                    let getVersion t v =
                        if t () = None then
                            fun () -> Some(v)
                        else
                            t

                    let test : unit -> SupportedFrameworks option = fun () -> None
                #if NET40
                    let test = getVersion test Net40
                #endif
                #if NET45
                    let test = getVersion test Net45
                #endif
                #if NET46
                    let test = getVersion test Net46
                #endif

                    test ()

                Some(currentFramework) |> expectsToBe expectedVersion
        )

