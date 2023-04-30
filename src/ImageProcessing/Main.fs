namespace ImageProcessing

open Argu
open ConsoleParsing
open ImageProcessing


[<AutoOpen>]
module Processing =
    let applyAllFilters (filters: Filters list) (image: Image) =
        List.fold (fun img (filter: Filters) -> applyFilter filter.Kernel img) image filters

    let applyAllRotations (rotations: Direction list) (image: Image) =
        List.fold
            (fun image rotation ->
                match rotation with
                | Left -> rotate90 image false
                | Right -> rotate90 image true)
            image
            rotations

    let applyAllFiltersMany (filters: Filters list) (images: Image[]) =
        Array.map (applyAllFilters filters) images

    let applyAllRotationsMany (rotations: Direction list) (images: Image[]) =
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
                let image = loadImage pathOut

                let image1 = applyAllFilters filters image
                let image2 = applyAllRotations rotations image1

                saveImage image2 pathIn

                sprintf "Image \"%s\" was processed successfully." (image.Name)

            else
                let images = loadImagesFromDirectory pathOut

                let images1 = applyAllFiltersMany filters images
                let images2 = applyAllRotationsMany rotations images1

                saveManyImages pathIn images2

                sprintf "%i images were successfully processed." images.Length


module Main =

    [<EntryPoint>]
    let main (argv: string array) =

        let parser = ArgumentParser.Create<Arguments>(programName = "ImageProcessing.exe")
        let results = parser.Parse argv

        let response = processing results
        printfn $"{response}"

        0
