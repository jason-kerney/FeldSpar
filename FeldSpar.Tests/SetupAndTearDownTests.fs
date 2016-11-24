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

module ``Setup and teardown should`` =
    let getFlowContinue r = 
        match r with
        | ContinueFlow(r, d, c) -> (r, d, c)
        | _ -> failwith "Unexpected result from setup"

    let getFlowFailure r =
        match r with
        | FlowFailed(reason) -> reason
        | _ -> failwith "Unexpected result from setup"

    let successfulSetup = beforeTest (fun context -> Success, 42, context)
    let successfulTest = (fun _env _data -> Success)

    let ``return a TestEnvironment -> SetupFlow<'a> when beforeTest is called and it returns int data`` =
        Test(fun env ->
            let setup = successfulSetup

            (setup env).GetType () |> expectsToBe (ContinueFlow(Success, 42, env).GetType ())
        )

    let ``return (Success, data, test environment) when setul is successful`` =
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
                beforeTest (fun env -> Success, "data", {env with TestName = env.TestName + "_Setup" })

            let (_, _, context) = getFlowContinue (setup env)

            context.TestName |> expectsToBe (env.TestName + "_Setup")
        ) 

    let ``returns TestFlow.SetupFailure when the setup returns something other then success`` =
        Test(fun env ->
            let failure = GeneralFailure("This is an error")
            let setup = 
                beforeTest(fun env -> Failure(failure), 32, env )

            let result = setup env |> getFlowFailure

            result |> expectsToBe (SetupFailure(failure))
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

    let ``combines a test and a setup to return TestEnvironment -> SetupFlow<'a>`` =
        Test(fun env ->
            let test = successfulSetup
                        |> theTest successfulTest

            (test env).GetType () |> expectsToBe (ContinueFlow(Success, 42, env).GetType ())
        )

    let ``returns the failure when the test fails`` =
        Test(fun env ->
            let test = successfulSetup
                        |> theTest (fun _ _ -> Failure(GeneralFailure("The Test Failed")))

            test env |> getFlowFailure |> expectsToBe (GeneralFailure("The Test Failed"))
        )

    let ``returns the setup failure without running the test if the setup fails`` =
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

            let result = test env |> getFlowFailure

            verify
                {
                    let! success = result |> expectsToBe (SetupFailure (failure))
                    let! success = called |> expectsToBe false |> withFailComment "the test was run when it shouldn't have been"
                    return Success
                }

        )

    let ``returns an exception failure if the test throws an exception`` =
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

    let ``return TestTemplate when setup, test and teardown are combined`` =
        Test(fun env ->
            let _test : TestEnvironment -> TestResult = 
                successfulSetup
                |> theTest successfulTest
                |> afterTheTest (fun env result data -> Success)

            Success
        )
        