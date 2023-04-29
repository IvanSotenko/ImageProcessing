module ImageProcessing.Tests.Generators

open FsCheck
open Expecto
open System

type FilterKernel =
    | FilterKernel of float32[][]
    member xs.Get =
        match xs with
        | FilterKernel a -> a


type NonSquare2DArray<'A> =
    | NonSquare2DArray of 'A [][]
    member xs.Get =
        match xs with
        | NonSquare2DArray a -> a


type EvenSquare2DArray<'A> =
    | EvenSquare2DArray of 'A [][]
    member xs.Get =
        match xs with
        | EvenSquare2DArray a -> a
        
        
let rnd = Random()

let unmatchedInt (minValue, maxValue) targetInt =
    let values = [ for i in minValue .. maxValue do
                       if i <> targetInt then i ]
    
    values[rnd.Next(values.Length)]
    
    
type ImageTypes =
    static member Kernel() =
        let ker len =
            Arb.generate<float32>
            |> Gen.arrayOfLength len
            |> Gen.arrayOfLength len
            |> Gen.map FilterKernel
                      
        Gen.sized ker |> Gen.scaleSize (fun s -> 2*(s/15) + 1) |> Arb.fromGen
    
    
    static member NonSquareKernel() =
        let ker len1 =
            let len2 = unmatchedInt (1, 100) len1
            Arb.generate<float32>
            |> Gen.arrayOfLength len1
            |> Gen.arrayOfLength len2
            |> Gen.map NonSquare2DArray
        
        Gen.sized ker |> Arb.fromGen
    
    
    static member EvenKernel() =
        let ker len =
            Arb.generate<float32>
            |> Gen.arrayOfLength len
            |> Gen.arrayOfLength len
            |> Gen.map EvenSquare2DArray
                      
        Gen.sized ker |> Gen.scaleSize (fun s -> 2*(s/15)) |> Arb.fromGen
        
let config =
    { FsCheckConfig.defaultConfig with
        arbitrary = [ typeof<ImageTypes> ] }
