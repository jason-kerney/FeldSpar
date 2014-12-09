namespace FeldSpar.Api.Engine.ClrInterop
open System
open FeldSpar.Framework
open FeldSpar.Framework.Engine

/// <summary>
/// A class to pass data with test events
/// </summary>
type TestEventArgs (name:string) =
    inherit EventArgs ()

    /// <summary>
    /// The name of the test that raised the event
    /// </summary>
    member this.Name 
        with get () = name

/// <summary>
/// A class to pass data when a test finishes
/// </summary>
type TestCompeteEventArgs (name:string, result:TestResult) =
    inherit TestEventArgs (name)

    /// <summary>
    /// The result of the test
    /// </summary>
    member this.TestResult 
        with get () = result

/// <summary>
/// A class to wrap execution of tests
/// </summary>
type Engine () = 
    let foundEvent = new Event<TestEventArgs>()
    let runningEvent = new Event<TestEventArgs>()
    let testCompletedEvent = new Event<TestCompeteEventArgs>()

    let found name = 
        foundEvent.Trigger(TestEventArgs(name))

    let running name =
        runningEvent.Trigger(TestEventArgs(name))

    let complete result name =
        testCompletedEvent.Trigger(TestCompeteEventArgs(name, result))

    let report (status:ExecutionStatus) =
        match status with
        | Found(token) -> token.TestName |> found
        | Running(token) -> token.TestName |> running
        | Finished(token, result) -> token.TestName |> complete result

        ()

    let doWork (work:(ExecutionStatus -> unit) -> IToken -> _) (token:IToken) =
        token |> work report |> ignore
        

    /// <summary>
    /// Event Raised when a test is found. Finding happens twice. Once when executing a find and once when running the tests
    /// </summary>
    [<CLIEvent>]
    member this.TestFound = foundEvent.Publish

    /// <summary>
    /// Event is raised when a test starts running
    /// </summary>
    [<CLIEvent>]
    member this.TestRunning = runningEvent.Publish

    /// <summary>
    /// Event is raised when a test finishes running
    /// </summary>
    [<CLIEvent>]
    member this.TestFinished = testCompletedEvent.Publish

    /// <summary>
    /// Looks for tests in assembly
    /// </summary>
    /// <param name="token">the token for the test assembly</param>
    member this.FindTests (token:IToken) =
        token |> doWork (findTestsAndReport false)

    /// <summary>
    /// Finds and runs all tests in a given assembly
    /// </summary>
    /// <param name="token">the token for the test assembly</param>
    member this.RunTests (token:IToken) =
        token |> doWork (runTestsAndReport false)
