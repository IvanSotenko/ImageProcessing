namespace ImageProcessing

open Brahma.FSharp
open ImageProcessing


module Main =
    // let pathToExamples = "/home/gsv/Projects/TestProj2020/src/ImgProcessing/Examples"
    // let inputFolder = System.IO.Path.Combine(pathToExamples, "input")
    //
    // let demoFile =
    //     System.IO.Path.Combine(inputFolder, "armin-djuhic-ohc29QXbS-s-unsplash.jpg")

    [<EntryPoint>]
    let main (argv: string array) =

        (*
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
        *)

        let pathToExamples = "C:\Users\ivans\Documents\spbsu\pics\input"
        let pathOut = "C:\Users\ivans\Documents\spbsu\pics\output"


        Streaming.processAllFiles pathToExamples pathOut [ applyFilter2 edgesKernel; applyFilter2 gaussianBlurKernel ]

        // let path = "C:\Users\ivans\Documents\spbsu\pics\input\dodge.jpg"
        //
        // let dodge = loadAsImage path
        //
        // let a = imageToArr2D dodge

        // let ex = [(0, 0); (0, 1); (0, 2); (0, 3); (1, 0); (1, 1); (1, 2); (1, 3); (2, 0); (2, 1); (2, 2); (2, 3)]
        // let arr = [|0; 1; 2; 3; 4; 5; 6; 7; 8; 9; 10; 11|]
        // let height = 3
        // let width = 4
        //
        // let a = Array2D.init height width (fun i j -> arr[i*width + j])
        //
        //
        // printfn "%A" a
        0
