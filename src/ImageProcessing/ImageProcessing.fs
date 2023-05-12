module ImageProcessing.ImageProcessing


open System
open Brahma.FSharp
open SixLabors.ImageSharp
open SixLabors.ImageSharp.PixelFormats

[<Struct>]
type Image =
    val Data: array<byte>
    val Width: int
    val Height: int
    val Name: string

    new(data, height, width, name) =
        { Data = data
          Width = width
          Height = height
          Name = name }

type Direction =
    | Left
    | Right


type FilterKernel =
    | GaussianBlur
    | Edges
    | MotionBlur
    | YSobel
    | Emboss
    | OutLine

    member this.Kernel =
        match this with

        | GaussianBlur ->
            [| [| 1; 4; 6; 4; 1 |]
               [| 4; 16; 24; 16; 4 |]
               [| 6; 24; 36; 24; 6 |]
               [| 4; 16; 24; 16; 4 |]
               [| 1; 4; 6; 4; 1 |] |]
            |> Array.map (Array.map (fun x -> (float32 x) / 256.0f))

        | Edges ->
            [| [| 0; 0; -1; 0; 0 |]
               [| 0; 0; -1; 0; 0 |]
               [| 0; 0; 2; 0; 0 |]
               [| 0; 0; 0; 0; 0 |]
               [| 0; 0; 0; 0; 0 |] |]
            |> Array.map (Array.map float32)

        | MotionBlur ->
            (Array.init 9 (fun i -> Array.init 9 (fun j -> if i = j then 0.1 else 0.)))
            |> Array.map (Array.map float32)

        | YSobel ->
            [| [| -1; 0; 1 |]; [| -2; 0; 2 |]; [| -1; 0; 1 |] |]
            |> Array.map (Array.map (fun x -> (float32 x) / 6f))

        | Emboss ->
            [| [| -2; -1; 0 |]; [| -1; 1; 1 |]; [| 0; 1; 2 |] |]
            |> Array.map (Array.map float32)

        | OutLine ->
            [| [| -1; -1; -1 |]; [| -1; 8; -1 |]; [| -1; -1; -1 |] |]
            |> Array.map (Array.map (fun x -> (float32 x) / 9f))


let loadImage (file: string) =
    let img = Image.Load<L8> file

    let buf = Array.zeroCreate<byte> (img.Width * img.Height)

    img.CopyPixelDataTo(Span<byte> buf)
    Image(buf, img.Height, img.Width, System.IO.Path.GetFileName file)

let saveImage file (image: Image) =
    let img = Image.LoadPixelData<L8>(image.Data, image.Width, image.Height)
    img.Save file

let allowedImageFormats =
    Set.ofArray [| ".gif"; ".png"; ".webp"; ".pbm"; ".tiff"; ".bmp"; ".jpeg"; ".jpg"; ".tga" |]


let getImagePaths dir =
    if System.IO.File.Exists dir then
        Array.singleton dir
    else
        let files = System.IO.Directory.GetFiles dir

        files
        |> Array.filter (fun file -> allowedImageFormats.Contains(System.IO.Path.GetExtension file))

let loadImages dir =
    let imgFiles = getImagePaths dir
    Array.map loadImage imgFiles


let saveImages directory (images: Image[]) =
    let save (image: Image) =
        let newName = "proc_" + image.Name
        saveImage (System.IO.Path.Combine(directory, newName)) image

    Array.iter save images


let checkKernelFormat (kernel: float32[][]) =
    if (Array.isEmpty kernel) then
        Some(ArgumentException("The filter kernel is empty"))
    else
        let isSquare =
            Array.fold (fun b xs -> b && ((Array.length xs) = kernel.Length)) true kernel

        if not isSquare then
            Some(ArgumentException("The height and width of the filter kernel do not match"))
        elif (kernel.Length % 2) = 0 then
            Some(ArgumentException("The height and width of the filter kernel is even number"))
        else
            None


let applyFilter (filter: float32[][]) (img: Image) =

    match checkKernelFormat filter with
    | Some exp -> raise exp
    | None -> ()

    let filterD = (Array.length filter) / 2
    let filter = Array.concat filter

    let processPixel pi =
        let px = pi / img.Width
        let py = pi % img.Width

        let dataToHandle =
            [| for i in px - filterD .. px + filterD do
                   for j in py - filterD .. py + filterD do
                       if i < 0 || i >= img.Height || j < 0 || j >= img.Width then
                           float32 img.Data[pi]
                       else
                           float32 img.Data[i * img.Width + j] |]

        Array.fold2 (fun s x y -> s + x * y) 0.0f filter dataToHandle

    Image((Array.mapi (fun i _ -> byte (processPixel i)) img.Data), img.Height, img.Width, img.Name)


let rotate90 (direction: Direction) (img: Image) =

    let zeroArr = Array.zeroCreate img.Data.Length

    let mapping i _ =
        match direction with
        | Right -> img.Data[(img.Height - (i % img.Height) - 1) * img.Width + (i / img.Height)]
        | Left -> img.Data[(i % img.Height) * img.Width + (img.Width - (i / img.Height) - 1)]

    Image((Array.mapi mapping zeroArr), img.Width, img.Height, img.Name)


let processImagesSequentially pathIn pathOut applicators =

    let images = loadImages pathIn

    let applyAll (image: Image) =
        Logging.logger.Log($"Main thread: processing image {image.Name}")
        let result = List.fold (fun img applicator -> applicator img) image applicators
        Logging.logger.Log($"Main thread: processing of the image {image.Name} is completed")
        result

    let processedImages = images |> Array.map applyAll

    saveImages pathOut processedImages
    processedImages.Length


let applyFilterGPUKernel (clContext: ClContext) localWorkSize =

    let kernel =
        <@
            fun (r: Range1D) (img: ClArray<_>) imgW imgH (filter: ClArray<_>) filterD (result: ClArray<_>) ->
                let p = r.GlobalID0
                let pw = p % imgW
                let ph = p / imgW
                let mutable res = 0.0f

                for i in ph - filterD .. ph + filterD do
                    for j in pw - filterD .. pw + filterD do
                        let mutable d = 0uy

                        if i < 0 || i >= imgH || j < 0 || j >= imgW then
                            d <- img[p]
                        else
                            d <- img[i * imgW + j]

                        let f = filter[(i - ph + filterD) * (2 * filterD + 1) + (j - pw + filterD)]
                        res <- res + (float32 d) * f

                result[p] <- byte (int res)
        @>

    let kernel = clContext.Compile kernel

    fun (commandQueue: MailboxProcessor<_>) (filter: ClArray<float32>) filterD (img: ClArray<byte>) imgH imgW (result: ClArray<_>) ->

        let ndRange = Range1D.CreateValid(imgH * imgW, localWorkSize)

        let kernel = kernel.GetKernel()
        commandQueue.Post(Msg.MsgSetArguments(fun () -> kernel.KernelFunc ndRange img imgW imgH filter filterD result))
        commandQueue.Post(Msg.CreateRunMsg<_, _> kernel)
        result

let applyFiltersGPU (clContext: ClContext) localWorkSize =
    let kernel = applyFilterGPUKernel clContext localWorkSize
    let queue = clContext.QueueProvider.CreateQueue()

    fun (filters: list<float32[][]>) (img: Image) ->

        let mutable input =
            clContext.CreateClArray<_>(img.Data, HostAccessMode.NotAccessible)

        let mutable output =
            clContext.CreateClArray(
                img.Data.Length,
                HostAccessMode.NotAccessible,
                allocationMode = AllocationMode.Default
            )

        for filter in filters do
            let filter = Array.concat filter

            let filterD = (Array.length filter) / 2

            let clFilter =
                clContext.CreateClArray<_>(filter, HostAccessMode.NotAccessible, DeviceAccessMode.ReadOnly)

            let oldInput = input
            input <- kernel queue clFilter filterD input img.Height img.Width output
            output <- oldInput
            queue.Post(Msg.CreateFreeMsg clFilter)

        let result = Array.zeroCreate (img.Height * img.Width)

        let result = queue.PostAndReply(fun ch -> Msg.CreateToHostMsg(input, result, ch))
        queue.Post(Msg.CreateFreeMsg input)
        queue.Post(Msg.CreateFreeMsg output)
        Image(result, img.Width, img.Height, img.Name)
