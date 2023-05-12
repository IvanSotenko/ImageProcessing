module ImageProcessing.Tests.AgentProcessingTests

open Argu
open Expecto
open ImageProcessing

open ImageProcessing
open Streaming
open Generators

type Msg =
    | ReqFolder of AsyncReplyChannel<string>
    | EOS of AsyncReplyChannel<unit>


let actualOutputFolderName = "actualOutput"
let expectedOutputFolderName = "expectedOutput"
let topFolderName = "output"
    
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
                        
                    | EOS ch ->
                        let pathToDelete = System.IO.Path.Join([|path; topFolderName|])
                        System.IO.Directory.Delete(pathToDelete, true)
                        ch.Reply()
                        
                }
            loop 1)

    member this.GetFolder() = agent.PostAndReply(ReqFolder)
    member this.EOS() = agent.PostAndReply(EOS)
    

let testFolder = "..\..\..\..\..\..\ImageProcessing\\testImages"
let generator = FolderGenerator(testFolder)
let testInputFolder = System.IO.Path.Join([|testFolder; "input"|])


[<Tests>]
let tests =
    testList
        ""
        [ testPropertyWithConfig ioConfig ""
          <| fun (applicators: Applicators) ->
              
              let outputFolder = generator.GetFolder()
              let actualOutputFolder = System.IO.Path.Join([|outputFolder; actualOutputFolderName|])
              let expectedOutputFolder = System.IO.Path.Join([|outputFolder; expectedOutputFolderName|])
              
              let args = []
              
              processImagesSequentially testInputFolder expectedOutputFolder applicators.Get |> ignore
              processImagesUsingAgents testInputFolder actualOutputFolder applicators.Get args |> ignore
              
              let actualResult = loadImages actualOutputFolder
              let expectedResult = loadImages expectedOutputFolder

              Expect.equal actualResult expectedResult "The results were different" ]
