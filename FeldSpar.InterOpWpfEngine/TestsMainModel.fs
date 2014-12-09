namespace FeldSpar.Api.Engine.ClrInterop.ViewModels
open System
open FeldSpar.Framework
open System.ComponentModel
open System.Collections.ObjectModel
open System.Collections.Generic
open System.Windows.Input
open System.Collections.Specialized

type TestsMainModel () as this =
    inherit PropertyNotifyBase()

    let mutable assemblies = new ObservableCollection<ITestAssemblyModel>()
    let mutable isRunning = false
    let mutable selected : ITestDetailModel = Defaults.emptyTestDetailModel

    let runIt runner (this:TestsMainModel) = 
        let self = this :> ITestsMainModel
        self.IsRunning <- true
        for testAssemblyModel in self.Assemblies do
            let model = testAssemblyModel
            model |> runner
            ()

        self.IsRunning <- false

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
        this |> runIt (fun model -> model.Run null)

    member this.Debug _ = 
        this |> runIt (fun model -> model.Debug null)

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

        member this.DebugCommand
            with get () = new DelegateCommand((fun _ -> this.Debug(null)), fun _ -> not this.ITestsMainModel.IsRunning) :> ICommand

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
        
    member this.DebugCommand
        with get () = this.ITestsMainModel.DebugCommand

    member this.AddCommand
        with get () = this.ITestsMainModel.AddCommand