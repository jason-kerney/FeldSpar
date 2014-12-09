namespace FeldSpar.Api.Engine.ClrInterop.ViewModels

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
