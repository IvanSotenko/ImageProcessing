module ImageProcessing.ConsoleParsing

open Argu
open ImageProcessing

exception NoTransformationsException of string

type Method =
    | Seq
    | Agent

type Arguments =
    | [<ExactlyOnce>] Path of pathIn: string * pathOut: string
    | Filter of name: Filters
    | Rotate of direction: Direction
    | [<AltCommandLine("-m")>] Method of name: Method

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Path _ ->
                "Specify the directory from which to take the images OR path to the image to be processed and the directory where to save the processed ones."
            | Filter _ -> "Specify filter to apply."
            | Rotate _ -> "Specify the direction of rotation."
            | Method _ -> "Specify the image processing method."
