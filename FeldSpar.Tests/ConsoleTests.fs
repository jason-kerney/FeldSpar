namespace FeldSpar.Console.Tests
open FeldSpar.Console.Helpers.Data
open FeldSpar.Framework
open FeldSpar.Framework.Verification
open FeldSpar.Framework.TestSummaryUtilities
open FeldSpar.Framework.Engine
open FeldSpar.Console

module ConsoleTests = 
    let ``Test all Verbosity levels`` = 
        Test(
            fun env -> 
                vebosityLevels |> checkAllAgainstStandard env
        )

    let ``Compare Verbosity only excepts specific modifiers`` =
        Test(
            fun env ->
                let values = ["Max"; "Results"; "Errors"; "Detail"; "maxx"; "max"; "results"; "errors"; "detail"; "MAX"; "RESULTS"; "ERRORS"; "DETAIL"]
                let max = (values |> List.map (fun f -> f.Length) |> List.max) + 1
                let results = values |> List.map (fun f -> sprintf "'%s'%s%s a verbosity setting" f (String.replicate (max - (f.Length)) " ") (match compareVerbosity f with true -> "is" | false -> "isn't"))

                results |> checkAllAgainstStandard env
        )

    