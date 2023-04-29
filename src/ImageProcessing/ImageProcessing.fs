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

    new(data, width, height, name) =
        { Data = data
          Width = width
          Height = height
          Name = name }

let loadAs2DArray (file: string) =
    let img = Image.Load<L8> file
    let res = Array2D.zeroCreate img.Height img.Width

    for i in 0 .. img.Width - 1 do
        for j in 0 .. img.Height - 1 do
            res[j, i] <- img.Item(i, j).PackedValue

    res

let loadAsImage (file: string) =
    let img = Image.Load<L8> file

    let buf = Array.zeroCreate<byte> (img.Width * img.Height)

    img.CopyPixelDataTo(Span<byte> buf)
    Image(buf, img.Width, img.Height, System.IO.Path.GetFileName file)

let save2DByteArrayAsImage file (imageData: byte[,]) =
    let h = imageData.GetLength 0
    let w = imageData.GetLength 1

    let flat2Darray array2D =
        seq {
            for x in [ 0 .. (Array2D.length1 array2D) - 1 ] do
                for y in [ 0 .. (Array2D.length2 array2D) - 1 ] do
                    yield array2D[x, y]
        }
        |> Array.ofSeq

    let img = Image.LoadPixelData<L8>(flat2Darray imageData, w, h)
    img.Save file

let saveImage (image: Image) file =
    let img = Image.LoadPixelData<L8>(image.Data, image.Width, image.Height)
    img.Save file

let gaussianBlurKernel =
    [| [| 1; 4; 6; 4; 1 |]
       [| 4; 16; 24; 16; 4 |]
       [| 6; 24; 36; 24; 6 |]
       [| 4; 16; 24; 16; 4 |]
       [| 1; 4; 6; 4; 1 |] |]
    |> Array.map (Array.map (fun x -> (float32 x) / 256.0f))

let edgesKernel =
    [| [| 0; 0; -1; 0; 0 |]
       [| 0; 0; -1; 0; 0 |]
       [| 0; 0; 2; 0; 0 |]
       [| 0; 0; 0; 0; 0 |]
       [| 0; 0; 0; 0; 0 |] |]
    |> Array.map (Array.map float32)

let motionBlurKernel =
    (Array.init 9 (fun i -> Array.init 10 (fun j -> if i = j then 0.1 else 0.)))
    |> Array.map (Array.map float32)

let ySobelKernel =
    [| [| -1; 0; 1 |]; [| -2; 0; 2 |]; [| -1; 0; 1 |] |]
    |> Array.map (Array.map (fun x -> (float32 x) / 6f))


let embossKernel =
    [| [| -2; -1; 0 |]; [| -1; 1; 1 |]; [| 0; 1; 2 |] |]
    |> Array.map (Array.map float32)


let outlineKernel =
    [| [| -1; -1; -1 |]; [| -1; 8; -1 |]; [| -1; -1; -1 |] |]
    |> Array.map (Array.map (fun x -> (float32 x) / 9f))


let checkKernelFormat (kernel: float32[][]) =
    if (Array.isEmpty kernel) then
        Some (ArgumentException("The filter kernel is empty"))
    else
        let isSquare = Array.fold (fun b xs -> b && ((Array.length xs) = kernel.Length)) true kernel
        
        if not isSquare then
            Some (ArgumentException("The height and width of the filter kernel do not match"))
        elif (kernel.Length % 2) = 0 then
            Some (ArgumentException("The height and width of the filter kernel is even number"))
        else
            None


let applyFilter (filter: float32[][]) (img: byte[,]) =
    
    match checkKernelFormat filter with
    | Some exp -> raise exp
    | None -> ()
    
    let imgH = img.GetLength 0
    let imgW = img.GetLength 1

    let filterD = (Array.length filter) / 2

    let filter = Array.concat filter

    let processPixel px py =
        let dataToHandle =
            [| for i in px - filterD .. px + filterD do
                   for j in py - filterD .. py + filterD do
                       if i < 0 || i >= imgH || j < 0 || j >= imgW then
                           float32 img[px, py]
                       else
                           float32 img[i, j] |]

        Array.fold2 (fun s x y -> s + x * y) 0.0f filter dataToHandle

    Array2D.mapi (fun x y _ -> byte (processPixel x y)) img


let rotate90 (img: byte[,]) (clockwise: bool) =

    let imgH = img.GetLength 0
    let imgW = img.GetLength 1
    let zeroArr2d = Array2D.zeroCreate imgW imgH

    let mapping x y _ =
        if clockwise then
            img[imgH - y - 1, x]
        else
            img[y, imgW - x - 1]

    Array2D.mapi mapping zeroArr2d


let loadAs2DArrayFromDirectory directory =
    let files = System.IO.Directory.GetFiles directory
    (Array.map loadAs2DArray files), files

let save2DByteArrayAsImageMany directoryIn (names: string[]) (images: byte[,][]) =
    let save i image =
        save2DByteArrayAsImage (System.IO.Path.Combine(directoryIn, names[i])) image

    Array.iteri save images


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
