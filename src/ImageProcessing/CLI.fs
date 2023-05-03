module ImageProcessing.ConsoleParsing

open Argu
open ImageProcessing
open Streaming

exception NoTransformationsException of string

type Arguments =
    | [<ExactlyOnce>] Path of pathIn: string * pathOut: string
    | Filter of name: Filters
    | Rotate of direction: Direction
    | [<Last; Unique>] Seq 
    | [<Last; Unique>] Agent of ParseResults<AgentArgs>
    | [<Last; Unique>] AgentParallel

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Path _ ->
                "Specify the directory from which to take the images OR path to the image to be processed and the directory where to save the processed ones."
            | Filter _ -> "Specify filter to apply."
            | Rotate _ -> "Specify the direction of rotation."
            | Seq _ -> "Process images sequentially."
            | Agent _ -> "Process the image using agents."
            | AgentParallel _ -> "Process images using parallelism implemented using agents."
