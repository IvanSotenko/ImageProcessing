namespace ImageProcessing

open Argu
open ConsoleParsing
open ImageProcessing


[<AutoOpen>]
module Processing =
    let applyAllFilters (filters: Filters list) (image: byte[,]) =
        List.fold (fun image (filter: Filters) -> applyFilter filter.Kernel image) image filters

    let applyAllRotations (rotations: Direction list) (image: byte[,]) =
        List.fold
            (fun image rotation ->
                match rotation with
                | Left -> rotate90 image false
                | Right -> rotate90 image true)
            image
            rotations

    let applyAllFiltersMany (filters: Filters list) (images: byte[,][]) =
        Array.map (applyAllFilters filters) images

    let applyAllRotationsMany (rotations: Direction list) (images: byte[,][]) =
        Array.map (applyAllRotations rotations) images


    let processing (results: ParseResults<Arguments>) =

        let path = results.GetResult Path
        let pathOut = fst path
        let pathIn = snd path

        if (not (results.Contains Filter)) && (not (results.Contains Rotate90)) then
            results.Raise(NoTransformationsException("No transformations were specified"))

        else
            let filters = results.GetResults Filter
            let rotations = results.GetResults Rotate90

            // checking whether the path corresponds to a directory or file
            if System.IO.File.Exists pathOut then
                let image = loadAs2DArray pathOut

                let image1 = applyAllFilters filters image
                let image2 = applyAllRotations rotations image1

                save2DByteArrayAsImage pathIn image2

                sprintf "Image \"%s\" was processed successfully." (System.IO.Path.GetFileName pathOut)

            else
                let images, paths = loadAs2DArrayFromDirectory pathOut

                let getNewName (path: string) =
                    $"processed_{System.IO.Path.GetFileName path}"

                let names = Array.map getNewName paths

                let images1 = applyAllFiltersMany filters images
                let images2 = applyAllRotationsMany rotations images1

                save2DByteArrayAsImageMany pathIn names images2

                sprintf "%i images were successfully processed." paths.Length


module Main =

    [<EntryPoint>]
    let main (argv: string array) =

        let parser = ArgumentParser.Create<Arguments>(programName = "ImageProcessing.exe")
        let results = parser.Parse argv

        let response = processing results
        printfn $"{response}"

        0
