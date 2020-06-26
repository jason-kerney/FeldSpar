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

let forkReport (a : string seq) =
    a |> Seq.iter (printfn "%s")
    a

Target.create "Clean" (fun _ ->
    !! "./**/bin/**"
    ++ "./**/obj/**"
    ++ "./**/bin"
    ++ "./**/obj"
    |> Shell.cleanDirs 

    [buildDir; testDir; deployDir] |> Shell.cleanDirs
)

Target.create "Build" (fun _ ->
    ["./FeldSpar.sln"]
    |> forkReport
    |> Seq.iter (DotNet.build (fun op -> { op with Configuration = DotNet.BuildConfiguration.Debug }))

    !! "./**/bin/"
    |> Seq.map (System.IO.DirectoryInfo)
    |> Seq.map (DirectoryInfo.getMatchingFiles "*.dll;*.exe;*.txt")
    |> List.ofSeq
    |> List.map (List.ofSeq)
    |> List.fold (List.append) []
    |> List.iter (fun fi -> fi.CopyTo (buildDir) |> ignore)
)

Target.create "All" ignore

"Clean"
  ==> "Build"
  ==> "All"

Target.runOrDefault "All"
