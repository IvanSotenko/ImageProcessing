module ImageProcessing.Streaming

open ImageProcessing.Logging
open ImageProcessing.ImageProcessing
open Argu

type AgentArgs =
    | [<Unique; CliPrefix(CliPrefix.None)>] ReadFirst
    | [<Unique; CliPrefix(CliPrefix.None)>] Chain

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | ReadFirst _ -> "Read the images before starting processing and saving."
            | Chain _ -> "Process images by creating an agent for each image applicator."


let listAllFiles dir =
    let files = System.IO.Directory.GetFiles dir
    List.ofArray files


type stringMsg =
    | Str of string
    | EOS of AsyncReplyChannel<unit>


type imageMsg =
    | Img of Image
    | EOS of AsyncReplyChannel<unit>


let imgSaver outDir name =
    let outFile (imgName: string) =
        let newName = "proc_" + imgName
        System.IO.Path.Combine(outDir, newName)

    MailboxProcessor.Start(fun inbox ->
        let rec loop () =
            async {
                let! msg = inbox.Receive()

                match msg with
                | EOS ch ->
                    logger.Log($"{name}: end of stream")
                    ch.Reply()

                | Img img ->
                    logger.Log($"{name}: saving image {img.Name}")
                    saveImage (outFile img.Name) img
                    logger.Log($"{name}: {img.Name} is saved")
                    return! loop ()
            }

        loop ())

let imgProcessor filterApplicator (nextAgent: MailboxProcessor<_>) name =

    MailboxProcessor.Start(fun inbox ->
        let rec loop () =
            async {
                let! msg = inbox.Receive()

                match msg with
                | EOS ch ->
                    nextAgent.PostAndReply EOS
                    logger.Log($"{name}: end of stream.")
                    ch.Reply()

                | Img img ->
                    logger.Log($"{name}: processing image {img.Name}.")
                    let filtered = filterApplicator img
                    logger.Log($"{name}: {img.Name} is processed.")
                    nextAgent.Post(Img filtered)
                    return! loop ()
            }

        loop ())

let readProcessAndSave outDir filterApplicator name =

    MailboxProcessor.Start(fun (inbox: MailboxProcessor<stringMsg>) ->
        let rec loop () =
            async {
                let! msg = inbox.Receive()

                match msg with
                | stringMsg.EOS ch ->
                    logger.Log($"{name}: end of stream.")
                    ch.Reply()

                | Str path ->
                    logger.Log($"{name}: File is reading - {System.IO.Path.GetFullPath path}.")
                    let img = loadImage path
                    logger.Log($"{name}: processing image {img.Name}.")
                    let processedImg = filterApplicator img
                    logger.Log($"{name}: saving image {img.Name}")
                    saveImages outDir (Array.singleton processedImg)
                    return! loop ()
            }

        loop ())


/// <summary>
///     Creates a chain of agents in the same order as the applicators are in the applicators list,
///     each agent applies one applicator to an image and passes it to the next agent. Saver at the end of the chain.
/// </summary>
/// <returns>
///     First agent from the chain.
/// </returns>
let createProcessorChain (applicators: (Image -> Image)[]) outDir : MailboxProcessor<imageMsg> =
    let lastIndex = applicators.Length - 1

    let rec loop (curProcessor: MailboxProcessor<imageMsg>) index =
        match index with
        | -1 -> curProcessor
        | _ ->
            let nextProcessor =
                imgProcessor applicators[index] curProcessor $"ImgProcessor{index + 1}"

            loop nextProcessor (index - 1)

    let saver = imgSaver outDir "ImageSaver"
    loop saver lastIndex


let processImagesUsingAgents inDir outDir applicators (args: AgentArgs list) =

    if List.contains Chain args then

        let firstProcessor = createProcessorChain (applicators |> Array.ofList) outDir

        if List.contains ReadFirst args then
            let imagesToProcess = loadImages inDir
            imagesToProcess |> Array.iter (fun img -> firstProcessor.Post(Img img))

            firstProcessor.PostAndReply(EOS)
            imagesToProcess.Length

        else
            let imagesPaths = getImagePaths inDir

            imagesPaths
            |> Array.iter (fun path ->
                let image = loadImage path
                firstProcessor.Post(Img image))

            firstProcessor.PostAndReply(EOS)
            imagesPaths.Length


    else

        let applicator image =
            List.fold (fun img applicator -> applicator img) image applicators

        let saver = imgSaver outDir "ImageSaver"
        let processor = imgProcessor applicator saver "ImageProcessor"

        if List.contains ReadFirst args then
            let imagesToProcess = loadImages inDir
            imagesToProcess |> Array.iter (fun img -> processor.Post(Img img))

            processor.PostAndReply(EOS)
            imagesToProcess.Length

        else
            let imagesPaths = getImagePaths inDir

            imagesPaths
            |> Array.iter (fun path ->
                let image = loadImage path
                processor.Post(Img image))

            processor.PostAndReply(EOS)
            imagesPaths.Length


let processImagesParallelUsingAgents inDir outDir applicators =
    let applicator image =
        List.fold (fun img applicator -> applicator img) image applicators

    let imagesPaths = getImagePaths inDir

    let processors =
        Array.init imagesPaths.Length (fun i -> readProcessAndSave outDir applicator $"ImageProcessor{i}")

    processors |> Array.iteri (fun i proc -> proc.Post(Str imagesPaths[i]))
    processors |> Array.iter (fun proc -> proc.PostAndReply(stringMsg.EOS))

    imagesPaths.Length
