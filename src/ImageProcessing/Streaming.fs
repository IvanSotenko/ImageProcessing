module ImageProcessing.Streaming

open ImageProcessing.Logging
open ImageProcessing.ImageProcessing

let listAllFiles dir =
    let files = System.IO.Directory.GetFiles dir
    List.ofArray files

type msg =
    | Img of Image
    | EOS of AsyncReplyChannel<unit>

let imgSaver outDir =
    let outFile (imgName: string) = System.IO.Path.Combine(outDir, imgName)

    MailboxProcessor.Start(fun inbox ->
        let rec loop () = async {
            let! msg = inbox.Receive()

            match msg with
            | EOS ch ->
                logger.Log("Saver: end of stream")
                ch.Reply()
            | Img img ->
                logger.Log(sprintf "Saving image: %s" img.Name)
                saveImage img (outFile img.Name)
                logger.Log(sprintf "Saved: %s" img.Name)
                return! loop ()
        }

        loop ()
    )

let imgProcessor filterApplicator (imgSaver: MailboxProcessor<_>) =

    let filter = filterApplicator

    MailboxProcessor.Start(fun inbox ->
        let rec loop () = async {
            let! msg = inbox.Receive()

            match msg with
            | EOS ch ->
                logger.Log("Image processor is ready to finish!")
                imgSaver.PostAndReply EOS
                logger.Log("Image processor is finished!")
                ch.Reply()
            | Img img ->
                logger.Log(sprintf "Filtering: %s" img.Name)
                let filtered = filter img
                logger.Log(sprintf "Filtered: %s" img.Name)
                imgSaver.Post(Img filtered)
                return! loop ()
        }

        loop ()
    )

let processAllFiles inDir outDir filterApplicators =
    let mutable cnt = 0

    let imgProcessors =
        filterApplicators
        |> List.map (fun x ->
            let imgSaver = imgSaver outDir
            imgProcessor x imgSaver
        )
        |> Array.ofList

    printfn $"processors count: {Array.length imgProcessors}"

    let filesToProcess = listAllFiles inDir

    for file in filesToProcess do
        (imgProcessors |> Array.minBy (fun p -> p.CurrentQueueLength)).Post(Img(loadAsImage file))
        logger.Log(sprintf "queued: %s" file)

    for imgProcessor in imgProcessors do
        imgProcessor.PostAndReply EOS
