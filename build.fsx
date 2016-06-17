// include Fake lib
#r @"packages\FAKE\tools\FakeLib.dll"
open Fake
open Fake.VersionHelper

RestorePackages ()

// Properties
let net46 = "46"
let net40 = "40"
let netVersion = net46
let netDir = Some(netVersion)
let netVersionFileName = "." + netVersion
let fSharpProjects = "*" + netVersionFileName + ".*.fsproj"
let releaseDir = "bin/Release/"

let getSubFolder = function
    | None -> ""
    | Some(nv) -> nv + "/"

let getFolder root nv =
    let subFolder = getSubFolder nv
    root + subFolder

let buildDir = getFolder "./_build/"
let testDir = getFolder "./_test/"
let deployDir = getFolder "./_deploy/"

let nugetDeployDir = 
    let t = "C:/Nuget.Local/"
    if System.IO.Directory.Exists t then t
    else netDir |> deployDir

let version () =
    (netDir |> buildDir) + "FeldSparFramework" + netVersionFileName + ".dll" |> GetAssemblyVersionString 

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

// Default target
Target "Clean" (fun _ ->
    CleanDirs [None |> buildDir; None |> testDir; None |> deployDir; nugetDeployDir]
)

let buildApp netDir = 
    build "./FeldSparFramework/" releaseDir (buildDir netDir) "AppBuild-Output:" fSharpProjects

let doesNothing = (fun _ -> ())

Target "BuildApp" doesNothing

Target "BuildApp46" (fun _ ->
    Some(net46) |> buildApp
)

Target "BuildApp40" (fun _ ->
    Some(net40) |> buildApp
)

let buildConsole netDir =
    build "./FeldSpar.ContinuousIntegration/" releaseDir (buildDir netDir) "BuildConsole-Output:" fSharpProjects

Target "BuildConsole" doesNothing

Target "BuildConsole40" (fun _ ->
    Some(net40) |>  buildConsole
)

Target "BuildConsole46" (fun _ ->
    Some(net46) |> buildConsole
)

let buildTest netDir =
    let dir = netDir |> testDir
    !! ("./**/" + fSharpProjects)
        |> MSBuildRelease dir "Build"
        |> Log "TestBuild-Output:"

Target "BuildTest" doesNothing
         
Target "BuildTest40" (fun _ ->
    Some(net40) |> buildTest
)
         
Target "BuildTest46" (fun _ ->
    Some(net46) |> buildTest
)

Target "Test" (fun _ ->
    FileSystemHelper.directoryInfo "./FeldSpar.Tests/" |>
        FileSystemHelper.filesInDir |>
        Array.map(fun fi -> fi.FullName) |>
        Array.filter(fun fi -> fi.Contains("approved")) |>
        Copy (testDir netDir)
    let result = Shell.Exec ((buildDir netDir) + "FeldSpar" + netVersionFileName + ".ContinuousIntegration.exe" ,"--v ERRORS --r \".\\RunReport.json\"  --a \".\\FeldSpar" + netVersionFileName + ".Tests.exe\"", ?dir=Some(testDir netDir))
    if result <> 0 then failwith "Failed Tests"
)

Target "Zip" (fun _ ->
    let sourceDir = netDir |> buildDir
    let destDir = netDir |> deployDir
    
    let dirInfo = System.IO.DirectoryInfo(destDir)
    if not dirInfo.Exists then
        dirInfo.Create()

    !! (sourceDir + "/**/*.*")
        |> Zip sourceDir (destDir + "FeldSparFSharp." + (version ()) + ".zip")
)

Target "Default" (fun _ ->
    trace "Hello world from FAKE"
)

Target "Nuget" (fun _ ->
    Shell.Exec ("nuget", @"pack ..\FeldSparFramework\FeldSpar" + netVersionFileName + ".Framework.fsproj -IncludeReferencedProjects -Prop Configuration=Release", netDir |> deployDir) |> ignore
    Shell.Exec ("nuget", @"pack ..\FeldSpar.ContinuousIntegration\FeldSpar" + netVersionFileName + ".ContinuousIntegration.fsproj -IncludeReferencedProjects -Prop Configuration=Release", netDir |> deployDir) |> ignore
)

Target "LocalDeploy" (fun _ ->
    let dir = netDir |> deployDir
    if dir = nugetDeployDir then ()
    else
        FileSystemHelper.directoryInfo dir
            |> FileSystemHelper.filesInDir
            |> Array.filter (fun fi -> fi.Extension = ".nupkg")
            |> Array.map (fun fi -> fi.FullName)
            |> Copy nugetDeployDir

        FileSystemHelper.directoryInfo nugetDeployDir
            |> FileSystemHelper.filesInDir
            |> Array.map (fun fi -> fi.FullName)
            |> Array.iter (printfn "LocalDeploy-Output: %s")
    
    use file = System.IO.File.Create(dir + "push.txt")
    let writer = new System.IO.StreamWriter(file)

    FileSystemHelper.directoryInfo nugetDeployDir
        |> FileSystemHelper.filesInDir
        |> Array.filter (fun fi -> fi.Extension = ".nupkg")
        |> Array.map (fun fi -> fi.FullName)
        |> Array.iter (fun name -> writer.WriteLine(sprintf "nuget push %A" name))

    writer.Close()

    printfn "%A" (System.DateTime.Now)
)

// Dependencies
"Clean"
    ==> "BuildApp40"
    ==> "BuildApp46"
    ==> "BuildApp"

    ==> "BuildConsole40"
    ==> "BuildConsole46"
    ==> "BuildConsole"

    ==> "BuildTest40"
    ==> "BuildTest46"
    ==> "BuildTest"

    ==> "Test"

    ==> "Zip"
    ==> "Default"
    ==> "Nuget"
    ==> "LocalDeploy"

// start build
RunTargetOrDefault "Default"