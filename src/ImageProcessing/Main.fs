namespace ImageProcessing

open System
open ConsoleParsing
open ImageProcessing
open Streaming
open Logging

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

    let defaultProcessMethod = Seq


    let getApplicators filters rotations =
        List.append
            (List.map (fun (filter: FilterKernel) -> applyFilter filter.Kernel) filters)
            (List.map (fun (direction: Direction) -> rotate90 direction) rotations)


    [<EntryPoint>]
    let main (argv: string array) =

        let parser = ArgumentParser.Create<Arguments>(programName = "ImageProcessing")
        let results = parser.Parse argv

        let path = results.GetResult Path
        let pathIn = fst path
        let pathOut = snd path

        let imgCount =
            if (not (results.Contains Filter)) && (not (results.Contains Rotate)) then
                results.Raise(NoTransformationsException("No transformations were specified"))

            else
                
                if not (results.Contains Logging) then
                    logger.Terminate()
                    
                let filters = results.GetResults Filter
                let rotations = results.GetResults Rotate

                let method =
                    if results.Contains Method then
                        (results.GetResult Method).GetAllResults()[0]
                    else
                        defaultProcessMethod

                let applicators = getApplicators filters rotations

                match method with
                | Seq -> processImagesSequentially pathIn pathOut applicators
                | Agent args -> processImagesUsingAgents pathIn pathOut applicators (args.GetAllResults())
                | AgentParallel -> processImagesParallelUsingAgents pathIn pathOut applicators

        finalMessage imgCount

        0
