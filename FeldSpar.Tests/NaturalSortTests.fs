namespace FeldSpar.Console.Tests
open System
open FeldSpar.Console.Helpers
open FeldSpar.Console.Helpers.Data
open FeldSpar.Framework
open FeldSpar.Framework.TestSummaryUtilities
open FeldSpar.Framework.Engine
open FeldSpar.Framework.Verification
open FeldSpar.Framework.Verification.ChecksClean
open FeldSpar.Framework.Verification.ApprovalsSupport
open FeldSpar.Framework.Sorting.Sorters

module NaturalSortTests = 
    let ``Can sort basic alphas`` =
        Test(fun env ->
            ["c";"a";"b"] |> List.sortWith natualCompare |> checkAgainstStandardObjectAsCleanedString env
        )

    let ``Can sort alpha words`` =
        Test(fun env ->
            ["cat"; "apple"; "bat"; "balloon"; "ball"; "cart"; "cats"; "ask"] |> List.sortWith natualCompare |> checkAgainstStandardObjectAsCleanedString env
        )

    let ``Can sort numbers`` =
        Test(fun env ->
            ["10";"3";"11"; "30"; "20"; "12"; "2"; "8"] |> List.sortWith natualCompare |> checkAgainstStandardObjectAsCleanedString env
        )

    let ``Can sort numbers and strings`` =
        Test(fun env ->
            ["10";"3"; "hello"; "11"; "30"; "FeldSpar"; "20"; "Hello"; "cat"; "12"; "2"; "abbot"; "Abbot"; "8"] |> List.sortWith natualCompare |> checkAgainstStandardObjectAsCleanedString env
        )

    let ``Can sort words with numbers`` =
        Test(fun env ->
            [
                "File 20"; "Zoolander"; "File 2"; 
                "File 12"; "Abbot"; "File 11"; 
                "File 1"; "Cat"; "File10";
                "File2"; "File20"; "File13";
                "File"] |> List.sortWith natualCompare |> checkAgainstStandardObjectAsCleanedString env
        )

    let ``Can sort tuples by words with numbers as a sequence`` =
        Test(fun env ->
            [
                ("File 20", 33); 
                ("Zoolander", 33); 
                ("File 2", 33); 
                ("File 12", 33); 
                ("Abbot", 33); 
                ("File 11", 33); 
                ("File 1", 33); 
                ("Cat", 33); 
                ("File10", 33);
                ("File2", 33); 
                ("File20", 33); 
                ("File13", 33);
                ("File", 33)] |> Seq.ofList |> naturalSortBy (fun (key, value) -> key) |> checkAgainstStandardObjectAsCleanedString env
        )