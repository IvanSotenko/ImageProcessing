namespace ImageProcessing

open System
open ConsoleParsing
open ImageProcessing
open Streaming

[<AutoOpen>]
module PrintInfo =

    let finalMessage imagesCount =
        let message =
            match imagesCount with
            | 0 -> "No images were found on the specified path."
            | 1 -> "1 image was successfully processed."
            | n when n > 1 -> $"{n} images were successfully processed."
            | _ -> raise (ArgumentException("The count of images is negative."))

        printfn $"{message}"


module Main =
    open Argu

    let getApplicators filters rotations =
        List.append
            (List.map (fun (filter: Filters) -> applyFilter filter.Kernel) filters)
            (List.map (fun (direction: Direction) -> rotate90 direction) rotations)


    [<EntryPoint>]
    let main (argv: string array) =

        let parser = ArgumentParser.Create<Arguments>(programName = "ImageProcessing")
        let results = parser.Parse argv
        
        let path = results.GetResult Path
        let pathOut = fst path
        let pathIn = snd path
        
        let imgCount =
            if (not (results.Contains Filter)) && (not (results.Contains Rotate)) then
                results.Raise(NoTransformationsException("No transformations were specified"))
        
            else
                let filters = results.GetResults Filter
                let rotations = results.GetResults Rotate
                
                let applicators = getApplicators filters rotations
                
                if results.Contains Seq then
                    processImagesSequentially pathOut pathIn applicators
                elif results.Contains Agent then
                    let agentArgs = results.GetResult Agent
                    processImagesUsingAgents pathOut pathIn applicators agentArgs
                elif results.Contains AgentParallel then
                    0
                else
                    failwith "No method specified"
        
        finalMessage imgCount
        
        0
