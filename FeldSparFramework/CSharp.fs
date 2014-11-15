namespace FeldSpar.ClrInterop
open System
open FeldSpar.Framework
open FeldSpar.Framework.Engine
open System.ComponentModel
open System.Runtime.CompilerServices
open System.Collections.ObjectModel
open System.Collections.Generic
open System.IO
open System.Windows.Input

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
        path |> Reflection.Assembly.LoadFile

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

type TestStatus = 
    | None = 0
    | Running = 1
    | Success = 2
    | Failure = 3
    | Ignored = 4

type PropertyNotifyBase () =
    let notify = new Event<_, _>()

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = notify.Publish

    abstract member OnPropertyChanged : string -> unit
    default this.OnPropertyChanged ([<CallerMemberName>]propertyName:string) =
        notify.Trigger(this, new PropertyChangedEventArgs(propertyName))

    member this.OnPropertyChanged () = this.OnPropertyChanged(null)

type ITestDetailModel = 
    abstract member Name : string with get, set
    abstract member Status : TestStatus with get, set
    abstract member FailDetail : string with get, set
    abstract member AssemblyName : string with get, set
    abstract member Parent : ITestAssemblyModel with get, set

and ITestAssemblyModel =
    abstract member IsVisible : bool with get, set
    abstract member IsRunning : bool with get, set
    abstract member Name : string with get
    abstract member AssemblyPath : string with get
    abstract member Tests : ObservableCollection<ITestDetailModel> with get
    abstract member Results : ObservableCollection<TestResult> with get
    abstract member RunCommand : System.Windows.Input.ICommand with get
    abstract member ToggleVisibilityCommand : System.Windows.Input.ICommand with get
    abstract member Run : obj -> unit

type ITestsMainModel =
    abstract IsRunning : bool with get, set
    abstract Description : string with get
    abstract Selected : ITestDetailModel with get, set
    abstract Assemblies : System.Collections.ObjectModel.ObservableCollection<ITestAssemblyModel> with get, set
    abstract Results : TestResult array with get
    abstract Tests : ITestDetailModel array with get
    abstract RunCommand : System.Windows.Input.ICommand with get
    abstract AddCommand : System.Windows.Input.ICommand with get

module Defaults =
    let emptyCommand = 
        {new System.Windows.Input.ICommand with
            [<CLIEvent>]
            member this.CanExecuteChanged = (new Event<_, _>()).Publish
            member this.CanExecute param = true
            member this.Execute param = ()
        }

    let emptyTestAssemblyModel = 
        {new ITestAssemblyModel with
            member this.IsVisible 
                with get () = false
                and set value = ()
            member this.IsRunning
                with get () = false
                and set value = ()
            member this.Name with get () = String.Empty
            member this.AssemblyPath with get () = String.Empty
            member this.Tests 
                with get () = new System.Collections.ObjectModel.ObservableCollection<ITestDetailModel>()
            member this.Results 
                with get () = new System.Collections.ObjectModel.ObservableCollection<TestResult>()
            member this.RunCommand with get () = emptyCommand
            member this.ToggleVisibilityCommand with get () = emptyCommand
            member this.Run param = ()
        }

    let emptyTestDetailModel = 
        {new ITestDetailModel with
            member this.Name
                with get () = String.Empty
                and set value = ()
            member this.Status 
                with get () = TestStatus.None
                and set value = ()
            member this.FailDetail
                with get () = String.Empty
                and set value = ()
            member this.AssemblyName
                with get () = String.Empty
                and set value = ()
            member this.Parent
                with get () = emptyTestAssemblyModel
                and set value = ()
        }

// Thank you:
// http://wpftutorial.net/DelegateCommand.html
type DelegateCommand (execute:Action<obj>, canExecute:Predicate<obj>) =
    let notify = new Event<_, _>()


    interface ICommand with
        [<CLIEvent>]
        member this.CanExecuteChanged = notify.Publish

        member this.CanExecute param =
            if canExecute = null then true
            else canExecute.Invoke param

        member this.Execute param = execute.Invoke param

    new (execute:Action<obj>) = DelegateCommand(execute, fun _ -> true)


    member this.RaiseCanExecuteChanged () = notify.Trigger(this, EventArgs.Empty)

type TestDetailModel () =
    inherit PropertyNotifyBase ()

    let mutable name = ""
    let mutable status = TestStatus.Success
    let mutable failDetail = ""
    let mutable assemblyName = ""
    let mutable parent :ITestAssemblyModel = Defaults.emptyTestAssemblyModel

    member this.Name
        with get () = name
        and set value = name <- value

    member this.Status
        with get () = status
        and set value = status <- value

    member this.FailDetail
        with get () = failDetail
        and set value = failDetail <- value

    member this.AssemblyName
        with get () = assemblyName
        and set value = assemblyName <- value

    member this.Parent
        with get () = parent
        and set value = parent <- value
    
    interface ITestDetailModel with
        member this.Name
            with get () = name
            and set value = name <- value

        member this.Status
            with get () = status
            and set value = status <- value

        member this.FailDetail
            with get () = failDetail
            and set value = failDetail <- value

        member this.AssemblyName
            with get () = assemblyName
            and set value = assemblyName <- value

        member this.Parent
            with get () = parent
            and set value = parent <- value

type TestAssemblyModel (assemblyPath) as this =
    inherit PropertyNotifyBase ()

    let engine = new Engine()
    let tests = new ObservableCollection<ITestDetailModel>()
    let results = new ObservableCollection<TestResult>()
    let knownTests = new Dictionary<string, ITestDetailModel>()
    let name = Path.GetFileName(assemblyPath)

    let mutable isRunning = true
    let mutable isVisible = true

    let convert (result:TestResult) = 
        match result with
        | Success -> (TestStatus.Success, String.Empty)
        | Failure(GeneralFailure(msg)) -> (TestStatus.Failure, "General Failure" + Environment.NewLine + msg)
        | Failure(ExpectationFailure(msg)) -> (TestStatus.Failure, "Expectation Not Met" + Environment.NewLine + msg)
        | Failure(ExceptionFailure(ex)) -> (TestStatus.Failure, ex.ToString())
        | Failure(Ignored(msg)) -> (TestStatus.Failure, "Ignored:" + Environment.NewLine + msg)
        | Failure(StandardNotMet) -> (TestStatus.Failure, "Gold Standard not met, check the comparison or configure comparison")
    
    do
        engine.TestFound.Add
            (
                fun args ->
                    if knownTests.ContainsKey (args.Name) then ()
                    else
                        let detail = new TestDetailModel()
                        detail.Name <- args.Name
                        detail.Status <- TestStatus.None
                        detail.AssemblyName <- name
                        detail.Parent <- this

                        tests.Add detail

                        knownTests.Add (detail.Name, detail)
                        this.OnPropertyChanged("Tests")
            )

        engine.TestFinished.Add
            (
                fun args ->
                    results.Add (args.TestResult)

                    let status, message = convert (args.TestResult)
                    let detail = knownTests.[args.Name]
                    detail.Status <- status
                    detail.FailDetail <- message

                    this.OnPropertyChanged("Results")
                    this.OnPropertyChanged("Tests")
            )

        engine.TestRunning.Add (fun args -> knownTests.[args.Name].Status <- TestStatus.Running)

        engine.FindTests(assemblyPath)

    member private this.ITestAssemblyModel = this :> ITestAssemblyModel

    member this.ToggleVisibility _ =
        this.ITestAssemblyModel.IsVisible <- not this.ITestAssemblyModel.IsVisible

    interface ITestAssemblyModel with
        member this.IsVisible
            with get () = isVisible
            and set value = 
                if value = isVisible then ()
                else
                    isVisible <- value
                    this.OnPropertyChanged "IsVisible"

        member this.IsRunning
            with get () = isRunning
            and set value = 
                if isRunning = value then ()
                else
                    isRunning <- value
                    this.OnPropertyChanged "IsRunning"

        member this.Name with get () = name

        member this.AssemblyPath with get () = assemblyPath

        member this.Tests with get () = tests

        member this.Results with get () = results

        member this.Run _ = 
            async {
                this.ITestAssemblyModel.IsRunning <- true
                results.Clear()

                for test in tests do
                    test.Status <- TestStatus.None

                let t = new Threading.Tasks.Task<unit>(fun () -> engine.RunTests(assemblyPath))
                t.Start()
                do! Async.AwaitTask(t)

                this.ITestAssemblyModel.IsRunning <- false

            } |> Async.Start

        member this.RunCommand 
            with get () = 
                new DelegateCommand(fun ignored -> this.ITestAssemblyModel.Run(ignored)) :> ICommand

        member this.ToggleVisibilityCommand 
            with get () =
                new DelegateCommand(fun ignored -> this.ToggleVisibility(ignored)) :> ICommand
                
