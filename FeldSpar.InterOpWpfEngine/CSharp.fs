namespace FeldSpar.Api.Engine.ClrInterop
open System
open FeldSpar.Framework
open FeldSpar.Framework.Engine
open System.ComponentModel
open System.Runtime.CompilerServices
open System.Collections.ObjectModel
open System.Collections.Generic
open System.IO
open System.Windows.Input
open System.Collections.Specialized

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


namespace FeldSpar.Api.Engine.ClrInterop.ViewModels
open System
open FeldSpar.Framework
open FeldSpar.Framework.Engine
open System.ComponentModel
open System.Runtime.CompilerServices
open System.Collections.ObjectModel
open System.Collections.Generic
open System.IO
open System.Windows.Input
open System.Collections.Specialized
open FeldSpar.Api.Engine.ClrInterop

/// <summary>
/// The Status of the current test
/// </summary>
type TestStatus = 
    | None = 0
    | Running = 1
    | Success = 2
    | Failure = 3
    | Ignored = 4

/// <summary>
/// Base class used to fire Property Notified events
/// </summary>
type PropertyNotifyBase () =
    let notify = new Event<_, _>()

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = notify.Publish

    /// <summary>
    /// Used to raise property changed.
    /// </summary>
    /// <param name="propertyName">The name of the property to raise the event for</param>
    abstract member OnPropertyChanged : string -> unit

    /// <summary>
    /// Used to raise property changed.
    /// </summary>
    /// <param name="propertyName">The name of the property to raise the event for</param>
    default this.OnPropertyChanged ([<CallerMemberName>]propertyName:string) =
        notify.Trigger(this, new PropertyChangedEventArgs(propertyName))

    /// <summary>
    /// Used to raise property changed.
    /// </summary>
    member this.OnPropertyChanged () = this.OnPropertyChanged(null)

/// <summary>
/// The Details of a test
/// </summary>
type ITestDetailModel = 
    /// <summary>
    /// The name of the test
    /// </summary>
    abstract member Name : string with get, set
    /// <summary>
    /// The Status of the test
    /// </summary>
    abstract member Status : TestStatus with get, set
    /// <summary>
    /// If the test failed, this is the information about the failure
    /// </summary>
    abstract member FailDetail : string with get, set
    /// <summary>
    /// The name of the assembly that contained the test
    /// </summary>
    abstract member AssemblyName : string with get, set
    /// <summary>
    /// The information about the assembly that contains this test
    /// </summary>
    abstract member Parent : ITestAssemblyModel with get, set

/// <summary>
/// Information about a test assembly
/// </summary>
and ITestAssemblyModel =
    /// <summary>
    /// The evet that is raised if the assembly file is deleted
    /// </summary>
    [<CLIEvent>]abstract member DeletedFile : IEvent<EventHandler, EventArgs>
    /// <summary>
    /// Whether or not this information should be availible for display
    /// </summary>
    abstract member IsVisible : bool with get, set
    /// <summary>
    /// Whether or not tests from this assembly are running
    /// </summary>
    abstract member IsRunning : bool with get, set
    /// <summary>
    /// The file name of this test Assembly
    /// </summary>
    abstract member Name : string with get
    /// <summary>
    /// The path of the file for this assembly
    /// </summary>
    abstract member AssemblyPath : string with get
    /// <summary>
    /// The tests that were found within this assembly
    /// </summary>
    abstract member Tests : ObservableCollection<ITestDetailModel> with get
    /// <summary>
    /// The results from all the executed tests
    /// </summary>
    abstract member Results : ObservableCollection<TestResult> with get
    /// <summary>
    /// The ICommand used to run the tests
    /// </summary>
    abstract member RunCommand : System.Windows.Input.ICommand with get
    /// <summary>
    /// The ICommand that is used to change the visibility of this assembly
    /// </summary>
    abstract member ToggleVisibilityCommand : System.Windows.Input.ICommand with get
    /// <summary>
    /// This runs the tests
    /// </summary>
    /// <param name="ignored">this parameter is not used, but nessesary for the ICommand</param>
    abstract member Run : obj -> unit

/// <summary>
/// This is the main model used to control and display tests
/// </summary>
type ITestsMainModel =
    /// <summary>
    /// Indicates if the test is running.
    /// </summary>
    abstract IsRunning : bool with get, set
    /// <summary>
    /// The currently selected test's fail description
    /// </summary>
    abstract Description : string with get
    /// <summary>
    /// The currently selected test
    /// </summary>
    abstract Selected : ITestDetailModel with get, set
    /// <summary>
    /// The test assemblies that are contained in the system
    /// </summary>
    abstract Assemblies : System.Collections.ObjectModel.ObservableCollection<ITestAssemblyModel> with get, set
    /// <summary>
    /// The results of all tests
    /// </summary>
    abstract Results : TestResult array with get
    /// <summary>
    /// All tests
    /// </summary>
    abstract Tests : ITestDetailModel array with get
    /// <summary>
    /// The ICommand used to run all tests
    /// </summary>
    abstract RunCommand : System.Windows.Input.ICommand with get
    /// <summary>
    /// The ICommant used to add test assemblies
    /// </summary>
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
            [<CLIEvent>]
            member this.DeletedFile = (new Event<EventHandler, EventArgs>()).Publish
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

/// <summary>
/// An implementation of ICommand for use in WPF
/// Thank you: http://wpftutorial.net/DelegateCommand.html
/// </summary>
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

/// <summary>
/// Implementation Class representing the information of a test
/// </summary>
type TestDetailModel () =
    inherit PropertyNotifyBase ()

    let mutable name = ""
    let mutable status = TestStatus.Success
    let mutable failDetail = ""
    let mutable assemblyName = ""
    let mutable parent :ITestAssemblyModel = Defaults.emptyTestAssemblyModel

    /// <summary>
    /// A Shortcut to prevent the need to cast
    /// </summary>
    member this.ITestDetailModel 
        with get () = this :> ITestDetailModel
    
    interface ITestDetailModel with
        member this.Name
            with get () = name
            and set value = 
                if name <> value
                then 
                    name <- value
                    this.OnPropertyChanged("Name")

        member this.Status
            with get () = status
            and set value = 
                if status <> value
                then
                    status <- value
                    this.OnPropertyChanged("Status")

        member this.FailDetail
            with get () = failDetail
            and set value = 
                if failDetail <> value 
                then 
                    failDetail <- value
                    this.OnPropertyChanged("FailDetail")

        member this.AssemblyName
            with get () = assemblyName
            and set value = 
                if assemblyName <> value
                then
                    assemblyName <- value
                    this.OnPropertyChanged("AssemblyName")

        member this.Parent
            with get () = parent
            and set value = 
                if value <> parent
                then
                    parent <- value
                    this.OnPropertyChanged("Parent")

    /// <summary>
    /// The name of the test
    /// </summary>
    member this.Name
        with get () = this.ITestDetailModel.Name
        and set value = this.ITestDetailModel.Name <- value

    /// <summary>
    /// The execution status of the test
    /// </summary>
    member this.Status
        with get () = this.ITestDetailModel.Status
        and set value = this.ITestDetailModel.Status <- value

    /// <summary>
    /// If this test failed, then this is the detail of the failure.
    /// </summary>
    member this.FailDetail
        with get () = this.ITestDetailModel.FailDetail
        and set value = this.ITestDetailModel.FailDetail <- value

    /// <summary>
    /// The file name of the test assembly
    /// </summary>
    member this.AssemblyName
        with get () = this.ITestDetailModel.AssemblyName
        and set value = this.ITestDetailModel.AssemblyName <- value

    /// <summary>
    /// The test assembly information that contains this test
    /// </summary>
    member this.Parent
        with get () = this.ITestDetailModel.Parent
        and set value = this.ITestDetailModel.Parent <- value

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

        member this.Run _ = 
            async {
                this.ITestAssemblyModel.IsRunning <- true
                results.Clear()

                for test in tests do
                    test.Status <- TestStatus.None

                let t = new Threading.Tasks.Task<unit>(fun () -> engine.RunTests(token))
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
    /// The ICommand used to run the tests
    /// </summary>
    member this.RunCommand 
        with get () = this.ITestAssemblyModel.RunCommand

    /// <summary>
    /// The ICommand used to toggle if this assembly should be visible
    /// </summary>
    member this.ToggleVisibilityCommand 
        with get () = this.ITestAssemblyModel.ToggleVisibilityCommand
                
type TestsMainModel () as this =
    inherit PropertyNotifyBase()

    let mutable assemblies = new ObservableCollection<ITestAssemblyModel>()
    let mutable isRunning = false
    let mutable selected : ITestDetailModel = Defaults.emptyTestDetailModel

    do
        let itemsRemovedActions = [ NotifyCollectionChangedAction.Remove; NotifyCollectionChangedAction.Replace ]
        assemblies.CollectionChanged.Add
            (
                fun args -> 
                    for newItem in args.NewItems do
                        let newItem = newItem :?> INotifyPropertyChanged
                        newItem.PropertyChanged.AddHandler(this.itemOnPropertyChangedHandler)

                    if args.NewItems.Count > 0 then this.OnPropertyChanged("Tests")

                    if itemsRemovedActions |> List.filter (fun action -> args.Action = action) |> List.length > 0
                    then 
                        for oldItem in args.OldItems do
                            let oldItem = oldItem :?> INotifyPropertyChanged
                            oldItem.PropertyChanged.RemoveHandler(this.itemOnPropertyChangedHandler)

                        if args.OldItems.Count > 0 then this.OnPropertyChanged("Tests")
            )

    member private this.itemOnPropertyChangedHandler =
        new PropertyChangedEventHandler(fun _ args -> this.itemOnPropertyChanged args) 
            
    member this.itemOnPropertyChanged (propertyChangedEventArgs:PropertyChangedEventArgs) = 
        if propertyChangedEventArgs.PropertyName = "Results"
        then this.OnPropertyChanged("Results")

        if propertyChangedEventArgs.PropertyName = "Tests"
        then this.OnPropertyChanged("Tests")

    member this.Add _ = 
        let fileOpen = new Microsoft.Win32.OpenFileDialog ();
        fileOpen.Filter <- "test suites|*.dll;*.exe"
        fileOpen.Multiselect <- false

        let result = fileOpen.ShowDialog()
        if result.GetValueOrDefault() = true
        then
            if assemblies |> Seq.filter(fun a -> a.Name = fileOpen.FileName) |> Seq.length = 0
            then
                let testAssemblyModel = new TestAssemblyModel(fileOpen.FileName) :> ITestAssemblyModel
                assemblies.Add(testAssemblyModel)

                let dict = new Dictionary<string, EventHandler>()


                let onDelete = 
                    new EventHandler (
                        fun sender args ->
                            let sender = sender :?> ITestAssemblyModel
                            let self = this :> ITestsMainModel
                            (self.Assemblies).Remove(sender) |> ignore
                            sender.DeletedFile.RemoveHandler(dict.["onDelete"])
                    )

                dict.Add("onDelete", onDelete)

                testAssemblyModel.DeletedFile.AddHandler onDelete

    member this.Run _ = 
        let self = this :> ITestsMainModel
        self.IsRunning <- true
        for testAssemblyModel in self.Assemblies do
            let model = testAssemblyModel
            model.Run null
            ()

        self.IsRunning <- false

    member private this.GetTestItems (selector: ITestAssemblyModel -> 'b seq) =
        query { for assembly in assemblies do
                    select (selector assembly)
                } |> Seq.collect (fun a -> a)

    interface ITestsMainModel with
        member this.Assemblies
            with get () = assemblies
            and set value = 
                if value <> assemblies
                then
                    assemblies <- value
                    this.OnPropertyChanged("Assemblies")

        member this.IsRunning 
            with get () = isRunning
            and set value = 
                if value <> isRunning
                then
                    isRunning <- value
                    this.OnPropertyChanged("IsRunning")

        member this.Description
            with get () = 
                selected.Name + "\n\n" + selected.FailDetail

        member this.Selected
            with get () = selected
            and set value =
                if value <> selected
                then
                    selected <- value
                    this.OnPropertyChanged("Selected")
                    this.OnPropertyChanged("Description")

        member this.Results
            with get () = this.GetTestItems (fun (a:ITestAssemblyModel) -> a.Results :> TestResult seq) |> Seq.toArray

        member this.Tests
            with get () = this.GetTestItems (fun (a:ITestAssemblyModel) -> a.Tests :> ITestDetailModel seq) |> Seq.toArray

        member this.RunCommand
            with get () = new DelegateCommand((fun _ -> this.Run(null)), fun _ -> not this.ITestsMainModel.IsRunning) :> ICommand

        member this.AddCommand
            with get () = new DelegateCommand(fun _ -> this.Add(null)) :> ICommand

    member private this.ITestsMainModel = this :> ITestsMainModel

    member this.Assemblies
        with get () = this.ITestsMainModel.Assemblies
        and set value = this.ITestsMainModel.Assemblies <- value

    member this.IsRunning
        with get () = this.ITestsMainModel.IsRunning
        and set value = this.ITestsMainModel.IsRunning <- value

    member this.Description
        with get () = this.ITestsMainModel.Description

    member this.Selected
        with get () = this.ITestsMainModel.Selected
        and set value = this.ITestsMainModel.Selected <- value

    member this.Results
        with get () = this.ITestsMainModel.Results

    member this.Tests
        with get () = this.ITestsMainModel.Tests

    member this.RunCommand
        with get () = this.ITestsMainModel.RunCommand

    member this.AddCommand
        with get () = this.ITestsMainModel.AddCommand