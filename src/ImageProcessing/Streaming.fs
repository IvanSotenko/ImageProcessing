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

type msg =
    | Img of Image
    | EOS of AsyncReplyChannel<unit>

let logger = Logger()

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
                    saveImage img (outFile img.Name)
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


let createProcessorChain (applicators: (Image -> Image)[]) outDir : MailboxProcessor<msg>[] =

    let rec loop (processors: MailboxProcessor<msg>[]) iterNum =
        match processors.Length with
        | n when n = applicators.Length -> processors
        | 0 ->
            let saver = imgSaver outDir "ImageSaver"

            let processor =
                Array.singleton (
                    imgProcessor applicators[iterNum] saver $"ImgProcessor{applicators.Length - iterNum - 1}"
                )

            loop (Array.concat [ processor; processors ]) (iterNum + 1)
        | _ ->
            let processor =
                Array.singleton (
                    imgProcessor
                        applicators[iterNum]
                        (Array.head processors)
                        $"ImgProcessor{applicators.Length - iterNum - 1}"
                )

            loop (Array.concat [ processor; processors ]) (iterNum + 1)

    loop Array.empty 0


let processImagesUsingAgents inDir outDir applicators (args: ParseResults<AgentArgs>) =

    if args.Contains Chain then

        let processors =
            createProcessorChain (applicators |> Array.ofList |> Array.rev) outDir

        let firstProcessor = Array.head processors

        if args.Contains ReadFirst then
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

        if args.Contains ReadFirst then
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
