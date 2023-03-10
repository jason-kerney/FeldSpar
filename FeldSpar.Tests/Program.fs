﻿namespace FeldSpar.Console.Tests
open FeldSpar.Framework
open FeldSpar.Framework.ConsoleRunner
open FeldSpar.Framework.Engine
open FeldSpar.Framework.Verification.ApprovalsSupport
open ApprovalTests

module Program =
    
    //let ``Setup Global Reports`` = 
    let Setup_Global_Reports = 
        Config(fun () -> 
        { 
            Reporters = [
                            fun _ -> 
                                    Searching
                                        |> findFirstReporter<Reporters.DiffReporter>
                                        |> findFirstReporter<Reporters.WinMergeReporter>
                                        |> findFirstReporter<Reporters.InlineTextReporter>
                                        |> findFirstReporter<Reporters.AllFailingTestsClipboardReporter>
                                        |> unWrapReporter
                                            
                            fun _ -> Reporters.ClipboardReporter() :> Core.IApprovalFailureReporter;

                            fun _ -> Reporters.QuietReporter() :> Core.IApprovalFailureReporter;
                        ]
        })

    let currentToken =
        let assembly = System.Reflection.Assembly.GetExecutingAssembly()
        let assemblyPath =
            let codebase = assembly.Location
            let uriBuilder = System.UriBuilder codebase
            let path = System.Uri.UnescapeDataString uriBuilder.Path
            System.IO.Path.GetDirectoryName path

        { new IToken with
            member this.AssemblyPath = assemblyPath
            member this.AssemblyName = System.IO.Path.GetFileName assemblyPath
            member this.Assembly = assembly
            member this.IsDebugging = false
            member this.GetExportedTypes () = assembly.GetExportedTypes()
        }

    runAndReportFailure UseAssemblyConfiguration ShowDetails currentToken |> ignore

    printf "\nDone!"

    //System.Console.ReadKey true |> ignore
