// include Fake lib
#r @"packages\FAKE\tools\FakeLib.dll"
open Fake

RestorePackages ()

// Properties
let testDirName = "test"
let buildDir = "./build/"
let testDir = "./" + testDirName + "/"
let testPath = ".\\" + testDirName + "\\"
let deployDir = "./deploy/"

// version info
let version = "0.3.1"

// Default target
Target "Clean" (fun _ ->
    CleanDirs [buildDir; testDir; deployDir]
)

Target "BuildApp" (fun _ ->
    !! "./FeldSparFramework/*.fsproj"
     |> MSBuildRelease buildDir "Build"
     |> Log "AppBuild-Output:"
)

Target "BuildConsole" (fun _ ->
    !! "./FeldSpar.Console/*.fsproj"
     |> MSBuildRelease buildDir "Build"
     |> Log "AppBuild-Output:"
)

Target "BuildGui" (fun _ ->
    !! "./GuiRunner/*.csproj"
     |> MSBuildRelease buildDir "Build"
     |> Log "AppBuild-Output:"
)

Target "BuildTest" (fun _ ->
    !! "./**/*.fsproj"
        |> MSBuildDebug testDir "Build"
        |> Log "TestBuild-Output:"
)

//(* Having problems with git conversion of end of line characters
Target "Test" (fun _ ->
    FileSystemHelper.directoryInfo "./FeldSpar.Console/" |>
        FileSystemHelper.filesInDir |>
        Array.map(fun fi -> fi.FullName) |>
        Array.filter(fun fi -> fi.Contains("approved")) |>
        Copy testDir
    Shell.Exec (testPath + "FeldSpar.Console.exe" ,"--a \"FeldSpar.Tests.dll\"", ?dir=Some(testDir)) |> ignore
)//*)

Target "Zip" (fun _ ->
    !! (buildDir + "/**/*.*")
        |> Zip buildDir (deployDir + "FeldSparFSharp." + version + ".zip")
)

Target "Default" (fun _ ->
    trace "Hello world from FAKE"
)

// Dependencies
"Clean"
    ==> "BuildApp"
    ==> "BuildGui"
    ==> "BuildConsole"
    ==> "BuildTest"
    ==> "Test"
    ==> "Zip"
    ==> "Default"


// start build
RunTargetOrDefault "Default"