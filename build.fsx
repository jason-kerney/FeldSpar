#r "paket:
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.IO.Zip
nuget Fake.Api.GitHub
nuget Fake.DotNet.AssemblyInfoFile
nuget Fake.Core.XML
nuget Fake.Core.Target //"

#load ".fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators
open Fake.Api
open Fake.Core.Xml

Target.initEnvironment ()

let frameworkVer = SemVer.parse "2.0.0"
let ciVer = SemVer.parse "2.0.0"

type BuildType =
    | Debug
    override this.ToString () =
        match this with
        | Debug -> "Debug"

// Properties
let buildDir = "./_build/"
let testDir = "./_test/"
let deployDir = "./_deploy/"

let fork f thing =
    f thing
    thing

let nugetDeployDir = 
    let p = 
        let t = "C:/Nuget.Local/"
        if System.IO.Directory.Exists t then t
        else deployDir
    System.IO.DirectoryInfo p
    
Target.create "Clean" (fun _ ->
    !! "./**/bin"
    ++ "./**/obj"
    |> Shell.cleanDirs 

    [buildDir; testDir; deployDir] |> Shell.cleanDirs
)

let setVersion projectPath versionString =
    let proj = Xml.loadDoc projectPath
    let proj = Xml.replaceXPathInnerText "/Project/PropertyGroup/Version" versionString proj
    proj.Save projectPath


Target.create "SetVersions" (fun _ ->
    setVersion "./FeldSpar.Framework/Framework.fsproj" frameworkVer.AsString
    setVersion "./FeldSpar.ContinuousIntegration/ContinuousIntegration.fsproj" ciVer.AsString
)

Target.create "Build" (fun _ ->
    ["./FeldSpar.sln"]
    |> Seq.iter (DotNet.build (fun op -> { op with Configuration = DotNet.BuildConfiguration.Debug;  }))
)

let copyDirectory target (source:System.IO.DirectoryInfo) =
     let targetPath = System.IO.Path.Combine (target, source.Name)
     System.IO.Directory.CreateDirectory targetPath |> ignore

     source.GetFiles ()
     |> Array.map (fun f -> f, System.IO.Path.Combine(targetPath, f.Name))
     |> Array.map(fun (f, t) -> f.CopyTo(t))

let getBuildDir folder = 
    sprintf "%s/%s" buildDir folder

let copyBuildFiles folder =
    let buildTarget = Debug
    let target = getBuildDir folder
    let lib = sprintf "%s/lib" target 
    let source = buildTarget.ToString ()

    !! ("./" + folder + "/**/content")
    ++ ("./" + folder + "/**/tools")
    |> Seq.map System.IO.DirectoryInfo
    |> Seq.iter (fun di -> copyDirectory target di |> ignore)

    !! ("./" + folder + "/bin/" + source + "/**/*.dll")
    ++ ("./" + folder + "/bin/" + source + "/**/*.exe")
    |> Seq.map (fun p -> lib |> System.IO.DirectoryInfo, System.IO.FileInfo p)
    |> Seq.map (fun (di, fi) -> 
            let rec getFolders (current : System.IO.DirectoryInfo) acc =
                if current.Parent.Name = source then acc
                else
                    let p = sprintf "%s%s/" current.Name acc
                    getFolders current.Parent p

            let f = getFolders fi.Directory ""
            let path = sprintf "%s/%s" di.FullName f
            let d = System.IO.DirectoryInfo path

            if not d.Exists then d.Create ()

            fi.Name |> sprintf "%s%s" path, fi
        )
    |> Seq.iter (fun (p, fi) -> fi.CopyTo p |> ignore)

Target.create "BuildCopy" (fun _ ->
    System.Threading.Thread.Sleep 500
    "FeldSpar.Framework" |> copyBuildFiles
    
    //let ciSource = System.IO.DirectoryInfo "./FeldSpar.ContinuousIntegration/bin/Debug/netcoreapp3.1"
    let ciTarget = "FeldSpar.ContinuousIntegration" |> getBuildDir |> sprintf "%s/lib" |> System.IO.DirectoryInfo
    //Fake.IO.DirectoryInfo.copyRecursiveTo true ciTarget ciSource |> ignore

    !! "./FeldSpar.ContinuousIntegration/bin/Debug/**/ApprovalTests.dll"
    ++ "./FeldSpar.ContinuousIntegration/bin/Debug/**/ApprovalUtilities.dll"
    ++ "./FeldSpar.ContinuousIntegration/bin/Debug/**/Argu.dll"
    ++ "./FeldSpar.ContinuousIntegration/bin/Debug/**/FeldSpar.ContinuousIntegration.dll"
    ++ "./FeldSpar.ContinuousIntegration/bin/Debug/**/FeldSpar.ContinuousIntegration.exe"
    ++ "./FeldSpar.ContinuousIntegration/bin/Debug/**/*.json"
    ++ "./FeldSpar.ContinuousIntegration/bin/Debug/**/FSharp.Core.dll"
    ++ "./FeldSpar.ContinuousIntegration/bin/Debug/**/FeldSparFramework.dll"
    |> Seq.iter (fun n -> 
        let fi = System.IO.FileInfo n
        let fiTarget = fi.Name |> sprintf "%s/%s" ciTarget.FullName

        if not ciTarget.Exists then ciTarget.Create ()

        fi.CopyTo fiTarget |> ignore
    )

    let frameworkBuild = "FeldSpar.Framework" |> getBuildDir
    let ciBuild = "FeldSpar.ContinuousIntegration" |> getBuildDir

    "./FeldSparFramework.nuspec"
    |> System.IO.FileInfo
    |> fun fi -> (frameworkBuild |> System.IO.DirectoryInfo, fi)
    |> fun (di, fi) -> sprintf "%s/%s" di.FullName fi.Name |> fi.CopyTo 
    |> ignore
    
    "./ContinuousIntegration.nuspec"
    |> System.IO.FileInfo
    |> fun fi -> (ciBuild |> System.IO.DirectoryInfo, fi)
    |> fun (di, fi) -> sprintf "%s/%s" di.FullName fi.Name |> fi.CopyTo 
    |> ignore
)

Target.create "DeployCopy" (fun _ ->
    !! (sprintf "%s/**/*.nupkg" buildDir)
    ++ (sprintf "%s/**/*.zip" buildDir)
    |> fork (Seq.iter (printfn "\t%s"))
    |> Shell.copy deployDir
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

Target.create "CreatePackage" (fun _ ->
    Shell.Exec ("nuget", @"pack .\FeldSpar.Framework\FeldSparFramework.nuspec -IncludeReferencedProjects -Version " + frameworkVer.AsString, buildDir) |> ignore
    Shell.Exec ("nuget", @"pack .\FeldSpar.ContinuousIntegration\ContinuousIntegration.nuspec -IncludeReferencedProjects -Version " + ciVer.AsString, buildDir) |> ignore
    ()
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
    let frameworkSource = sprintf "%s/FeldSpar.Framework" buildDir |> System.IO.DirectoryInfo |> fun di -> di.FullName
    let ciSource = sprintf "%s/FeldSpar.ContinuousIntegration" buildDir |> System.IO.DirectoryInfo |> fun di -> di.FullName
    let version =
        if frameworkVer < ciVer then ciVer
        else frameworkVer

    [   !! (sprintf "%s/**/*" frameworkSource)
            |> Zip.filesAsSpecs frameworkSource
            |> Zip.moveToFolder "FeldSpar.Framework"
        !! (sprintf "%s/**/*" ciSource)
            |> Zip.filesAsSpecs ciSource
            |> Zip.moveToFolder "FeldSpar.ContinuousIntegration"
    ]
    |> Seq.concat
    |> Zip.zipSpec (sprintf @"%s/FeldSpar.%s.zip" buildDir version.AsString)
)

Target.create "LocalDeploy" (fun _ ->
    printfn "--------> Deploy to: %s" deployDir

    [nugetDeployDir.FullName]
    |> Shell.cleanDirs

    !! (sprintf "%s/*.nupkg" deployDir)
    |> fork (Seq.iter (printfn "\t%s <- %A" deployDir))
    |> Shell.copy nugetDeployDir.FullName
)

Target.create "All" ignore

"Clean"
  ==> "SetVersions"
  ==> "Build"
  ==> "BuildCopy"
  ==> "Zip"
  ==> "TestCopy"
  ==> "Test"
  ==> "CreatePackage"
  ==> "DeployCopy"
  ==> "LocalDeploy"
  ==> "All"

Target.runOrDefault "Test"
