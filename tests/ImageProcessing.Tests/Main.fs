namespace ImageProcessing.Tests
open AgentProcessingTests


module ExpectoTemplate =

    open Expecto

    [<EntryPoint>]
    let main argv =
        runTestsInAssembly defaultConfig argv |> ignore
        generator.EOS()
        0
