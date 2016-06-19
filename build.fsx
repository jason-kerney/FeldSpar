// include Fake lib
#r @"packages\FAKE\tools\FakeLib.dll"
open Fake
open Fake.VersionHelper

RestorePackages ()

// Properties
let net46 = "46"
let net40 = "40"

let netVersionFileName = function
    | None -> ""
    | Some(v) -> "." + v

let fSharpProjects = function
    | None -> ""
    | Some(netVersionFileName) -> "*" + netVersionFileName + ".*.fsproj"


let getSubFolder = function
    | None -> ""
    | Some(nv) -> nv + "/"

let getFolder root nv =
    let subFolder = getSubFolder nv
    root + subFolder

let releaseDir netDir = 
    let baseDir = "bin/Release"
    match netDir with
    | None -> baseDir + "/"
    | Some(ver) -> baseDir +  ver + "/"

let buildDir = getFolder "./_build/"
let testDir = getFolder "./_test/"
let deployDir = getFolder "./_deploy/"

let nugetDeployDir netDir = 
    let t = "C:/Nuget.Local/"
    if System.IO.Directory.Exists t then t
    else netDir |> deployDir

let feldSpar = "FeldSparFramework"
let continuousIntegration = "ContinuousIntegration"

let version netDir =
    (netDir |> buildDir) + feldSpar + (netVersionFileName netDir) + ".dll" |> GetAssemblyVersionString 

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
    let destination = feldSpar |> asPath (buildDir netDir)
    let projects = fSharpProjects netDir
    let release = releaseDir netDir
    build target release destination "AppBuild-Output:" projects


let buildConsole netDir =
    let projects = fSharpProjects netDir
    build ("./FeldSpar." + continuousIntegration + "/") (releaseDir netDir) (buildDir netDir) "BuildConsole-Output:" projects

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

    let result = Shell.Exec ((buildDir netDir) + "FeldSpar" + netVersion + "." + continuousIntegration + ".exe" ,"--v ERRORS --r \".\\RunReport.json\"  --a \".\\FeldSpar" + netVersion + ".Tests.exe\"", ?dir=Some(testDir netDir))
    if result <> 0 then failwith "Failed Tests"

Target "Nuke" (fun _ ->
    Shell.Exec ("git", "clean -xfd")  |> ignore
)

Target "Clean" (fun _ ->
    CleanDirs [None |> buildDir; None |> testDir; None |> deployDir; nugetDeployDir None]
)

let doesNothing = (fun _ -> ())

Target "BuildApp" doesNothing

Target "BuildApp46" (fun _ ->
    Some(net46) |> buildApp
)

Target "BuildApp40" (fun _ ->
    Some(net40) |> buildApp
)

Target "BuildConsole" doesNothing

Target "BuildConsole40" (fun _ ->
    Some(net40) |>  buildConsole
)

Target "BuildConsole46" (fun _ ->
    Some(net46) |> buildConsole
)

Target "BuildTest" doesNothing
         
Target "BuildTest40" (fun _ ->
    Some(net40) |> buildTest
)
         
Target "BuildTest46" (fun _ ->
    Some(net46) |> buildTest
)

Target "Test" doesNothing

Target "Test40" (fun _ ->
    Some(net40) |> test
)

Target "Test46" (fun _ ->
    Some(net46) |> test
)

let zip netDir =
    let sourceDir = None |> buildDir
    let destDir = None |> deployDir
    
    let dirInfo = System.IO.DirectoryInfo(destDir)
    if not dirInfo.Exists then
        dirInfo.Create()

    !! (sourceDir + "/**/*.*")
        |> Zip sourceDir (destDir + "FeldSparFSharp." + (version netDir) + ".zip")

Target "Zip" (fun _ ->
    zip (Some(net40))
    zip (Some(net46))
)

Target "Default" doesNothing

Target "Nuget" (fun _ ->
    //Shell.Exec ("nuget", @"pack ..\FeldSparFramework\FeldSpar" + (netVersionFileName netDir) + ".Framework.fsproj -IncludeReferencedProjects -Prop Configuration=Release", netDir |> deployDir) |> ignore
    //Shell.Exec ("nuget", @"pack ..\FeldSpar.ContinuousIntegration\FeldSpar" + (netVersionFileName netDir) + ".ContinuousIntegration.fsproj -IncludeReferencedProjects -Prop Configuration=Release", netDir |> deployDir) |> ignore
    ()
)

let localDelpoy netDir = 
    let dir = netDir |> deployDir
    let nugetDir = nugetDeployDir netDir
    if dir = nugetDir then ()
    else
        FileSystemHelper.directoryInfo dir
            |> FileSystemHelper.filesInDir
            |> Array.filter (fun fi -> fi.Extension = ".nupkg")
            |> Array.map (fun fi -> fi.FullName)
            |> Copy nugetDir

        FileSystemHelper.directoryInfo nugetDir
            |> FileSystemHelper.filesInDir
            |> Array.map (fun fi -> fi.FullName)
            |> Array.iter (printfn "LocalDeploy-Output: %s")
    
    use file = System.IO.File.Create(dir + "push.txt")
    let writer = new System.IO.StreamWriter(file)

    FileSystemHelper.directoryInfo nugetDir
        |> FileSystemHelper.filesInDir
        |> Array.filter (fun fi -> fi.Extension = ".nupkg")
        |> Array.map (fun fi -> fi.FullName)
        |> Array.iter (fun name -> writer.WriteLine(sprintf "nuget push %A" name))

    writer.Close()

    printfn "%A" (System.DateTime.Now)

Target "LocalDeploy" (fun _ ->
    localDelpoy (Some(net40))
    localDelpoy (Some(net46))
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

    ==> "Test40"
    ==> "Test46"
    ==> "Test"

    ==> "Zip"
    ==> "Default"

    ==> "Nuget"
    ==> "LocalDeploy"

// start build
RunTargetOrDefault "Default"