// include Fake lib
#r @"packages\FAKE\tools\FakeLib.dll"
open Fake
open Fake.VersionHelper
open System.IO

RestorePackages ()

// Properties
let net46  = "46"
let net451 = "451"
let net452 = "452"
let net45  = "45"
let net40  = "40"

let netVersionFileName = function
    | None -> ""
    | Some(v) -> "." + v

let fSharpProjects = function
    | None -> ""
    | Some(netVersionFileName) -> "*" + netVersionFileName + ".*.fsproj"


let getSubFolder st next =
    match st with
    | None -> ""
    | Some(nv) -> nv + "/" + next 

let getFolder root next nv =
    let subFolder = getSubFolder nv next
    root + subFolder

let releaseDir netDir = 
    let baseDir = "bin/Release"
    match netDir with
    | None -> baseDir + "/"
    | Some(ver) -> baseDir +  ver + "/"

let getTargetFolder typeDir project netDir =
    let baseDir = sprintf "./%s/" typeDir
    let projectDir = (getFolder baseDir "lib/" project)
    
    getFolder projectDir "" netDir

let buildDir project = 
    getTargetFolder "_build" project

let testDir = getFolder "./_test/" ""

let deployDir = getFolder "./_deploy/" ""

let nugetDeployDir netDir = 
    let t = "C:/Nuget.Local/"
    if System.IO.Directory.Exists t then t
    else netDir |> deployDir

let feldSpar = "FeldSparFramework"
let feldSparDir = Some(feldSpar)
let continuousIntegration = "ContinuousIntegration"
let ciDir = Some(continuousIntegration)

let version netDir =
    (buildDir feldSparDir netDir) + feldSpar + (netVersionFileName netDir) + ".dll" |> GetAssemblyVersionString 

let build appDir tmpDir targetDir label projecType =
    let tmpDir = (appDir + tmpDir)

    tmpDir |> directoryInfo |> (fun d -> d.ToString()) |> CleanDir

    let o = !! (appDir + projecType)
            |> MSBuildRelease tmpDir "Build"
            |> Log label

    FileSystemHelper.directoryInfo tmpDir
        |> FileSystemHelper.filesInDir
        |> Array.map (fun fi -> fi.FullName)
        |> Copy targetDir
    ()

let asPath baseName dirName =
    baseName + "/" + dirName + "/"

let buildApp netDir = 
    let target = feldSpar |> asPath "."
    let destination = (buildDir feldSparDir netDir)

    let projects = fSharpProjects netDir
    let release = releaseDir netDir

    printfn "Building '%s'" projects
    build target release destination "AppBuild-Output:" projects


let buildConsole netDir =
    let projects = fSharpProjects netDir
    build ("./FeldSpar." + continuousIntegration + "/") (releaseDir netDir) (buildDir ciDir netDir) "BuildConsole-Output:" projects

let buildTest netDir =
    let dir = netDir |> testDir
    let projects = fSharpProjects netDir
    !!("./**/" + projects)
        |> MSBuildRelease dir "Build"
        |> Log "TestBuild-Output:"

let tee f (g) (a:'a) =
    f a |> g

let test netDir = 
    FileSystemHelper.directoryInfo "./FeldSpar.Tests/"
        |> FileSystemHelper.filesInDir
        |> Array.map(fun fi -> fi.FullName)
        |> Array.filter(fun fi -> fi.Contains("approved"))
        |> Copy (testDir netDir)

    let netVersion = netVersionFileName netDir
    let ciPath = (buildDir ciDir netDir)

    //printfn "%A" (ciPath + "FeldSpar" + netVersion + "." + continuousIntegration + ".exe" ,"--v ERRORS --r \".\\RunReport.json\"  --a \".\\FeldSpar" + netVersion + ".Tests.exe\"", Some(testDir netDir))
    let result = Shell.Exec (ciPath + "FeldSpar" + netVersion + "." + continuousIntegration + ".exe" ,"--v ERRORS --r \".\\RunReport.json\"  --a \".\\FeldSpar" + netVersion + ".Tests.exe\"", ?dir=Some(testDir netDir))
    if result <> 0 then failwith "Failed Tests"
    ()

let clean () = 
    [None |> buildDir None; None |> testDir; None |> deployDir; nugetDeployDir None]
    |> CleanDirs

let zip netDir =
    let sourceDir = None |> buildDir None
    let destDir = None |> deployDir
    
    let dirInfo = System.IO.DirectoryInfo(destDir)
    if not dirInfo.Exists then
        dirInfo.Create()

    !! (sourceDir + "/**/*.*")
        |> Zip sourceDir (destDir + "FeldSparFSharp." + (version netDir) + ".zip")

let copyDirectory target (source:System.IO.DirectoryInfo) =
     let targetPath = Path.Combine(target, source.Name)
     Directory.CreateDirectory targetPath |> ignore

     source.GetFiles ()
     |> Array.map (fun f -> f, Path.Combine(targetPath, f.Name))
     |> Array.map(fun (f, t) -> f.CopyTo(t))

let nugetMainSetup path =
    FileSystemHelper.directoryInfo ("./" + feldSpar + "/")
        |> FileSystemHelper.subDirectories
        |> Array.filter(fun di -> di.Name.Contains("content") || di.Name.Contains("tools"))
        |> Array.map(fun di -> copyDirectory path di )
        |> ignore

    FileSystemHelper.directoryInfo ("./")
        |> FileSystemHelper.filesInDir
        |> Array.map(fun fi -> fi.FullName)
        |> Array.filter(fun name -> name.Contains("FeldSparFramework.nuspec"))
        |> Copy path
        |> ignore

let nugetCiSetup path =
    FileSystemHelper.directoryInfo ("./")
        |> FileSystemHelper.filesInDir
        |> Array.map(fun fi -> fi.FullName)
        |> Array.filter(fun name -> name.Contains("ContinuousIntegration.nuspec"))
        |> Copy path
        |> ignore
        
let getParent path =
    [path]
        |> List.map DirectoryInfo
        |> List.map (fun di -> di.Parent.FullName)
        |> List.head 

//Targets

Target "Nuke" (fun _ ->
    Shell.Exec ("git", "clean -xfd")  |> ignore
)

Target "Clean" (fun _ ->
    clean ()
)

Target "BuildApp46" (fun _ ->
    Some(net46) |> buildApp
)

Target "BuildApp45" (fun _ ->
    Some(net45) |> buildApp
)

Target "BuildApp451" (fun _ ->
    Some(net451) |> buildApp
)

Target "BuildApp452" (fun _ ->
    Some(net452) |> buildApp
)

Target "BuildApp40" (fun _ ->
    Some(net40) |> buildApp
)

Target "BuildConsole40" (fun _ ->
    Some(net40) |>  buildConsole
)

Target "BuildConsole45" (fun _ ->
    Some(net45) |> buildConsole
)

Target "BuildConsole451" (fun _ ->
    Some(net451) |> buildConsole
)

Target "BuildConsole452" (fun _ ->
    Some(net452) |> buildConsole
)

Target "BuildConsole46" (fun _ ->
    Some(net46) |> buildConsole
)

Target "BuildTest40" (fun _ ->
    Some(net40) |> buildTest
)

Target "BuildTest45" (fun _ ->
    Some(net45) |> buildTest
)

Target "BuildTest451" (fun _ ->
    Some(net451) |> buildTest
)

Target "BuildTest452" (fun _ ->
    Some(net452) |> buildTest
)
         
Target "BuildTest46" (fun _ ->
    Some(net46) |> buildTest
)

Target "Test40" (fun _ ->
    Some(net40) |> test
)

Target "Test45" (fun _ ->
    Some(net45) |> test
)

Target "Test451" (fun _ ->
    Some(net451) |> test
)

Target "Test46" (fun _ ->
    Some(net46) |> test
)

Target "Zip" (fun _ ->
    zip (Some(net40))
    zip (Some(net46))
)

Target "Default" DoNothing

Target "Nuget" (fun _ ->
    let corePath = 
        buildDir feldSparDir None
            |> getParent
    let ciPath = 
        buildDir ciDir None
            |> getParent

    let v = version (Some(net40))

    corePath |> nugetMainSetup
    ciPath |> nugetCiSetup

    //Shell.Exec ("nuget", @"pack ..\FeldSparFramework\FeldSpar" + (netVersionFileName netDir) + ".Framework.fsproj -IncludeReferencedProjects -Prop Configuration=Release", netDir |> deployDir) |> ignore
    //Shell.Exec ("nuget", @"pack ..\FeldSpar.ContinuousIntegration\FeldSpar" + (netVersionFileName netDir) + ".ContinuousIntegration.fsproj -IncludeReferencedProjects -Prop Configuration=Release", netDir |> deployDir) |> ignore

    let feldSparNuget = Path.Combine(corePath, "FeldSparFramework.nuspec")
    let ciSparNuget = Path.Combine(ciPath, "ContinuousIntegration.nuspec")
    let deploy = (nugetDeployDir None)

    [deploy; feldSparNuget; ciSparNuget] |> List.iter (printfn "%A")

    Shell.Exec("nuget", "pack " + feldSparNuget + " -Version " + v, deploy) |> ignore
    Shell.Exec("nuget", "pack " + ciSparNuget + " -Version " + v, deploy) |> ignore

    use file = System.IO.File.Create(Path.Combine(deploy, "push.txt"))
    let writer = new System.IO.StreamWriter(file)

    FileSystemHelper.directoryInfo deploy
        |> FileSystemHelper.filesInDir
        |> Array.filter (fun fi -> fi.Extension = ".nupkg")
        |> Array.map (fun fi -> fi.FullName)
        |> Array.iter (fun name -> writer.WriteLine(sprintf "nuget push %A" name))

    writer.Close()

    printfn "%A" (System.DateTime.Now)
    ()
)

Target "Build40" (fun _ ->
    run "40"
)

Target "Build45" (fun _ ->
    run "45"
)

Target "Build451" (fun _ ->
    run "451"
)

Target "Build452" (fun _ ->
    run "452"
)

Target "Build46" (fun _ ->
    run "46"
)

Target "Build" DoNothing
Target "40" DoNothing
Target "45" DoNothing
Target "451" DoNothing
Target "452" DoNothing
Target "46" DoNothing

// Dependencies
"Clean"
    ==> "Build40"
    ==> "Build45"
    ==> "Build451"
    ==> "Build452"
    ==> "Build46"
    ==> "Build"

    ==> "Zip"
    ==> "Default"

    ==> "Nuget"

"BuildApp40"
    ==> "BuildConsole40"
    ==> "BuildTest40"
    ==> "Test40"
    ==> "40"

"BuildApp45"
    ==> "BuildConsole45"
    ==> "BuildTest45"
    ==> "Test45"
    ==> "45"

"BuildApp451"
    ==> "BuildConsole451"
    ==> "BuildTest451"
    ==> "Test451"
    ==> "451"

"BuildApp452"
    ==> "BuildConsole452"
    ==> "BuildTest452"
    ==> "452"

"BuildApp46"
    ==> "BuildConsole46"
    ==> "BuildTest46"
    ==> "Test46"
    ==> "46"

// start build
RunTargetOrDefault "Default"