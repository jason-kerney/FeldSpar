namespace FeldSpar.Console.Tests
open FeldSpar.Console.Helpers.Data
open FeldSpar.Framework
open FeldSpar.Framework.Verification
open FeldSpar.Framework.TestSummaryUtilities
open FeldSpar.Framework.Engine

module IsolationTests = 
    type ChangeAble () =
        let mutable x = 0
        member public this.Increment () =
            x <- x + 1

        member public this.X
            with get () =
                x

    let ``Verify that tests run in isolation`` =
        Test((fun env ->
                let changer = ChangeAble()
                let mainExpected = changer.X
                                
                let ``First test to test isolation`` = 
                    Test((fun env ->
                            let expected = 1;
                            do changer.Increment ()

                            let actual = changer.X

                            actual |> expectsToBe expected |> addMessage "changer did not increment correctly"
                        ))

                let ``Second test to test isolation`` = 
                    Test((fun env ->
                            let expected = 1;
                            do changer.Increment ()

                            let actual = changer.X

                            actual |> expectsToBe expected |> addMessage "changer did not increment correctly expected"
                        ))

                let results = [("First test to test isolation", ``First test to test isolation``); ("Second test to test isolation", ``Second test to test isolation``)] |> runAsTests (env.AssemblyPath)
                let isolatedResults = results |> reduceToFailures |> Seq.isEmpty

                let mainActual = changer.X

                verify
                    {
                        let! testsWhereIsolatedFromEachother = isolatedResults |> isTrue (GeneralFailure("tests failed to manipulate the changer correctly or in isolation"))
                        let! testsWhereIsolatedFromMain = mainActual |> expectsToBe mainExpected |> addMessage "tests did not run in isolation."
                        return Success
                    }
            ))
