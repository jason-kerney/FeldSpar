namespace FeldSpar.ClrInterop
open System
open FeldSpar.Framework
open FeldSpar.Framework.Engine
open System.ComponentModel
open System.Runtime.CompilerServices

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
    abstract member Tests : System.Collections.ObjectModel.ObservableCollection<ITestDetailModel> with get
    abstract member Results : System.Collections.ObjectModel.ObservableCollection<TestResult> with get
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

// Thank you:
// http://wpftutorial.net/DelegateCommand.html
type DelegateCommand (execute:Action<obj>, canExecute:Predicate<obj>) =
    let notify = new Event<_, _>()


    interface System.Windows.Input.ICommand with
        [<CLIEvent>]
        member this.CanExecuteChanged = notify.Publish

        member this.CanExecute param =
            if canExecute = null then true
            else canExecute.Invoke param

        member this.Execute param = execute.Invoke param

    new (execute:Action<obj>) = DelegateCommand(execute, fun _ -> true)


    member this.RaiseCanExecuteChanged () = notify.Trigger(this, EventArgs.Empty)