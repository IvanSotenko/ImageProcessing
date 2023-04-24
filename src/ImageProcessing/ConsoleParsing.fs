module ImageProcessing.ConsoleParsing

open Argu
open ImageProcessing

exception NoTransformationsException of string

type Direction =
    | Left
    | Right


type Filters =
    | GaussianBlur
    | Edges
    | MotionBlur
    | YSobel
    | Emboss
    | OutLine

    member this.Kernel =
        match this with
        | GaussianBlur -> gaussianBlurKernel
        | Edges -> edgesKernel
        | MotionBlur -> motionBlurKernel
        | YSobel -> ySobelKernel
        | Emboss -> embossKernel
        | OutLine -> outlineKernel


type Arguments =
    | [<ExactlyOnce>] Path of pathIn: string * pathOut: string
    | [<AltCommandLine("-fl")>] Filter of name: Filters
    | [<AltCommandLine("-rt")>] Rotate90 of direction: Direction

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Path _ ->
                "Specify the directory from which to take the images and the
                         directory where to save the processed ones OR specify the path
                         to the image to be processed and the path where to save the processed one."
            | Filter _ -> "Specify filter to apply."
            | Rotate90 _ -> "Specify the direction of rotation."
