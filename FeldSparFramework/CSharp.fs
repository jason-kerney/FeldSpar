namespace FeldSpar.ClrInterop
open System
open FeldSpar.Framework
open FeldSpar.Framework.Engine

type TestEventArgs (name:string) =
    inherit EventArgs ()

    member this.Name 
        with get () = name

type TestCompeteEventArgs (name:string, result:TestResult) =
    inherit TestEventArgs (name)

    member this.TestResult 
        with get () = result

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
        | Found(token) -> token.Name |> found
        | Running(token) -> token.Name |> running
        | Finished(token, result) -> token.Name |> complete result

        ()

    [<CLIEvent>]
    member this.TestFound = foundEvent.Publish

    [<CLIEvent>]
    member this.TestRunning = runningEvent.Publish

    [<CLIEvent>]
    member this.TestFinished = testCompletedEvent.Publish

    member this.FindTests (assembly:System.Reflection.Assembly) =
        assembly |> findTestsAndReport report |> ignore

    member this.RunTests (assembly:System.Reflection.Assembly) =
        assembly |> runTestsAndReport report |> ignore