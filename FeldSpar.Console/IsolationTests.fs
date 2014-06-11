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

    let testTestIsolation =
        Test({
                Description = "Verify that tests run in isolation"
                UnitTest = (fun env ->
                                let changer = ChangeAble()
                                let mainExpected = changer.X
                                
                                let test1 = 
                                    Test({
                                            Description = "First test to test isolation";
                                            UnitTest = (fun env ->
                                                            let expected = 1;
                                                            do changer.Increment ()

                                                            let actual = changer.X

                                                            actual |> expectsToBe expected "changer did not increment correctly expected %d but got %d"
                                                        )
                                        })

                                let test2 = 
                                    Test({
                                            Description = "Second test to test isolation";
                                            UnitTest = (fun env ->
                                                            let expected = 1;
                                                            do changer.Increment ()

                                                            let actual = changer.X

                                                            actual |> expectsToBe expected "changer did not increment correctly expected %d but got %d"
                                                        )
                                         })

                                let results = [test1; test2] |> FeldSpar.Console.Helpers.Data.runTests
                                let isolatedResults = results |> reduceToFailures |> Seq.isEmpty

                                let mainActual = changer.X

                                verify
                                    {
                                        let! testsWhereIsolatedFromEachother = isolatedResults |> isTrue (GeneralFailure("tests failed to manipulate the changer correctly or in isolation"))
                                        let! testsWhereIsolatedFromMain = mainActual |> expectsToBe mainExpected "tests did not run in isolation. changer.X was expected to be %d but instead was %d"
                                        return Success
                                    }
                            )
            })
