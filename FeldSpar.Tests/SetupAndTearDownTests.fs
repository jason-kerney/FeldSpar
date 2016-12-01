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
    let throwsSetup = beforeTest<string> (fun _env -> failwith "setup threw exception")
    let successfulTest = (fun _env _data -> Success)

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
            let setup = successfulSetup

            (setup env).GetType () |> expectsToBe (ContinueFlow(Success, 42, env).GetType ())
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
            let template : TestEnvironment -> SetupFlow<unit> = startWithTheTest (fun _env -> Success)
            Success
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

module ``A test with a setup should`` =
    open SetupAndTearDownTestingSupport

    let ``return TestEnvironment -> SetupFlow<'a>`` =
        Test(fun env ->
            let _test : TestEnvironment -> SetupFlow<int> = 
                successfulSetup
                |> theTest successfulTest

            Success
        )

    let ``returns success when the test returns success`` =
        Test(fun env ->
            let test = 
                successfulSetup
                |> theTest successfulTest

            test env |> expectsToBeSuccessful
        )

    let ``return the failure when the test fails`` =
        Test(fun env ->
            let actualFailure = GeneralFailure("The Test Failed")
            let test = successfulSetup
                        |> theTest (fun _ _ -> Failure(actualFailure))

            test env |> expectsToBeFailureWith (actualFailure, Some 42)
        )

    let ``return the setup failure without running the test if the setup fails`` =
        Test(fun env ->
            let mutable called = false
            
            let failure = GeneralFailure("This is an error")
            let setup = 
                beforeTest(fun env -> Failure(failure), 32, env )

            let test = 
                setup
                |> theTest 
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

    let ``return an exception failure if the test throws an exception`` =
        Test(fun env ->
            let expectedMessage = "the test explodes"
            let test =
                successfulSetup
                |> theTest (fun _ _ -> failwith expectedMessage)

            let reason = 
                test env 
                |> getFlowFailure

            let actualMessage = 
                match reason with
                | ExceptionFailure (ex) -> ex.Message
                | _ -> failwith "Unexpected test resualt"

            actualMessage |> expectsToBe expectedMessage
        )

module ``Teardown should`` =
    open SetupAndTearDownTestingSupport

    let ``return TestTemplate when setup, test and teardown are combined`` =
        Test(fun env ->
            let _test : TestTemplate = 
                successfulSetup
                |> theTest successfulTest
                |> afterTheTest (fun _env _result _data -> Success)

            Success
        )

    let ``return success when everything passes`` =
        Test(fun env ->
            let test =
                successfulSetup
                |> theTest successfulTest
                |> afterTheTest (fun _env _result _data -> Success)

            test env
        )

    let ``be called if everything passes`` =
        Test(fun env ->
            let mutable value = 0
            let test =
                successfulSetup
                |> theTest successfulTest
                |> afterTheTest
                    (fun _env _result _data ->
                        value <- 42
                        Success
                    )

            test env |> ignore

            value |> expectsToBe 42 |> withFailComment "teardown was not called"
        )

module ``When setup fails teardown should`` =
    open SetupAndTearDownTestingSupport

    let ``be called`` =
        Test(fun env ->
            let mutable called = false
            let test =
                throwsSetup
                |> theTest successfulTest
                |> afterTheTest 
                    (fun _env _result _data ->
                        called <- true
                        Success
                    ) 

            test env |> ignore

            called |> expectsToBe true |> withFailComment "teardown not called"
        )

    let ``be called with no data if setup threw an exception`` =
        Test(fun env ->
            let mutable setupData : string Option = Some ("beginning data")
            let test =
                throwsSetup
                |> theTest successfulTest
                |> afterTheTest 
                    (fun _env _result data ->
                        setupData <- data
                        Success
                    )

            test env |> ignore

            setupData |> expectsToBe None
        )

    let ``get the failure`` =
        Test(fun env ->
            let mutable testResult = Success
            let failMessage = "setup failed"
            let test =
                beforeTest (fun env -> failResult failMessage, 42, env)
                |> theTest successfulTest
                |> afterTheTest
                    (fun _env result _data ->
                        testResult <- result
                        Success
                    )

            test env |> ignore 

            testResult |> expectsToBe (Failure (SetupFailure (GeneralFailure failMessage)))
        )

    let ``recieve the setup data`` =
        Test(fun env ->
            let givenData = "data from setup"
            let mutable dataFromTest = Some "Not Good Data"
            let test =
                beforeTest (fun env -> failResult "setup failed", givenData, env)
                |> theTest successfulTest
                |> afterTheTest
                    (fun _env _result data ->
                        dataFromTest <- data
                        Success
                    )

            test env |> ignore
            dataFromTest |> expectsToBe (Some givenData)
        )

    let ``return with the setup failure even when teardown succeeds`` =
        Test(fun env ->
            let failMessage = "setup failed"
            let test =
                beforeTest (fun env -> failResult failMessage, (), env)
                |> theTest successfulTest
                |> afterTheTest (fun _env _result _data -> Success)

            let result = test env
            result |> expectsToBe (Failure (SetupFailure (GeneralFailure failMessage)))
        )

module ``When the test fails teardown should`` =
    open SetupAndTearDownTestingSupport

    let ``be called`` =
        Test(fun env ->
            let mutable wasCalled = false
            let test =
                successfulSetup
                |> theTest successfulTest
                |> afterTheTest
                    (fun _env _result _data ->
                        wasCalled <- true
                        Success
                    )

            test env |> ignore

            wasCalled |> expectsToBe true |> withFailComment "teardown was not called"
        )

    let ``be called with setup data even if failure is an exception`` =
        Test(fun env ->
            let mutable actual = Some 0
            let test = 
                beforeTest (fun env -> Success, 32, env)
                |> theTest (fun _env _data -> failwith "Test explosion")
                |> afterTheTest
                    (fun _env _result data ->
                        actual <- data
                        Success
                    )

            test env |> ignore
            actual |> expectsToBe (Some 32) |> withFailComment "teardown was not called"
        )

    let ``be called with the failure from the test`` =
        Test(fun env ->
            let mutable actual = Success
            let failure = failResult "test failed"
            let test =
                successfulSetup
                |> theTest (fun _env _data -> failure)
                |> afterTheTest
                    (fun env result _data ->
                        actual <- result
                        Success
                    )

            test env |> ignore

            actual |> expectsToBe failure
        )

    let ``return the test failure if the teardown succeeds`` =
        Test(fun env ->
            let failure = failResult "test failed"
            let test =
                successfulSetup
                |> theTest (fun _env _data -> failure)
                |> afterTheTest (fun _env _result _data -> Success)

            let actual = test env
            actual |> expectsToBe failure
        )