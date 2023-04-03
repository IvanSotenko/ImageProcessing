module ImageProcessing.Tests.Tests

open Expecto
open ImageProcessing

[<Tests>]
let rotateTests =
    testList
        "Tests for rotate90Left and rotate90Right functions"
        [ testProperty "Turning image four times by 90 degrees is an identical transformation (Left)."
          <| fun (img: byte[,]) ->

              let mutable rotating = img

              for i in 1..4 do
                  rotating <- ImageProcessing.rotate90Left rotating

              Expect.equal rotating img "The results were different"


          testProperty "Turning image four times by 90 degrees is an identical transformation (Right)."
          <| fun (img: byte[,]) ->

              let mutable rotating = img

              for i in 1..4 do
                  rotating <- ImageProcessing.rotate90Right rotating

              Expect.equal rotating img "The results were different" ]
