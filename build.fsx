// include Fake lib
#r @"packages\FAKE\tools\FakeLib.dll"
open Fake
open Fake.VersionHelper

RestorePackages ()

// Properties
let fSharpProjects = "*.fsproj"
let releaseDir = "bin/Release/"
let buildDir = "./build/"
let testDir = "./test/"
let deployDir = "./deploy/"

let nugetDeployDir = 
    let t = "C:/Nuget.Local/"
    if System.IO.Directory.Exists t then t
    else deployDir

// version info
//let version = "0.3.1"

let version () =
    buildDir + "FeldSparFramework.dll" |> GetAssemblyVersionString 

let build appDir tmpDir targetDir label projecType =
    let tmpDir = (appDir + tmpDir)
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
    CleanDirs [buildDir; testDir; deployDir; nugetDeployDir]
)

Target "BuildApp" (fun _ ->
    build "./FeldSparFramework/" releaseDir buildDir "AppBuild-Output:" fSharpProjects
)

Target "BuildConsole" (fun _ ->
    build "./FeldSpar.Console/" releaseDir buildDir "BuildConsole-Output:" fSharpProjects
)

Target "BuildGui" (fun _ ->
    build "./GuiRunner/" releaseDir buildDir "BuildGui-Output:" "*.csproj"
)

Target "BuildTest" (fun _ ->
    !! "./**/*.fsproj"
        |> MSBuildRelease testDir "Build"
        |> Log "TestBuild-Output:"
)

Target "Test" (fun _ ->
    FileSystemHelper.directoryInfo "./FeldSpar.Console/" |>
        FileSystemHelper.filesInDir |>
        Array.map(fun fi -> fi.FullName) |>
        Array.filter(fun fi -> fi.Contains("approved")) |>
        Copy testDir
    let result = Shell.Exec (testDir + "FeldSpar.Console.exe" ,"--a \"FeldSpar.Tests.dll\"", ?dir=Some(testDir))
    if result <> 0 then failwith "Failed Tests"
)

Target "Zip" (fun _ ->
    !! (buildDir + "/**/*.*")
        |> Zip buildDir (deployDir + "FeldSparFSharp." + (version ()) + ".zip")
)

Target "Default" (fun _ ->
    trace "Hello world from FAKE"
)

Target "Nuget" (fun _ ->
    Shell.Exec ("nuget", @"pack C:\Users\Jason\Documents\GitHub\FeldSpar\FeldSparFramework\FeldSpar.Framework.fsproj -IncludeReferencedProjects -Prop Configuration=Release", deployDir) |> ignore
    Shell.Exec ("nuget", @"pack C:\Users\Jason\Documents\GitHub\FeldSpar\FeldSpar.Console\FeldSpar.Console.fsproj -IncludeReferencedProjects -Prop Configuration=Release", deployDir) |> ignore
    Shell.Exec ("nuget", @"pack C:\Users\Jason\Documents\GitHub\FeldSpar\GuiRunner\FeldSparGui.csproj -IncludeReferencedProjects -Prop Configuration=Release", deployDir) |> ignore
)

Target "LocalDeploy" (fun _ ->
    if deployDir = nugetDeployDir then ()
    else
        FileSystemHelper.directoryInfo deployDir
            |> FileSystemHelper.filesInDir
            |> Array.filter (fun fi -> fi.Extension = ".nupkg")
            |> Array.map (fun fi -> fi.FullName)
            |> Copy nugetDeployDir

        FileSystemHelper.directoryInfo nugetDeployDir
            |> FileSystemHelper.filesInDir
            |> Array.map (fun fi -> fi.FullName)
            |> Array.iter (printfn "LocalDeploy-Output: %s")
)

// Dependencies
"Clean"
    ==> "BuildApp"
    ==> "BuildConsole"
    ==> "BuildGui"
    ==> "BuildTest"
    ==> "Test"
    ==> "Zip"
    ==> "Default"
    ==> "Nuget"
    ==> "LocalDeploy"

// start build
RunTargetOrDefault "Default"