module Graphs.Tests

open System
open System.IO

open Graphs
open Fuchu
open FsUnit
open Swensen.Unquote
open Swensen.Unquote.Operators // [Under the hood extras]

module private TestUtils =
    let test_graph_file file_name = 
        let testFilesDir = __SOURCE_DIRECTORY__
        let path = Path.GetFullPath (Path.Combine(testFilesDir, file_name))
        if not <| File.Exists(path) then 
            failwith <| sprintf "test file %s not found" path
        path

    let load_undirected_test_graph = 
        let isDirected = false
        Generation.readGraph (test_graph_file "undirected_graph.txt") isDirected

    let load_directed_test_graph = 
        let isDirected = true
        Generation.readGraph (test_graph_file "directed_graph.txt") isDirected

    let neighbours graph vertexIdNumber = 
        VertexId vertexIdNumber 
        |> Graph.vertexFromId graph 
        |> (fun v -> v.Neighbours)

    let neighbourIdNumbers graph vertexIdNumber = 
        neighbours graph vertexIdNumber
        |> Seq.map (fun vId -> vId.Id)
        |> Array.ofSeq
        |> Array.sort

open TestUtils

[<Tests>]
let graphTypeTests = 
    testList "Graph ADT" [
        testCase "vertex lookup" <| fun _ ->
            let g = load_undirected_test_graph
            let v1 = Graph.vertexFromId g (VertexId 1)
            v1.Identifier |> should equal (VertexId 1)

        testCase "vert lookup with zero or less id throws " <| fun _ ->            
            let g = load_undirected_test_graph
            (fun () -> Graph.vertexFromId g (VertexId 0) |> ignore)
                |> should throw typeof<System.Exception>

        testCase "vert lookup with out of range id throws" <| fun _ ->
            let g = load_undirected_test_graph
            (fun () -> Graph.vertexFromId g (VertexId 99) |> ignore)
                |> should throw typeof<System.IndexOutOfRangeException>
                
        testCase "vertices sequence is 1 based" <| fun _ ->
            let g = load_undirected_test_graph
            let verts = Graph.verticesSeq g
                        |> Array.ofSeq 
            verts |> Array.exists (fun vertex -> vertex.Identifier.Id = 0) 
                  |> should be False
    ]

[<Tests>]
let generationTests = 
    testList "Graph Generation" [
        testCase "Serialized header parsing" <| fun _ ->
            Generation.extractHeader "1 2" |> should equal (1, 2)

        testCase "Header parsing blows up on wrong number of inputs" <| fun _ ->
            (fun () -> Generation.extractHeader "1 2 3" |> ignore) 
                |> should throw typeof<System.Exception>

        testCase "Header parsing blows up if cannot parse to numbers" <| fun _ ->
            (fun () -> Generation.extractHeader "a b" |> ignore) 
                |> should throw typeof<System.FormatException>

        testCase "Vertex Id pair parsing" <| fun _ ->
            Generation.extractVertexIdPair "1 2" |> should equal (VertexId 1, VertexId 2)

        testCase "Vertex Id pair parsing blows if cannot parse numbers" <| fun _ ->
            (fun () -> Generation.extractVertexIdPair "a b" |> ignore) 
                |> should throw typeof<System.FormatException>

        testCase "Vertex Id parsing blows up on wrong number of inputs" <| fun _ ->
            (fun () -> Generation.extractVertexIdPair "1 2 3" |> ignore) 
                |> should throw typeof<System.Exception>
                        
        testCase "read undirected graph works" <| fun _ ->
            let isDirected = false
            let g = Generation.readGraph (test_graph_file "undirected_graph.txt") isDirected
            g.VerticesCount |> should equal 4
            g.EdgesCount |> should equal 5
            g.IsDirected |> should be False
            let adjacents = 
                neighbourIdNumbers g 
            adjacents 1 |> should equal [2; 4]
            adjacents 2 |> should equal [1; 3; 4]
            adjacents 3 |> should equal [2; 4]
            adjacents 4 |> should equal [1; 2; 3]

        testCase "read directed graph works" <| fun _ ->
            let isDirected = true
            let g = Generation.readGraph (test_graph_file "directed_graph.txt") isDirected
            g.VerticesCount |> should equal 5
            g.EdgesCount |> should equal 8
            g.IsDirected |> should be True
            let adjacents = 
                neighbourIdNumbers g 
            adjacents 1 |> should equal [2]
            adjacents 2 |> should equal [5]
            adjacents 3 |> should equal [1; 4]
            adjacents 4 |> should equal [3]
            adjacents 5 |> should equal [1; 3; 4]

        // Malicious / incorrect vertex numbers not currently cleanly handled:
        // - vertexIds out of range
        // - zero based indexing (we use 1-based)
    ]   

[<Tests>]
let visualisationTests = 
    testList "dot file language " [
        testCase "undirected graph" <| fun _ ->
            // match each line but strip leading/trailing whitespace
            "graph {
                1 -- 2
                1 -- 4
                2 -- 3
                2 -- 4
                3 -- 4
            }"
            |> ignore

        testCase "directed graph" <| fun _ ->
            "digraph {
                1 -> 2
                2 -> 5
                3 -> 1
                3 -> 4
                4 -> 3
                5 -> 1
                5 -> 3
                5 -> 4
            }"        
            |> ignore
    ]
    

// EXAMPLES //////////////////////////////////////////////////////////////////////////////////////////////////////
let fsCheckConfigOverride = { FsCheck.Config.Default with MaxTest = 10000 }
let testExamples = 
    testList "test list example" [
        
        testCase "Fuchu's basic assertion mechanism" <| fun _ ->
            Assert.Equal("1 is 1", 1, 1)                   

        testCase "FsUnit's expanded assertion library" <| fun _ ->
            1 |> should equal 1 
            1 |> should not' (equal 2)
            10.1 |> should (equalWithin 0.1) 10.11
            10.1 |> should not' ((equalWithin 0.001) 10.11)
            "ships" |> should startWith "sh"
            "ships" |> should not' (startWith "ss")
            "ships" |> should endWith "ps"
            "ships" |> should not' (endWith "ss")
            [1] |> should contain 1
            [] |> should not' (contain 1)
            [1..4] |> should haveLength 4
            //(fun () -> failwith "BOOM!" |> ignore) |> should throw typeof<System.Exception>
            true |> should be True
            false |> should not' (be True)
            "" |> should be EmptyString
            "" |> should be NullOrEmptyString
            null |> should be NullOrEmptyString
            null |> should be Null
            let anObj = "hi"
            let otherObj = "ho"
            anObj |> should not' (be Null)
            anObj |> should be (sameAs anObj)
            anObj |> should not' (be sameAs otherObj)
            11 |> should be (greaterThan 10)
            9 |> should not' (be greaterThan 10)
            11 |> should be (greaterThanOrEqualTo 10)
            9 |> should not' (be greaterThanOrEqualTo 10)
            10 |> should be (lessThan 11)
            10 |> should not' (be lessThan 9)
            10.0 |> should be (lessThanOrEqualTo 10.1)
            10 |> should not' (be lessThanOrEqualTo 9)
            0.0 |> should be ofExactType<float>
            1 |> should not' (be ofExactType<obj>)
            [] |> should be Empty // NUnit only
            [1] |> should not' (be Empty) // NUnit only
            "test" |> should be instanceOfType<string> // Currently, NUnit only and requires version 1.0.1.0+
            "test" |> should not' (be instanceOfType<int>) // Currently, NUnit only and requires version 1.0.1.0+
            2.0 |> should not' (be NaN) // Currently, NUnit only and requires version 1.0.1.0+
            [1;2;3] |> should be unique // Currently, NUnit only and requires version 1.0.1.0+

        testCase "Unquote step-by-step expression evaluation" <| fun _ ->
            test <@ (1+2)/3 = 1 @>
            // Some sugared quoted expressions <, > etc. with '!' suffix.
            true =! true
            1 <! 2
            2 >! 1
            4 <=! 4
            5 >=! 5
            "a" <>! "b"

        testCase "Unquote decompiling, evaluating and reducing of quotation expressions" <| fun _ ->
            //open Swensen.Unquote.Operators
            unquote <@ (1+2)/3 @> |> ignore
            decompile <@ (1+2)/3 @> |> ignore
            eval <@ "Hello World".Length + 20 @> |> ignore
            evalRaw<int> <@@ "Hello World".Length + 20 @@> |> ignore
            <@ (1+2)/3 @> |> reduce |> decompile |> ignore
            <@ (1+2)/3 @> |> reduceFully |> List.map decompile |> ignore
            <@ (1+2)/3 @> |> isReduced |> ignore
                    
        testProperty "FsCheck via Fuchu" <| 
            fun a b ->
                a + b = b + a
            
        testPropertyWithConfig fsCheckConfigOverride "Product is distributive over addition " <| 
            fun a b c ->
              a * (b + c) = a * b + a * c

    ]
//////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
[<EntryPoint>]
let main args =
    defaultMainThisAssembly args
    