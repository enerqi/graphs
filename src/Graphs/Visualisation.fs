﻿namespace Graphs

open System.IO

module Proc = Fake.ProcessHelper


module Visualisation = 

    open System
    open Chessie.ErrorHandling
    open Algorithms 

    /// Transform a graph to a string that is a valid dot graph description language of it
    let toDotGraphDescriptionLanguage (graph: Graph) = 
        let descriptionOpen = 
            if graph.IsDirected then
                "digraph {"
            else
                "graph {"
        let descriptionClose = "}"
        
        let edgeToString = 
            let edgeSyntax = 
                if graph.IsDirected then
                    " -> "
                else 
                    " -- "
            (fun (v1, v2) -> 
                string v1 + edgeSyntax + string v2)

        let edges = edgesSet graph
        let edgeDescriptions = edges 
                               |> Seq.map edgeToString
                               |> Seq.map (fun s -> "    " + s)

        seq { yield descriptionOpen
              yield! edgeDescriptions
              yield descriptionClose}
        |> String.concat "\n"


    /// Run the external 'dot' process to generate a graph image file
    /// Returns a Result of the image path or Error if creation failed
    let makeGraphVisualisation dotDescription outFilePathNoExtension = 

        let getOutFileDirectory = 
            tryF (fun _ -> Path.GetDirectoryName(outFilePathNoExtension)) FileAccessFailure
        
        let checkDirExists outDir = 
            if not (Directory.Exists outDir) then 
                let exn = new ArgumentException("outFilePathNoExtension", 
                                                sprintf "Directory of out file (%s) does not exist" outDir)
                fail (FileAccessFailure exn)
            else
                ok ()

        let makeTempFile = 
            tryF (fun _ -> Path.GetTempFileName()) FileAccessFailure
        
        trial {
            let! outDir = getOutFileDirectory
            do! checkDirExists outDir

            let! dotTempFileName = makeTempFile
            
            let writeDotTempFile = tryF (fun _ -> File.WriteAllText(dotTempFileName, dotDescription)) FileAccessFailure
            do! writeDotTempFile

            let dotCmd = "dot"
            let dotVisualisationFile = outFilePathNoExtension + ".png"
            let dotArgs = "-Tpng " + dotTempFileName + " -o " + dotVisualisationFile

            let runExternalProcess = tryF (fun _ -> Proc.Shell.Exec(dotCmd, dotArgs, outDir) |> ignore) VisualisationFailure
            do! runExternalProcess
       
            return dotVisualisationFile
        }
