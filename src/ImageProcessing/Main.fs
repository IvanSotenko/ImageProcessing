namespace ImageProcessing

(*open Brahma.FSharp

module Main =
    let pathToExamples = "/home/gsv/Projects/TestProj2020/src/ImgProcessing/Examples"
    let inputFolder = System.IO.Path.Combine(pathToExamples, "input")

    let demoFile =
        System.IO.Path.Combine(inputFolder, "armin-djuhic-ohc29QXbS-s-unsplash.jpg")

    [<EntryPoint>]
    let main (argv: string array) =
        let nvidiaDevice =
            ClDevice.GetAvailableDevices(platform = Platform.Nvidia)
            |> Seq.head

        let intelDevice =
            ClDevice.GetAvailableDevices(platform = Platform.Intel)
            |> Seq.head
        //ClDevice.GetFirstAppropriateDevice()
        //printfn $"Device: %A{device.Name}"

        let nvContext = ClContext(nvidiaDevice)
        let applyFiltersOnNvGPU = ImageProcessing.applyFiltersGPU nvContext 64

        let intelContext = ClContext(intelDevice)
        let applyFiltersOnIntelGPU = ImageProcessing.applyFiltersGPU intelContext 64

        let filters = [
            ImageProcessing.gaussianBlurKernel
            ImageProcessing.gaussianBlurKernel
            ImageProcessing.edgesKernel
        ]

        //let grayscaleImage = ImageProcessing.loadAs2DArray demoFile
        //let blur = ImageProcessing.applyFilter ImageProcessing.gaussianBlurKernel grayscaleImage
        //let edges = ImageProcessing.applyFilter ImageProcessing.edgesKernel blur
        //let edges =  applyFiltersGPU [ImageProcessing.gaussianBlurKernel; ImageProcessing.edgesKernel] grayscaleImage
        //ImageProcessing.save2DByteArrayAsImage edges "../../../../../out/demo_grayscale.jpg"
        let start = System.DateTime.Now

        Streaming.processAllFiles inputFolder "../../../../../out/" [
            applyFiltersOnNvGPU filters
            applyFiltersOnIntelGPU filters
        ]

        printfn
            $"TotalTime = %f{(System.DateTime.Now
                              - start)
                                 .TotalMilliseconds}"

        0
*)

module Main =
    let fileName = "egypt_cat"

    let pic = $"C:\Users\ivans\Documents\spbsu\pics\\{fileName}.jpg"

    // let relativePic = System.IO.Path.GetRelativePath (System.IO.Directory.GetCurrentDirectory(), pic)

    [<EntryPoint>]
    let main (argv: string array) =

        let filters = [
            ImageProcessing.gaussianBlurKernel
            ImageProcessing.gaussianBlurKernel
            ImageProcessing.edgesKernel
        ]

        // let img = ImageProcessing.loadAs2DArray pic
        // let img2 = ImageProcessing.applyFilter ImageProcessing.outline img
        // let img = ImageProcessing.applyFilter ImageProcessing.edgesKernel grayscaleImage
        // let edges =  applyFiltersGPU [ImageProcessing.gaussianBlurKernel; ImageProcessing.edgesKernel] grayscaleImage
        // ImageProcessing.save2DByteArrayAsImage img2 $"C:\Users\ivans\Documents\spbsu\pics\processed\\{fileName}_outline_NOTDIV.jpg"

        // let img = ImageProcessing.loadAs2DArray pic
        // let rotated = ImageProcessing.rotate90Left img
        // ImageProcessing.save2DByteArrayAsImage rotated "C:\Users\ivans\Documents\spbsu\pics\cat_rotated.jpg"

        0
