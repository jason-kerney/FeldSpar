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
    let runningItems = new System.Collections.Generic.List<string>()
    let finnishedItems = new System.Collections.Generic.List<string * TestResult>()

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

    let getAssembly path = 
        path |> System.Reflection.Assembly.LoadFile

    let doWork (work:(ExecutionStatus -> unit) -> Reflection.Assembly -> _) path =
        path |> getAssembly |> work report |> ignore
        

    [<CLIEvent>]
    member this.TestFound = foundEvent.Publish

    [<CLIEvent>]
    member this.TestRunning = runningEvent.Publish

    [<CLIEvent>]
    member this.TestFinished = testCompletedEvent.Publish

    member this.FindTests (path:string) =
        path |> doWork findTestsAndReport

    member this.RunTests (path:string) =
        path |> doWork runTestsAndReport