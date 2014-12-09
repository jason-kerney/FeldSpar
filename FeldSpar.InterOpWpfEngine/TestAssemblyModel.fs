namespace FeldSpar.Api.Engine.ClrInterop.ViewModels
open System
open FeldSpar.Framework
open System.Collections.ObjectModel
open System.Collections.Generic
open System.IO
open System.Windows.Input
open FeldSpar.Api.Engine.ClrInterop

/// <summary>
/// The information about a test assembly
/// </summary>
type TestAssemblyModel (path) as this =
    inherit PropertyNotifyBase ()

    let deletedFile = new Event<_, _>()

    let mutable token = path |> getToken
    let engine = new Engine()
    let tests = new ObservableCollection<ITestDetailModel>()
    let results = new ObservableCollection<TestResult>()
    let knownTests = new Dictionary<string, ITestDetailModel>()
    let watcher = new FileSystemWatcher(Path.GetDirectoryName(token.AssemblyPath), "*.*")

    let mutable isRunning = true
    let mutable isVisible = true

    let watchHandler (args:FileSystemEventArgs) = 
        results.Clear()
        tests.Clear()
        knownTests.Clear()

        match args.ChangeType with
        | WatcherChangeTypes.Deleted -> ()
        | WatcherChangeTypes.Renamed -> 
            token <- args.FullPath |> getToken

            engine.FindTests(token)
        | _ ->
            engine.FindTests(token)

    let convert (result:TestResult) = 
        match result with
        | Success -> (TestStatus.Success, String.Empty)
        | Failure(GeneralFailure(msg)) -> (TestStatus.Failure, "General Failure" + "\n" + msg)
        | Failure(ExpectationFailure(msg)) -> (TestStatus.Failure, "Expectation Not Met" + "\n" + msg)
        | Failure(ExceptionFailure(ex)) -> (TestStatus.Failure, ex.ToString())
        | Failure(Ignored(msg)) -> (TestStatus.Ignored, "Ignored:" + "\n" + msg)
        | Failure(StandardNotMet(path)) -> (TestStatus.Failure, sprintf "Gold Standard not met, check the comparison or configure comparison at %A" path)

    let runWith runToken (this:TestAssemblyModel) =
        async {
            this.IsRunning <- true
            results.Clear()

            for test in tests do
                test.Status <- TestStatus.None

            let t = new Threading.Tasks.Task<unit>(fun () -> engine.RunTests(runToken))
            t.Start()
            do! Async.AwaitTask(t)

            this.IsRunning <- false

        } |> Async.Start
    
    do
        engine.TestFound.Add
            (
                fun args ->
                    if knownTests.ContainsKey (args.Name) then ()
                    else
                        let detail = new TestDetailModel()
                        detail.Name <- args.Name
                        detail.Status <- TestStatus.None
                        detail.AssemblyName <- token.AssemblyName
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

        watcher.Changed.Add watchHandler
        watcher.Deleted.Add watchHandler
        watcher.Renamed.Add watchHandler
        watcher.Created.Add watchHandler

        watcher.EnableRaisingEvents <- true

        engine.FindTests(token)

    /// <summary>
    /// A shortcut method to prevent the need for casting
    /// </summary>
    member this.ITestAssemblyModel = this :> ITestAssemblyModel

    /// <summary>
    /// A method for changing whether or not this assembly is supposed to be visible
    /// </summary>
    /// <param name="ignored">This parmater is ignored but needed to uphold contract for ICommand</param>
    member this.ToggleVisibility _ =
        this.ITestAssemblyModel.IsVisible <- not this.ITestAssemblyModel.IsVisible

    interface ITestAssemblyModel with
        [<CLIEvent>]
        member this.DeletedFile = deletedFile.Publish

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

        member this.Name with get () = token.AssemblyName

        member this.AssemblyPath with get () = token.AssemblyPath

        member this.Tests with get () = tests

        member this.Results with get () = results

        member this.Run _ = this |> runWith token

        member this.Debug _ = this |> runWith (token |> withDebug)

        member this.RunCommand 
            with get () = 
                new DelegateCommand(fun ignored -> this.ITestAssemblyModel.Run(ignored)) :> ICommand

        member this.DebugCommand
            with get () =
                new DelegateCommand(fun ignored -> this.ITestAssemblyModel.Debug(ignored)) :> ICommand

        member this.ToggleVisibilityCommand 
            with get () =
                new DelegateCommand(fun ignored -> this.ToggleVisibility(ignored)) :> ICommand

    /// <summary>
    /// Whether or not this assembly should be visible
    /// </summary>
    member this.IsVisible
        with get () = this.ITestAssemblyModel.IsVisible
        and set value = this.ITestAssemblyModel.IsVisible <- value

    /// <summary>
    /// Whether or not any tests in this assembly are running
    /// </summary>
    member this.IsRunning
        with get () = this.ITestAssemblyModel.IsRunning
        and set value = this.ITestAssemblyModel.IsRunning <- value

    /// <summary>
    /// The file name of this assembly
    /// </summary>
    member this.Name with get () = this.ITestAssemblyModel.Name

    /// <summary>
    /// The path of this assembly
    /// </summary>
    member this.AssemblyPath with get () = this.ITestAssemblyModel.AssemblyPath

    /// <summary>
    /// The tests found within this assembly
    /// </summary>
    member this.Tests with get () = this.ITestAssemblyModel.Tests

    /// <summary>
    /// The results of all tests
    /// </summary>
    member this.Results with get () = this.ITestAssemblyModel.Results

    /// <summary>
    /// An asyncronous method to run tests.
    /// </summary>
    /// <param name="ignored">this parameter is not used but required to fullfil the ICommand contract</param>
    member this.Run _ = 
        async {
            this.ITestAssemblyModel.Run null
        } |> Async.Start

    /// <summary>
    /// An asyncronous method to run tests.
    /// </summary>
    /// <param name="ignored">this parameter is not used but required to fullfil the ICommand contract</param>
    member this.Debug _ = 
        async {
            this.ITestAssemblyModel.Debug null
        } |> Async.Start

    /// <summary>
    /// The ICommand used to run the tests
    /// </summary>
    member this.RunCommand 
        with get () = this.ITestAssemblyModel.RunCommand

    /// <summary>
    /// The ICommand used to run the tests
    /// </summary>
    member this.DebugCommand 
        with get () = this.ITestAssemblyModel.DebugCommand

    /// <summary>
    /// The ICommand used to toggle if this assembly should be visible
    /// </summary>
    member this.ToggleVisibilityCommand 
        with get () = this.ITestAssemblyModel.ToggleVisibilityCommand