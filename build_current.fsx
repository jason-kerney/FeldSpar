#r @"packages\FAKE\tools\FakeLib.dll"
#r "paket:
nuget Fake.Core.Target //"
// include Fake modules, see Fake modules section

open Fake.Core

// *** Define Targets ***
Target.create "Clean" (fun _ ->
  Trace.log " --- Cleaning stuff --- "
)

Target.create "Build" (fun _ ->
  Trace.log " --- Building the app --- "
)

Target.create "Deploy" (fun _ ->
  Trace.log " --- Deploying app --- "
)

open Fake.Core.TargetOperators

// *** Define Dependencies ***
"Clean"
  ==> "Build"
  ==> "Deploy"

// *** Start Build ***
Target.runOrDefault "Deploy"

//// include Fake lib
//#r @"packages\FAKE\tools\FakeLib.dll"
//open Fake
//open Fake.VersionHelper
//open Fake.DotNet
//open Fake.IO
//open Fake.IO.Globbing.Operators
//open Fake.DotNet
//open Fake.Core

//RestorePackages ()

//// Properties
//let fSharpProjects = "*.fsproj"
//let releaseDir = "bin/Release/"



//let buildDir = "./_build/"
//let testDir = "./_test/"
//let deployDir = "./_deploy/"

//let nugetDeployDir = 
//    let t = "C:/Nuget.Local/"
//    if System.IO.Directory.Exists t then t
//    else deployDir

//let version () =
//    buildDir + "FeldSparFramework.dll" |> GetAssemblyVersionString 

////let build appDir tmpDir targetDir label projecType =
////    let tmpDir = (appDir + tmpDir)

////    tmpDir |> directoryInfo |> (fun d -> d.ToString()) |> CleanDir

////    //let o = !! (appDir + projecType)
////    //        |> build  tmpDir "Build"
////    //        |> Log label

////    FileSystemHelper.directoryInfo tmpDir
////        |> FileSystemHelper.filesInDir
////        |> Array.map (fun fi -> fi.FullName)
////        |> Copy targetDir
////    ()

//// Default target
//Target "Clean" (fun _ ->
//    CleanDirs [buildDir; testDir; deployDir; nugetDeployDir]
//)

//Target "BuildApp" (fun _ ->
//    build "./FeldSparFramework/" releaseDir buildDir "AppBuild-Output:" fSharpProjects
//)

//Target "BuildTest" (fun _ ->
//    !! "./**/*.fsproj"
//        |> MSBuild.runRelease testDir "Build"
//        |> Log "TestBuild-Output:"
//)

//Target "Test" (fun _ ->
//    FileSystemHelper.directoryInfo "./FeldSpar.Tests/" |>
//        FileSystemHelper.filesInDir |>
//        Array.map(fun fi -> fi.FullName) |>
//        Array.filter(fun fi -> fi.Contains("approved")) |>
//        Copy testDir
//    let result = Shell.Exec (buildDir + "FeldSpar.Console.exe" ,"--v RESULTS --r \".\\RunReport.json\"  --a \".\\FeldSpar.Tests.dll\"", ?dir=Some(testDir))
//    if result <> 0 then failwith "Failed Tests"
//)

//Target "Zip" (fun _ ->
//    !! (buildDir + "/**/*.*")
//        |> Zip buildDir (deployDir + "FeldSparFSharp." + (version ()) + ".zip")
//)

//Target "Default" (fun _ ->
//    trace "Hello world from FAKE"
//)

//Target "Nuget" (fun _ ->
//    Shell.Exec ("nuget", @"pack ..\FeldSparFramework\FeldSpar.Framework.fsproj -IncludeReferencedProjects -Prop Configuration=Release", deployDir) |> ignore
//    Shell.Exec ("nuget", @"pack ..\FSharpWpf\FSharpWpf.fsproj -IncludeReferencedProjects -Prop Configuration=Release", deployDir) |> ignore
//)

//Target "LocalDeploy" (fun _ ->
//    if deployDir = nugetDeployDir then ()
//    else
//        FileSystemHelper.directoryInfo deployDir
//            |> FileSystemHelper.filesInDir
//            |> Array.filter (fun fi -> fi.Extension = ".nupkg")
//            |> Array.map (fun fi -> fi.FullName)
//            |> Copy nugetDeployDir

//        FileSystemHelper.directoryInfo nugetDeployDir
//            |> FileSystemHelper.filesInDir
//            |> Array.map (fun fi -> fi.FullName)
//            |> Array.iter (printfn "LocalDeploy-Output: %s")
    

//    use file = System.IO.File.Create(deployDir + "push.txt")
//    let writer = new System.IO.StreamWriter(file)

//    FileSystemHelper.directoryInfo nugetDeployDir
//        |> FileSystemHelper.filesInDir
//        |> Array.filter (fun fi -> fi.Extension = ".nupkg")
//        |> Array.map (fun fi -> fi.FullName)
//        |> Array.iter (fun name -> writer.WriteLine(sprintf "nuget push %A" name))

//    writer.Close()

//    printfn "%A" (System.DateTime.Now)
//)

//// Dependencies
//"Clean"
//    ==> "BuildApp"
//    ==> "BuildTest"
//    ==> "Test"
//    ==> "Zip"
//    ==> "Default"
//    ==> "Nuget"
//    ==> "LocalDeploy"

//// start build
//RunTargetOrDefault "Default"

