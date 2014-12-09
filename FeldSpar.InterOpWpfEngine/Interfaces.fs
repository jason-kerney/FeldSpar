namespace FeldSpar.Api.Engine.ClrInterop.ViewModels
open System
open FeldSpar.Framework
open System.ComponentModel
open System.Runtime.CompilerServices
open System.Collections.ObjectModel

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
    /// The ICommand used to run the tests in dubugger
    /// </summary>
    abstract member DebugCommand : System.Windows.Input.ICommand with get
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
    /// This launches debugger and runs the tests
    /// </summary>
    /// <param name="ignored">this parameter is not used, but nessesary for the ICommand</param>
    abstract member Debug : obj -> unit

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
    /// The ICommand used to run all tests
    /// </summary>
    abstract DebugCommand : System.Windows.Input.ICommand with get
    /// <summary>
    /// The ICommant used to add test assemblies
    /// </summary>
    abstract AddCommand : System.Windows.Input.ICommand with get
