namespace FeldSpar.Api.Engine.ClrInterop.ViewModels
open System
open FeldSpar.Framework

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
            member this.DebugCommand with get () = emptyCommand
            member this.ToggleVisibilityCommand with get () = emptyCommand
            member this.Run param = ()
            member this.Debug param = ()
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
