module ImageProcessing.Tests.AgentProcessingTests

open Expecto
open ImageProcessing

open ImageProcessing
open Streaming
open Generators

type Msg =
    | ReqFolder of AsyncReplyChannel<string>
    | DeleteFolder of path: string
    | EOS of AsyncReplyChannel<unit>


let actualOutputFolderName = "actualOutput"
let expectedOutputFolderName = "expectedOutput"
let topFolderName = "output"
    
/// <summary>
///     Deals with creating numbered folders and deleting them in a given directory
///     to avoid problems with accessing files during parallel testing
/// </summary>
/// <param name="path">Directory where output/testOutputN folders will be created.</param>
type FolderGenerator(path) =
    
    let agent =
        MailboxProcessor.Start(fun inbox ->

            let rec loop n =
                async {
                    let! msg = inbox.Receive()
                    
                    match msg with
                    | ReqFolder ch ->
                        let mainFolderName = $"testOutput{n}"
                        let pathToMainFolder = System.IO.Path.Join([|path; topFolderName; mainFolderName|])
                        let pathToActual = System.IO.Path.Join([|pathToMainFolder; actualOutputFolderName|])
                        let pathToExpected = System.IO.Path.Join([|pathToMainFolder; expectedOutputFolderName|])
                        
                        System.IO.Directory.CreateDirectory(pathToActual) |> ignore
                        System.IO.Directory.CreateDirectory(pathToExpected) |> ignore
                        ch.Reply(pathToMainFolder)
                        return! loop (n + 1)
                    
                    | DeleteFolder path ->
                        System.IO.Directory.Delete(path, true)
                        return! loop n
                        
                    | EOS ch ->
                        let pathToDelete = System.IO.Path.Join([|path; topFolderName|])
                        System.IO.Directory.Delete(pathToDelete, true)
                        ch.Reply()
                        
                }
            loop 1)

    member this.GetFolder() = agent.PostAndReply(ReqFolder)
    member this.CleanUp(path) = agent.Post(DeleteFolder path)
    member this.EOS() = agent.PostAndReply(EOS)
    

let testFolder = "..\..\..\..\..\..\ImageProcessing\\testImages"
let generator = FolderGenerator(testFolder)
let testInputFolder = System.IO.Path.Join([|testFolder; "input"|])


[<Tests>]
let agentProcessingTests =
    testList
        "Tests of the logic of functions for image processing using agents"
        [ testPropertyWithConfig ioConfig "processImagesUsingAgents is processImagesSequentially. Without any args"
          <| fun (applicators: Applicators) ->
              
              let args = []
              
              let outputFolder = generator.GetFolder()
              let actualOutputFolder = System.IO.Path.Join([|outputFolder; actualOutputFolderName|])
              let expectedOutputFolder = System.IO.Path.Join([|outputFolder; expectedOutputFolderName|])
              
              processImagesSequentially testInputFolder expectedOutputFolder applicators.Get |> ignore
              processImagesUsingAgents testInputFolder actualOutputFolder applicators.Get args |> ignore
              
              let actualResult = loadImages actualOutputFolder
              let expectedResult = loadImages expectedOutputFolder
              
              generator.CleanUp(outputFolder)
              
              Expect.equal actualResult expectedResult "The results were different"
              
              
          testPropertyWithConfig ioConfig "processImagesUsingAgents is processImagesSequentially. Args: ReadFirst"
          <| fun (applicators: Applicators) ->
              
              let args = [ ReadFirst ]
              
              let outputFolder = generator.GetFolder()
              let actualOutputFolder = System.IO.Path.Join([|outputFolder; actualOutputFolderName|])
              let expectedOutputFolder = System.IO.Path.Join([|outputFolder; expectedOutputFolderName|])
              
              processImagesSequentially testInputFolder expectedOutputFolder applicators.Get |> ignore
              processImagesUsingAgents testInputFolder actualOutputFolder applicators.Get args |> ignore
              
              let actualResult = loadImages actualOutputFolder
              let expectedResult = loadImages expectedOutputFolder
              
              generator.CleanUp(outputFolder)

              Expect.equal actualResult expectedResult "The results were different"
              
              
          testPropertyWithConfig ioConfig "processImagesUsingAgents is processImagesSequentially. Args: Chain"
          <| fun (applicators: Applicators) ->
              
              let args = [ Chain ]
              
              let outputFolder = generator.GetFolder()
              let actualOutputFolder = System.IO.Path.Join([|outputFolder; actualOutputFolderName|])
              let expectedOutputFolder = System.IO.Path.Join([|outputFolder; expectedOutputFolderName|])
              
              processImagesSequentially testInputFolder expectedOutputFolder applicators.Get |> ignore
              processImagesUsingAgents testInputFolder actualOutputFolder applicators.Get args |> ignore
              
              let actualResult = loadImages actualOutputFolder
              let expectedResult = loadImages expectedOutputFolder
              
              generator.CleanUp(outputFolder)
              
              Expect.equal actualResult expectedResult "The results were different"
              
              
          testPropertyWithConfig ioConfig "processImagesUsingAgents is processImagesSequentially. Args: Chain and ReadFirst"
          <| fun (applicators: Applicators) ->
              
              let args = [ ReadFirst; Chain ]
              
              let outputFolder = generator.GetFolder()
              let actualOutputFolder = System.IO.Path.Join([|outputFolder; actualOutputFolderName|])
              let expectedOutputFolder = System.IO.Path.Join([|outputFolder; expectedOutputFolderName|])
              
              processImagesSequentially testInputFolder expectedOutputFolder applicators.Get |> ignore
              processImagesUsingAgents testInputFolder actualOutputFolder applicators.Get args |> ignore
              
              let actualResult = loadImages actualOutputFolder
              let expectedResult = loadImages expectedOutputFolder
              
              generator.CleanUp(outputFolder)

              Expect.equal actualResult expectedResult "The results were different"
              
              
          testPropertyWithConfig ioConfig "processImagesParallelUsingAgents is processImagesSequentially"
          <| fun (applicators: Applicators) ->
              
              let outputFolder = generator.GetFolder()
              let actualOutputFolder = System.IO.Path.Join([|outputFolder; actualOutputFolderName|])
              let expectedOutputFolder = System.IO.Path.Join([|outputFolder; expectedOutputFolderName|])
              
              processImagesSequentially testInputFolder expectedOutputFolder applicators.Get |> ignore
              processImagesParallelUsingAgents testInputFolder actualOutputFolder applicators.Get |> ignore
              
              let actualResult = loadImages actualOutputFolder
              let expectedResult = loadImages expectedOutputFolder
              
              generator.CleanUp(outputFolder)

              Expect.equal actualResult expectedResult "The results were different" ]
