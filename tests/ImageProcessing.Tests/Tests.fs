module ImageProcessing.Tests.Tests

open Expecto
open ImageProcessing

open Generators
open ImageProcessing.ImageProcessing

[<Tests>]
let rotationTests =
    testList
        "Tests for rotate90 function"
        [ testProperty "Turning image four times by 90 degrees is an identical transformation (Left)"
          <| fun (img: byte[,]) ->

              let mutable rotating = img

              for i in 1..4 do
                  rotating <- rotate90 rotating false

              Expect.equal rotating img "The results were different"


          testProperty "Turning image four times by 90 degrees is an identical transformation (Right)"
          <| fun (img: byte[,]) ->

              let mutable rotating = img

              for i in 1..4 do
                  rotating <- rotate90 rotating true

              Expect.equal rotating img "The results were different" ]


[<Tests>]
let filtersTests =
    testList
        "Tests for filter applicators"
        [ testPropertyWithConfig config "Applying the filter does not change the size of the image"
          <| fun (kernel: FilterKernel) (img: byte[,]) ->
              
              let expectedResult = (Array2D.length1 img, Array2D.length2 img)
              let processedImg = applyFilter kernel.Get img
              let actualResult = (Array2D.length1 processedImg, Array2D.length2 processedImg)

              Expect.equal actualResult expectedResult "The results were different"
          
          
          testProperty "If filter kernel is empty an exception is thrown"
           <| fun (img: byte[,]) ->
               let filter: float32[][] = [||]
               Expect.throws (fun _ -> applyFilter filter img |> ignore) "The filter kernel is empty"
               
               
          testPropertyWithConfig config "If the filter kernel is not a square two-dimensional array an exception is thrown"
           <| fun (filter: NonSquare2DArray<float32>) (img: byte[,]) ->
               Expect.throws (fun _ -> applyFilter filter.Get img |> ignore) "The height and width of the filter kernel do not match"
               
               
          testPropertyWithConfig config "If the filter kernel is square two-dimensional array of even length an exception is thrown"
           <| fun (filter: EvenSquare2DArray<float32>) (img: byte[,]) ->
               Expect.throws (fun _ -> applyFilter filter.Get img |> ignore) "The height and width of the filter kernel is even number"
               
          ]
