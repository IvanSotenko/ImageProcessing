module ImageProcessing.Tests.Generators

open FsCheck
open Expecto
open System
open ImageProcessing
open ImageProcessing
open Streaming

type Kernel =
    | Kernel of float32[][]

    member xs.Get =
        match xs with
        | Kernel a -> a

type NonSquare2DArray<'A> =
    | NonSquare2DArray of 'A[][]

    member xs.Get =
        match xs with
        | NonSquare2DArray a -> a


type EvenSquare2DArray<'A> =
    | EvenSquare2DArray of 'A[][]

    member xs.Get =
        match xs with
        | EvenSquare2DArray a -> a


type Applicators =
    | Applicators of (Image -> Image) list

    member xs.Get =
        match xs with
        | Applicators a -> a


let rnd = Random()


let Arr2DToImage (name: string) (arr: byte[,]) =
    let len1 = Array2D.length1 arr
    let len2 = Array2D.length2 arr
    let len = len1 * len2
    Image((Array.init len (fun i -> arr[i / len2, i % len2])), len1, len2, name)


/// Returns a random integer from the range (minValue, maxValue), including bounds and not including targetInt
let unmatchedInt (minValue, maxValue) targetInt =
    let values =
        [ for i in minValue..maxValue do
              if i <> targetInt then
                  i ]

    values[rnd.Next(values.Length)]


type ImageTestTypes =
    static member Kernel() =
        let ker len =
            Arb.generate<float32>
            |> Gen.arrayOfLength len
            |> Gen.arrayOfLength len
            |> Gen.map Kernel

        Gen.sized ker
        |> Gen.scaleSize (fun s -> 2 * (s / 15) + 1) // Kernel diameter must be an odd number
        // (and not really big)
        |> Arb.fromGen


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

        Gen.sized ker |> Gen.scaleSize (fun s -> 2 * (s / 15)) |> Arb.fromGen


    static member Image() =
        Arb.generate<byte>
        |> Gen.array2DOf
        |> Gen.filter (fun xs -> (Array2D.length1 xs <> 0) && (Array2D.length2 xs <> 0))
        |> Gen.map2 Arr2DToImage Arb.generate<string>
        |> Arb.fromGen


type ioTestsTypes =
    static member ApplicatorList() =
        Gen.oneof
            [ Arb.generate<Direction> |> Gen.map rotate90

              Arb.generate<FilterKernel> |> Gen.map (fun filter -> applyFilter filter.Kernel) ]

        |> Gen.nonEmptyListOf
        |> Gen.scaleSize (fun s -> s / 15) // Set the scaling of the length of the array of applicators
        |> Arb.fromGen


    static member ImageList() =
        let genImgName =
            Arb.generate<char>
            |> Gen.filter Char.IsLetter
            |> Gen.arrayOf
            |> Gen.map String
            |> Gen.filter (fun s -> s <> "")
            |> Gen.map (fun s -> s + ".jpg")

        Arb.generate<byte>
        |> Gen.array2DOf
        |> Gen.scaleSize (fun s -> s * 1000) // Set the scaling of the size of a single image
        |> Gen.filter (fun xs -> (Array2D.length1 xs <> 0) && (Array2D.length2 xs <> 0))
        |> Gen.map2 Arr2DToImage genImgName
        |> Gen.arrayOf
        |> Gen.filter (fun xs -> xs.Length <> 0)
        |> Gen.scaleSize (fun s -> s / 5) // Set the scaling of the length of the array of images
        |> Arb.fromGen


let mainConfig =
    { FsCheckConfig.defaultConfig with arbitrary = [ typeof<ImageTestTypes> ] }

let ioConfig =
    { FsCheckConfig.defaultConfig with
        arbitrary = [ typeof<ioTestsTypes> ]
        maxTest = 100 }
