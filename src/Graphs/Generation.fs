﻿namespace Graphs

open System.IO

module Generation = 

    open Chessie.ErrorHandling

    /// Transform a pair string of integers e.g. "1 2" and return a result of the tuple of the integer values
    let extractHeader (header: string) : GraphResult<int* int> = 
        header.Split() |> Array.map int |> fun xs -> 
            match xs with 
            | [|vC; eC|] -> ok (vC, eC)
            | _ -> fail (ParsingFailure <| sprintf "Failed to extract header pair fom string: %s" header)
  
    /// Transform a pair string of integers e.g. "1 2" and return result of the tuple of the VertexId values
    let extractVertexIdPair (pairString: string) : GraphResult<VertexId * VertexId> = 
        match pairString.Split() with
        | [|v1; v2|] -> ok (VertexId (int v1), VertexId (int v2))
        | _ -> fail (ParsingFailure <| sprintf "Failed to extract pair from vertex pair string: %s" pairString)
            

    /// Parse a line-oriented string representation to create a graph
    ///
    /// Line 1: n (#vertices) m (#edges)
    ///         vertices use 1 based indices, 1 to n
    /// Line (2 to m-1): edge u v with id (>= 1 and <= n) - directed or undirected according to the problem.
    ///                | edge u v w - includes the weight 
    /// For now the graph should be simple - no self loops nor parallel edges.
    let readGraph isDirected (dataLines: seq<string>) : GraphResult<Graph> = 
        
        let header, edges = Seq.head dataLines, Seq.tail dataLines
        
        // Mutable datastructures + functional style Result type error handling is a 
        // little painful/weird to code mixing pipelining with mutation.
        // We use Result.map repeatedly to collect all the data needed to create the graph
        // We could create multiple Results - one for vertex/edge counts, one for edges etc.
        // but we would have to collect the multiple Results into one and something like 
        // Result.collect is not definitely in the F# 4.1 API at the moment.
        // It looks prettier pushing data through one pipeline aswell

        let withVertexArray (verticesCount, edgesCount) : (int * int * Vertex []) = 
            // The entry at index 0 will be ignored. Keeping it saves on offset calculations.
            // 1-based indices. F# inclusive [x..y]
            let vertexArray = [|for vIndex in [0..verticesCount] do
                                yield { Identifier = VertexId vIndex;
                                        Neighbours = new ResizeArray<VertexId>() } |]
            (verticesCount, edgesCount, vertexArray)

        let withEdgeVertexPairs (verticesCount, edgesCount, vertexArray) : (int * int * Vertex [] * seq<GraphResult<VertexId * VertexId>>) = 
            let pairs = edges |> Seq.map extractVertexIdPair
            (verticesCount, edgesCount, vertexArray, pairs)        
            
        let addAllEdgesToVertexArray ((_, _, vertexArray, pairs: seq<GraphResult<VertexId * VertexId>>), _) = 

            let addEdge (verticesArray: Vertex[]) (v1: VertexId) (v2: VertexId) = 
                verticesArray.[v1.Id].Neighbours.Add(v2)
                // v2 -> v1 for bi-directionality
                if not isDirected then 
                    verticesArray.[v2.Id].Neighbours.Add(v1)           
                    
            for pairResult in pairs do
                 match pairResult with 
                 | Ok ((v1, v2), _) -> addEdge vertexArray v1 v2                                
                 | Bad e -> printfn "Ignoring vertex pair that could not parse: %A" e        
                        
           
        let toGraph (verticesCount, edgesCount, vertexArray, _) = 
            { VerticesCount = verticesCount; 
              IsDirected = isDirected;
              EdgesCount = edgesCount;
              Vertices = vertexArray }              

        
        header
        |> extractHeader        
        |> lift withVertexArray
        |> lift withEdgeVertexPairs
        |> successTee addAllEdgesToVertexArray // side effecting
        |> lift toGraph


    /// Parse a line-oriented string representation from a file to create a graph
    let readGraphFromFile (isDirected: bool) (filePath:string) : GraphResult<Graph> = 
        tryF (fun _ -> File.ReadLines(filePath)) FileAccessFailure
        >>= readGraph isDirected
