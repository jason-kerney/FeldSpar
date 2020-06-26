#r "paket:
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Core.Target //"
#load ".fake/build.fsx/intellisense.fsx"
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

Target.initEnvironment ()

// Properties
let fSharpProjects = "*.fsproj"
let releaseDir = "bin/Release/"

let buildDir = "./_build/"
let testDir = "./_test/"
let deployDir = "./_deploy/"

let forkReport lbl (a : 'a seq) =
    a |> Seq.iter (printfn "%s %A" lbl)
    a

let forkReportList lbl a =
    a |> Seq.ofList |> forkReport lbl |> ignore
    a

Target.create "Clean" (fun _ ->
    !! "./**/bin"
    ++ "./**/obj"
    |> forkReport "Cleaning"
    |> Shell.cleanDirs 

    [buildDir; testDir; deployDir] |> Shell.cleanDirs
)

Target.create "Build" (fun _ ->
    ["./FeldSpar.sln"]
    |> forkReportList "building"
    |> Seq.iter (DotNet.build (fun op -> { op with Configuration = DotNet.BuildConfiguration.Debug }))
)

Target.create "TestCopy" (fun _ ->
    "./FeldSpar.Tests/bin/Debug/"
    |> System.IO.DirectoryInfo
    |> fun di ->
        di.GetDirectories ()
        |> forkReport "Moving"
        |> Seq.iter (fun dii -> dii.MoveTo (sprintf "%s%s" testDir dii.Name))

        di.GetFiles ()
        |> forkReport "Moving"
        |> Seq.iter (fun fi -> fi.MoveTo (sprintf "%s%s" testDir fi.Name))

    //|> Seq.map (System.IO.FileInfo)
    //|> Seq.iter (fun fi -> fi.CopyTo (sprintf "%s/%s" testDir fi.Name) |> ignore)
)

Target.create "Test" (fun _ -> 
    let testApp = "./FeldSpar.Tests.exe"
    Shell.pushd (sprintf "%snetcoreapp3.1" testDir)
    let result = 
        CreateProcess.fromRawCommandLine testApp ""
        |> CreateProcess.withWorkingDirectory "."
        |> Proc.run
    Shell.popd ()
    if result.ExitCode = 0 then ()
    else
        failwith (sprintf "Bad tests %A" result)
)

Target.create "All" ignore

"Clean"
  ==> "Build"
  ==> "TestCopy"
  ==> "Test"
  ==> "All"

Target.runOrDefault "All"
