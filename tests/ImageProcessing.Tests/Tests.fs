module ImageProcessing.Tests

open Expecto
open ImageProcessing

// [<Tests>]
// let BFSTest =
//     testList
//         "Tests for BFS.BFS function"
//         [ testProperty "Random starting vertices for a single graph (football)"
//           <| fun _ ->
//
//               let verts = randomVerts 115u
//
//               let expectedResult = (naiveBFS verts testMat1).Data
//               let actualResult = (BFS verts testMat1 0).Data
//
//               Expect.equal actualResult expectedResult $"the results were different, startVerts = {verts}" ]
