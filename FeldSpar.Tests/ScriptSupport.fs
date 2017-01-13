namespace FeldSpar.ScriptSupport.Tests

module ``When Scripting FeldSpar Should`` =
    open FeldSpar.Framework
    open FeldSpar.Framework.Engine
    open FeldSpar.Framework.Verification
    open FeldSpar.Framework.ScriptSupport 

    type TestMap = 
        {
            ContainerName : string
            TestTestName : string
            TestResult : TestResult
        }

    let execute env (Test test) = test env

    let ``convert from scripting test data in to a Test`` = 
        Test(fun env ->
            let name = "This is a script test"
            let failMessage = "a scripting failure"

            let expectsToHaveTheCorrectTests (Test(f)) =
                let newError s = 
                    Failure(ExpectationFailure(sprintf "Did not return the correct test. Got: %A" s))
                match (f env) with
                | Failure(GeneralFailure(msg)) ->
                    if msg = failMessage then Success
                    else newError msg
                | result -> newError result

            let datum = 
                {
                    Name = name
                    Case = Test(fun _ -> failResult failMessage)
                }

            let test = datum |> asTest
            verify
                {
                    let! containerNameIsCorrect = test.TestContainerName |> expectsToBe "Script"
                    let! nameIsCorrect = test.TestName |> expectsToBe name
                    let! testIsCorrect = test.Test |> expectsToHaveTheCorrectTests
                    return Success
                }
        )

    let ``convert from a list of scripting test data to an array of Tests`` =
        Test(fun env ->
            let data =
                [
                    {
                        Name = "A successful test"
                        Case = Test (fun _ -> Success)
                    }
                    {
                        Name = "A failing test"
                        Case = Test (fun _ -> failResult "a failing test")
                    }
                    {
                        Name = "An Ignored Test"
                        Case = Test (fun _ -> ``Not Yet Implemented``)
                    }
                ]

            let tests = data |> asTests

            tests
                |> Array.map (fun test ->
                                {
                                    TestTestName = test.TestName
                                    ContainerName = test.TestContainerName
                                    TestResult = execute env (test.Test)
                                }
                             ) 
                |> checkAllAgainstStandardBy env (sprintf "%A\n")
        )

    let ``run a list of scripting test data and return Tests Results`` =
        Test(fun env ->
                let data =
                    [
                        {
                            Name = "A failing test"
                            Case = Test (fun _ -> failResult "This test fails")
                        }
                        {
                            Name = "An ignored test"
                            Case = Test (fun _ -> ignoreWith "This test is ignored")
                        }
                        {
                            Name = "A passing test"
                            Case = Test (fun _ -> Success)
                        }
                    ]

                let config : AssemblyConfiguration = 
                        { 
                            Reporters = []
                        }

                data
                    |> runTests config (ignore)
                    |> List.sortBy (fun item -> (item.TestContainerName, item.TestName))
                    |> checkAllAgainstStandardBy env (sprintf "%A\n")
        )


    let ``allow for a short hand syntax for creating scripting test data`` =
        Test(fun env ->
            let name = "this is an ignored test"
            let ignoreComment = "this is an ignore comment"
            let testData = name  |> testedWith (fun _ -> ignoreWith ignoreComment)
            verify
                {
                    let! nameIsCorrect = testData.Name |> expectsToBe name |> withFailComment "name was incorrect"
                    let! testIsCorrect = testData.Case |> execute env |> expectsToBe (Failure(Ignored(ignoreComment)))
                    return Success
                }
        )