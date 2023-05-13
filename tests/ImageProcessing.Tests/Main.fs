namespace ImageProcessing.Tests

open AgentProcessingTests
open ImageProcessing


module ExpectoTemplate =

    open Expecto

    [<EntryPoint>]
    let main argv =
        Logging.logger.Terminate()
        runTestsInAssembly defaultConfig argv |> ignore
        generator.EOS()
        0
