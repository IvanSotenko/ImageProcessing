module ImageProcessing.ConsoleParsing

open Argu
open ImageProcessing
open Streaming

exception NoTransformationsException of string

type Method =
    | [<First; Unique; CliPrefix(CliPrefix.None)>] Seq
    | [<First; Unique; CliPrefix(CliPrefix.None)>] Agent of ParseResults<AgentArgs>
    | [<First; Unique; CliPrefix(CliPrefix.None)>] AgentParallel

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Seq _ -> "Process images sequentially."
            | Agent _ -> "Process the image using agents."
            | AgentParallel _ -> "Process images using parallelism implemented using agents."

and Arguments =
    | [<ExactlyOnce>] Path of pathIn: string * pathOut: string
    | Filter of name: Filters
    | Rotate of direction: Direction
    | [<Unique; AltCommandLine("-m")>] Method of ParseResults<Method>

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Path _ ->
                "Specify the directory from which to take the images OR path to the image to be processed and the directory where to save the processed ones."
            | Filter _ -> "Specify filter to apply."
            | Rotate _ -> "Specify the direction of rotation."
            | Method _ -> "Specify the processing method."
