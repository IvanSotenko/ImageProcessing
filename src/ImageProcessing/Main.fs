namespace ImageProcessing

open Argu
open ConsoleParsing
open ImageProcessing

module Main =
    
    let applyAllFilters (filters: Filters list) (image: byte[,]) =
        List.fold (fun image (filter: Filters) -> applyFilter filter.Kernel image) image filters
    
    let applyAllRotations (rotations: Direction list) (image: byte[,]) =
        List.fold (fun image rotation -> match rotation with | Left -> rotate90 image false | Right -> rotate90 image true)
                    image
                    rotations
                    
    let applyAllFiltersMany (filters: Filters list) (images: byte[,][]) =
        Array.map (applyAllFilters filters) images
    
    let applyAllRotationsMany (rotations: Direction list) (images: byte[,][]) =
        Array.map (applyAllRotations rotations) images
        
        
    [<EntryPoint>]
    let main (argv: string array) =

        let parser = ArgumentParser.Create<Arguments>(programName = "ImageProcessing.exe")
        let results = parser.Parse argv
        
        let first (a, _) = a
        let second (_, a) = a
        
        let filters = results.GetResults Filter
        let rotations = results.GetResults Rotate90
        let path = results.GetResult Path
        let pathOut = first path
        let pathIn = second path
            
        let processing =
            if (not (results.Contains Filter)) && (not (results.Contains Rotate90)) then
                results.Raise (NoTransformationsException("No transformations were specified"))
            else
                // checking whether the path corresponds to a directory or file
                if System.IO.File.Exists pathOut then
                    let image = loadAs2DArray pathOut
                    
                    let image1 = applyAllFilters filters image
                    let image2 = applyAllRotations rotations image1
                    
                    save2DByteArrayAsImage pathIn image2
                else
                    let images, paths = loadAs2DArrayFromDirectory pathOut
                    
                    let getNewName (path: string) = $"processed_{System.IO.Path.GetFileName path}"
                    let names = Array.map getNewName paths
                    
                    let images1 = applyAllFiltersMany filters images
                    let images2 = applyAllRotationsMany rotations images1
                    
                    save2DByteArrayAsImageMany pathIn names images2
                    
        processing
        printfn "DONE!"

        0
