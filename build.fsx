#r "paket:
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Api.GitHub
nuget Fake.DotNet.AssemblyInfoFile
nuget Fake.Core.Target //"
#load ".fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators
open Fake.DotNet.NuGet
open Fake.Api

Target.initEnvironment ()

let frameworkVer = SemVer.parse "1.2.0.3"
let ciVer = SemVer.parse "1.2.0.2"

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
    let fVer = sprintf "%A" (frameworkVer.AsString)
    let frameworkAttributes = 
        [ 
            Fake.DotNet.AssemblyInfoFile.Attribute ( "AssemblyVersion", fVer, "System.Reflection", "System.String" )
            Fake.DotNet.AssemblyInfoFile.Attribute ( "AssemblyFileVersion", fVer, "System.Reflection", "System.String" )
        ] |> Seq.ofList

    Fake.DotNet.AssemblyInfoFile.updateAttributes "./FeldSpar.Framework/AssemblyInfo.fs" frameworkAttributes

    ["./FeldSpar.sln"]
    |> Seq.iter (DotNet.build (fun op -> { op with Configuration = DotNet.BuildConfiguration.Debug;  }))
)

Target.create "TestCopy" (fun _ ->
    "./FeldSpar.Tests/bin/Debug/"
    |> System.IO.DirectoryInfo
    |> fun di ->
        di.GetDirectories ()
        |> Seq.iter (fun dii -> dii.MoveTo (sprintf "%s%s" testDir dii.Name))

        di.GetFiles ()
        |> Seq.iter (fun fi -> fi.MoveTo (sprintf "%s%s" testDir fi.Name))
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

////Example
//Target.create "GitHubRelease" (fun _ ->
//    let token =
//        match Environment.environVarOrDefault "github_token" "" with
//        | s when not (System.String.IsNullOrWhiteSpace s) -> s
//        | _ -> failwith "please set the github_token environment variable to a github personal access token with repro access."

//    let files =
//        [ "portable"; "packages" ]
//        |> List.map (fun n -> sprintf "release/dotnetcore/Fake.netcore/fake-dotnetcore-%s.zip" n)

//    GitHub.createClientWithToken token
//    |> GitHub.draftNewRelease gitOwner gitName release.NugetVersion (release.SemVer.PreRelease <> None) release.Notes
//    |> GitHub.uploadFiles files
//    |> GitHub.publishDraft
//    |> Async.RunSynchronously
//)

Target.create "Zip" (fun _ ->
    ()
)

Target.create "All" ignore

"Clean"
  ==> "Build"
  ==> "TestCopy"
  ==> "Test"
  ==> "All"

Target.runOrDefault "All"
