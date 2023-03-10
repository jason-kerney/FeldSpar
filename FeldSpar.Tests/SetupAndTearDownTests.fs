namespace FeldSpar.Console.Tests
open System
open System.Text.RegularExpressions
open FeldSpar.Console.Helpers
open FeldSpar.Console.Helpers.Data
open FeldSpar.Framework
open FeldSpar.Framework.TestSummaryUtilities
open FeldSpar.Framework.Engine
open FeldSpar.Framework.Verification
open FeldSpar.Framework.Verification.ChecksClean
open FeldSpar.Framework.Verification.ApprovalsSupport
open FeldSpar.Framework.Utilities

module SetupAndTearDownTestingSupport = 
    let getFlowContinue r = 
        match r with
        | ContinueFlow(r, d, c) -> (r, d, c)
        | _ -> failwith "Unexpected result from setup"

    let getFlowFailure r =
        match r with
        | FlowFailed(reason, _) -> reason
        | _ -> failwith "Unexpected result from setup"

    let successfulSetup = beforeTest (fun context -> Success, 42, context)
    let throwsSetup = beforeTest (fun _env -> failwith "setup threw exception")
    let successfulTest = (fun _env _data -> Success)

    let buildSuccessfulSetup env = Success, successfulSetup, env

    let expectsToBeSuccessful actual =
        match actual with
        | ContinueFlow (Success, _, _) -> Success
        | failure -> 
            sprintf "%A expected to be ContinueFlow (Success, _, _)" failure
            |> ExpectationFailure 
            |> Failure

    let expectsToBeFailure actual =
        match actual with
        | ContinueFlow _ -> 
            sprintf "ContinueFlow expetcted to be FlowFailed"
            |> ExpectationFailure
            |> Failure
        | _ -> Success

    let expectsToBeFailureWith ((failure: FailureType), (data : 'a)) actual =
        match actual with
        | ContinueFlow _ -> 
            sprintf "(ContinueFlow expetcted to be FlowFailed"
            |> ExpectationFailure
            |> Failure
        | FlowFailed (actualFailure, actualData) when failure = actualFailure && actualData = data-> Success
        | FlowFailed (actualFailure, actualData) -> 
            sprintf "FlowFailed (%A, %A) expected to be FlowFailed (%A, %A)" actualFailure actualData failure data
            |> ExpectationFailure
            |> Failure

module ``Setup should`` =
    open SetupAndTearDownTestingSupport

    let ``return a TestEnvironment -> SetupFlow<'a> when beforeTest is called`` =
        Test(fun env ->
                let _test : TestEnvironment -> SetupFlow<int> = 
                    beforeTest (fun env -> Success, 18, env)
                
                Success
        )

    let ``return (Success, data, test environment) when successful`` =
        Test(fun env ->
            let setup = 
                beforeTest (fun context -> Success, "data", context)

            let setupResult = setup env

            let (testResult, data, context) = getFlowContinue setupResult

            verify 
                {
                    let! result = (testResult, data) |> expectsToBe (Success, "data")

                    let! result = env.AssemblyPath |> expectsToBe context.AssemblyPath |> withFailComment "Environment Changed"
                    let! result = env.CanonicalizedContainerName |> expectsToBe context.CanonicalizedContainerName |> withFailComment "Environment Changed"
                    let! result = env.ContainerName |> expectsToBe context.ContainerName |> withFailComment "Environment Changed"
                    let! result = env.GoldStandardPath |> expectsToBe context.GoldStandardPath |> withFailComment "Environment Changed"
                    let! result = env.TestName |> expectsToBe context.TestName |> withFailComment "Environment Changed"

                    return Success
                }
        )

    let ``be able to return a differnt environment then what was passed in`` =
        Test(fun env ->
            let setup =
                beforeTest (fun env -> Success, "data", {env with TestName = "_Setup" })

            let (_, _, context) = getFlowContinue (setup env)

            verify
                {
                    let! a = context.TestName |> expectsToBe "_Setup"
                    let! b = context.TestName |> expectsNotToBe env.TestName
                    return Success
                }
        ) 

    let ``returns TestFlow.SetupFailure when the setup returns something other then success`` =
        Test(fun env ->
            let failure = GeneralFailure("This is an error")
            let setup = 
                beforeTest(fun env -> Failure(failure), 32, env )

            let result = setup env

            result |> expectsToBeFailureWith (SetupFailure failure, Some 32)
        )

    let ``returns TestFlow.SetupFailure when the setup throws exception`` =
        Test(fun env ->
            let msg = "this is a bad setup"
            let setup = 
                beforeTest(fun env -> 
                    failwith msg
                )

            let result = setup env |> getFlowFailure

            let reason = 
                match result with
                | SetupFailure(ExceptionFailure(r)) -> r.Message
                | _ -> failwith "Unexpected Result"

            reason |> expectsToBe msg
        )

module ``A test should`` =
    open SetupAndTearDownTestingSupport

    let ``not require a setup`` =
        Test(fun env ->
            let _test : TestEnvironment -> SetupFlow<unit> = startWithTheTest (fun _env -> Success)

            Success
        )

    let ``not require a teardown`` =
        Test(
            beforeTest buildSuccessfulSetup
            |> endWithTest
                (fun env setup ->
                    let _test : TestTemplate = 
                        setup |> endWithTest (fun _env _data -> Success)

                    Success
                )
        )

module ``A test without a teardown should`` =
    open SetupAndTearDownTestingSupport
    
    let ``return success if the test is successful`` =
        Test(
            beforeTest buildSuccessfulSetup
            |> endWithTest
                (
                    fun env setup ->
                        let test = setup |> endWithTest (fun _env _data -> Success)

                        test env |> expectsToBe Success
                )
        )

    let ``should be called if the setup succeeds`` =
        Test(
            beforeTest buildSuccessfulSetup
            |> endWithTest
                (
                    fun env setup ->
                        let mutable wasCalled = false
                        let test =
                            setup |> endWithTest 
                                (fun _env _data ->
                                    wasCalled <- true
                                    Success
                                )

                        test env |> ignore
                        wasCalled |> expectsToBe true |> withFailComment "the test wasn't called"
                )
        )

    let ``should not be called if setup fails`` =
        Test(
            beforeTest 
                (fun env ->
                    let failingSetup = beforeTest (fun env -> failResult "setup failed", (), env)
                    Success, failingSetup, env
                )
            |> endWithTest
                (fun env failingSetup ->
                    let mutable wasCalled = false
                    let test = 
                        failingSetup |> endWithTest 
                            (fun env context -> 
                                wasCalled <- true
                                Success
                            )
                    
                    test env |> ignore

                    wasCalled |> expectsToBe false |> withFailComment "test was called even though setup failed"
                )
        )

module ``A test without a setup should`` =
    open SetupAndTearDownTestingSupport
    
    let ``return success if the test is successful`` =
        Test(fun env ->
            let test = startWithTheTest (fun _env -> Success)
            let actual = test env

            actual |> expectsToBeSuccessful
        )

    let ``returns the failure if the test fails`` =
        Test(fun env ->
            let test = startWithTheTest (fun _env -> failResult "the test failed")
            let actual = test env
            
            actual |> expectsToBeFailureWith (GeneralFailure "the test failed", Some ())
        )

    let ``calls teardown even if it throws an exception`` =
        Test(fun env ->
            let mutable wasCalled = false
            let test = 
                startWithTheTest (fun _env -> failwith "test exploded")
                |> afterTheTest 
                    (fun _env _resualt _data ->
                        wasCalled <- true
                        Success
                    )

            test env |> ignore

            wasCalled |> expectsToBe true |> withFailComment "Teardown was not called"
        )

module ``A test with a setup should`` =
    open SetupAndTearDownTestingSupport

    let ``return TestEnvironment -> SetupFlow<'a>`` =
        Test(
            beforeTest buildSuccessfulSetup
            |> endWithTest
                (fun env setup ->
                    let _test : TestEnvironment -> SetupFlow<int> = setup |> theTest successfulTest

                    Success
                )
        )

    let ``returns success when the test returns success`` =
        Test(
            beforeTest buildSuccessfulSetup
            |> endWithTest
                (fun env setup ->
                    let test = setup |> theTest successfulTest

                    test env |> expectsToBeSuccessful
                )
        )

    let ``return the failure when the test fails`` =
        Test(
            beforeTest buildSuccessfulSetup
            |> endWithTest
                (fun env setup ->
                    let actualFailure = GeneralFailure("The Test Failed")
                    let test = setup |> theTest (fun _ _ -> Failure(actualFailure))

                    test env |> expectsToBeFailureWith (actualFailure, Some 42)
                )
        )

    let ``return the setup failure without running the test if the setup fails`` =
        Test(
            beforeTest 
                (fun env ->
                    let failure = GeneralFailure("This is an error")
                    let setup = beforeTest(fun env -> Failure(failure), 32, env )

                    Success, (failure, setup), env
                )
            |> endWithTest
                (fun env (failure, setup) ->
                    let mutable called = false
            
                    let test = 
                        setup |> theTest 
                            (fun _ _ -> 
                                called <- true
                                failwith "this should not run"
                            )

                    let result = test env

                    verify
                        {
                            let! success = result |> expectsToBeFailureWith (SetupFailure (failure), Some 32)
                            let! success = called |> expectsToBe false |> withFailComment "the test was run when it shouldn't have been"
                            return Success
                        }
                )
        )

    let ``return an exception failure if the test throws an exception`` =
        Test(
            beforeTest buildSuccessfulSetup
            |> endWithTest
                (fun env setup ->
                    let expectedMessage = "the test explodes"
                    let test = setup |> theTest (fun _ _ -> failwith expectedMessage)

                    let reason = test env |> getFlowFailure

                    let actualMessage = 
                        match reason with
                        | ExceptionFailure (ex) -> ex.Message
                        | _ -> failwith "Unexpected test resualt"

                    actualMessage |> expectsToBe expectedMessage
                )
        )

module ``Teardown should`` =
    open SetupAndTearDownTestingSupport

    let ``Get Successful Setup and Teardown`` (env : TestEnvironment) = Success, (successfulSetup |> theTest successfulTest), env

    let ``return TestTemplate when setup, test and teardown are combined`` =
        Test(
            beforeTest ``Get Successful Setup and Teardown``
            |> endWithTest
                (fun env (passingTest) ->
                    let _test : TestTemplate = 
                        passingTest |> afterTheTest (fun _env _result _data -> Success)

                    Success
                )
        )

    let ``return success when everything passes`` =
        Test(
            beforeTest ``Get Successful Setup and Teardown``
            |> endWithTest
                (fun env passingTest ->
                    let test =
                        passingTest |> afterTheTest (fun _env _result _data -> Success)

                    test env
                )
        )

    let ``be called if everything passes`` =
        Test(
            beforeTest ``Get Successful Setup and Teardown``
            |> endWithTest
                (fun env passingTest->
                    let mutable value = 0
                    let test =
                        passingTest 
                        |> afterTheTest
                            (fun _env _result _data ->
                                value <- 42
                                Success
                            )

                    test env |> ignore

                    value |> expectsToBe 42 |> withFailComment "teardown was not called"
                )
        )

module ``When setup fails teardown should`` =
    open SetupAndTearDownTestingSupport
    let ``Get test that throws durring setup`` (env : TestEnvironment) = Success, (throwsSetup |> theTest successfulTest), env

    let ``Get test that fails durring setup with`` data env =
        let failMessage = "setup failed"
        let aTestThatFailsInSetup = beforeTest (fun env -> failResult failMessage, data, env) |> theTest successfulTest

        Success, (aTestThatFailsInSetup, failMessage, data), env

    let ``be called`` =
        Test(
            beforeTest ``Get test that throws durring setup``
            |> endWithTest
                (fun env setupThatThrows ->
                    let mutable called = false
                    let test =
                        setupThatThrows
                        |> afterTheTest 
                            (fun _env _result _data ->
                                called <- true
                                Success
                            ) 

                    test env |> ignore

                    called |> expectsToBe true |> withFailComment "teardown not called"
                )
        )

    let ``be called with no data if setup threw an exception`` =
        Test(
            beforeTest ``Get test that throws durring setup``
            |> endWithTest
                (fun env setupThatThrows ->
                    let mutable setupData : string Option = Some ("beginning data")
                    let test =
                        setupThatThrows
                        |> afterTheTest 
                            (fun _env _result data ->
                                setupData <- data
                                Success
                            )

                    test env |> ignore

                    setupData |> expectsToBe None
                )
        )

    let ``get the failure`` =
        Test(
            ``Get test that fails durring setup with`` 42 
            |> beforeTest
            |> endWithTest
                (fun env (setupThatFails, failMessage, _) ->
                    let mutable testResult = Success
                    let test =
                        setupThatFails
                        |> afterTheTest
                            (fun _env result _data ->
                                testResult <- result
                                Success
                            )

                    test env |> ignore 

                    testResult |> expectsToBe (Failure (SetupFailure (GeneralFailure failMessage)))
                )
        )

    let ``recieve the setup data`` =
        Test(
            ``Get test that fails durring setup with`` "data from setup"
            |> beforeTest
            |> endWithTest
                (fun env (setupThatFails, failMessage, givenData) ->
                    let mutable dataFromTest = Some "Not Good Data"
                    let test =
                        setupThatFails
                        |> afterTheTest
                            (fun _env _result data ->
                                dataFromTest <- data
                                Success
                            )

                    test env |> ignore
                    dataFromTest |> expectsToBe (Some givenData)
                )
        )

    let ``return with the setup failure even when teardown succeeds`` =
        Test(
            ``Get test that fails durring setup with`` "setup failed"
            |> beforeTest
            |> endWithTest
                (fun env (setupThatFails, failMessage, givenData) ->
                    let test =
                        setupThatFails
                        |> afterTheTest (fun _env _result _data -> Success)

                    let result = test env
                    result |> expectsToBe (Failure (SetupFailure (GeneralFailure failMessage)))
                )
        )

module ``When the test fails teardown should`` =
    open SetupAndTearDownTestingSupport

    let ``build a failing test`` env = 
        let testFailure = failResult "test fails"
        let failingTest = 
            successfulSetup 
            |> theTest (fun _context _data -> testFailure)

        Success, (failingTest, testFailure), env

    let ``build a test that throws an exception`` env = 
            let data = 32
            let failingTest = 
                (fun context -> Success, data, context) 
                |> beforeTest 
                |> theTest (fun _context _data -> failwith "Test explosion")

            Success, (failingTest, data), env

    let ``be called`` =
        Test(
            beforeTest ``build a failing test``
            |> endWithTest
                (fun env (failingTest, _) ->
                    let mutable wasCalled = false
                    let test =
                        failingTest
                        |> afterTheTest
                            (fun _env _result _data ->
                                wasCalled <- true
                                Success
                            )

                    test env |> ignore

                    wasCalled |> expectsToBe true |> withFailComment "teardown was not called"
                )
        )

    let ``be called with setup data even if failure is an exception`` =
        Test(
            beforeTest ``build a test that throws an exception``
            |> endWithTest 
                (fun env (throwingTest, expectedSetupData) ->
                    let mutable actual = Some 0
                    let test = 
                        throwingTest
                        |> afterTheTest
                            (fun _env _result data ->
                                actual <- data
                                Success
                            )

                    test env |> ignore
                    actual |> expectsToBe (Some expectedSetupData) |> withFailComment "teardown was not called with correct data"
                )
        )

    let ``be called with the failure from the test`` =
        Test(
            beforeTest ``build a failing test``
            |> endWithTest
                (fun env (failingTest, testFailure) ->
                    let mutable actual = Success
                    let test =
                        failingTest
                        |> afterTheTest
                            (fun env result _data ->
                                actual <- result
                                Success
                            )

                    test env |> ignore

                    actual |> expectsToBe testFailure
                )
        )

    let ``return the test failure if the teardown succeeds`` =
        Test(
            beforeTest ``build a failing test``
            |> endWithTest
                (fun env (failingTest, testFailure) ->
                    let test =
                        failingTest
                        |> afterTheTest (fun _env _result _data -> Success)

                    let actual = test env
                    actual |> expectsToBe testFailure
                )
        )