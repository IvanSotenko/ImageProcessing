namespace ImageProcessing

open Argu
open System.Collections.Generic

module Main =

    type Arguments =
        | [<Mandatory>] DirIn of path: string
        | [<Mandatory>] DirOut of path: string
        | [<AltCommandLine("-fi")>] Filter of name: string

        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | DirIn _ -> "Specify the directory from which to process files."
                | DirOut _ -> "Specify the directory in which to save the processed files."
                | Filter _ -> "Specify filter to apply."

    [<EntryPoint>]
    let main (argv: string array) =

        let parser = ArgumentParser.Create<Arguments>(programName = "ImageProcessing.exe")
        let results = parser.Parse argv
        let all = results.GetAllResults()

        let dict = Dictionary<string, string>()

        let addToDict arg =
            match arg with
            | DirIn path -> dict.Add("DirIn", path)
            | DirOut path -> dict.Add("DirOut", path)
            | Filter name -> dict.Add("Filter", name)

        for arg in all do
            addToDict arg

        let filterKernel =
            match dict["Filter"] with
            | "gaussianBlur" -> ImageProcessing.gaussianBlurKernel
            | "edges" -> ImageProcessing.edgesKernel
            | "motionBlur" -> ImageProcessing.motionBlurKernel
            | "ySobel" -> ImageProcessing.ySobelKernel
            | "emboss" -> ImageProcessing.embossKernel
            | "outline" -> ImageProcessing.embossKernel
            | otherName -> failwith $"There is no filter named {otherName}"

        ImageProcessing.applyFilterToDirectory filterKernel dict["DirOut"] dict["DirIn"]
        printfn "DONE!"

        0
