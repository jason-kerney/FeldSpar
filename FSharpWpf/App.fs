module MainApp
open System
open System.Windows
open System.Windows.Controls
open FeldSpar.Api.Engine.ClrInterop.ViewModels
open System.IO
open System.Reflection

type Launcher () = 
    member this.Launch () =
        let mainWindowViewModel = 
            Application.LoadComponent
                (
                    new System.Uri("/FeldSparGui;component/TestAssembliesWindow.xaml", UriKind.Relative)
                ) :?> Window
        
        mainWindowViewModel.DataContext <- (new TestsMainModel () :> ITestsMainModel)

        mainWindowViewModel.ShowDialog() |>ignore


// Application Entry point
[<STAThread>]
[<EntryPoint>]
let main(_) = 
        let path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        let applicationName = "FeldSparGui"

        let appDomain = AppDomain.CreateDomain(friendlyName=applicationName,securityInfo=null,appBasePath=path, appRelativeSearchPath=path, shadowCopyFiles=true)

        let launcherType = typeof<Launcher>
        let sandBoxAssemblyName = launcherType.Assembly.FullName
        let sandBoxTypeName = launcherType.FullName

        let sandbox = appDomain.CreateInstanceAndUnwrap(sandBoxAssemblyName, sandBoxTypeName) :?> Launcher

        sandbox.Launch()

        AppDomain.Unload(appDomain)

        0
    