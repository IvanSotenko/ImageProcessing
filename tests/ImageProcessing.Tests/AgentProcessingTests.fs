module ImageProcessing.Tests.AgentProcessingTests

open Expecto
open ImageProcessing

open ImageProcessing
open Microsoft.FSharp.Control
open Streaming
open Generators

type Msg =
    | NewMainFolder of AsyncReplyChannel<int>
    | CreateSubFolder of num: int * name: string * AsyncReplyChannel<string>
    | DeleteDirectory of num: int
    | EOS of AsyncReplyChannel<unit>


let topFolderName = "testOutput"


/// <summary>
///     Deals with creating numbered folders for test output and deleting them
///     to avoid problems with accessing files during parallel testing
/// </summary>
type OutputFolderGenerator() =
    let curDir = System.IO.Directory.GetCurrentDirectory()
    let workingDir = System.IO.Path.Join([| curDir; topFolderName |])
    do System.IO.Directory.CreateDirectory(workingDir) |> ignore

    let getInternalFolderName n = $"output{n}"

    let agent =
        MailboxProcessor.Start(fun inbox ->

            let rec loop n =
                async {
                    let! msg = inbox.Receive()

                    match msg with
                    | NewMainFolder ch ->
                        let path = System.IO.Path.Join([| workingDir; getInternalFolderName n |])
                        path |> System.IO.Directory.CreateDirectory |> ignore
                        ch.Reply(n)
                        return! loop (n + 1)

                    | CreateSubFolder (num, name, ch) ->
                        let path = System.IO.Path.Join([| workingDir; getInternalFolderName num; name |])
                        path |> System.IO.Directory.CreateDirectory |> ignore
                        ch.Reply(path)
                        return! loop n

                    | DeleteDirectory num ->
                        let pathToDelete = System.IO.Path.Join([| workingDir; getInternalFolderName num |])
                        System.IO.Directory.Delete(pathToDelete, true)
                        return! loop n

                    | EOS ch ->
                        System.IO.Directory.Delete(workingDir, true)
                        ch.Reply()

                }

            loop 1)

    /// Creates a new main folder with a unique id
    member this.GetNewId() = agent.PostAndReply(NewMainFolder)

    /// Creates a subfolder to put the test output into
    member this.GetSubFolder(id, name) =
        agent.PostAndReply(fun ch -> CreateSubFolder(id, name, ch))

    /// Deletes the main folder
    member this.CleanUp(id) = agent.Post(DeleteDirectory id)
    member this.EOS() = agent.PostAndReply(EOS)

let generator = OutputFolderGenerator()


[<Tests>]
let agentProcessingTests =
    testList
        "Test of the logic of functions for image processing using agents"
        [ testPropertyWithConfig ioConfig ""
          <| fun (applicators: Applicators) (img: Image[]) ->
              let id = generator.GetNewId()

              let testInputFolder = generator.GetSubFolder(id, "input")
              saveImages testInputFolder img

              let expectedOutputFolder = generator.GetSubFolder(id, "expectedOutput")
              let actualOutputFolder = generator.GetSubFolder(id, "actualOutput")

              processImagesSequentially testInputFolder expectedOutputFolder applicators.Get
              |> ignore

              let expectedOutput = loadImages expectedOutputFolder

              let args = []

              processImagesUsingAgents testInputFolder actualOutputFolder applicators.Get args
              |> ignore

              let actualOutput = loadImages actualOutputFolder

              Expect.equal
                  actualOutput
                  expectedOutput
                  $"The output of processImagesUsingAgents (args = {args}) does not match results obtained by sequential processing"


              let args = [ ReadFirst ]

              processImagesUsingAgents testInputFolder actualOutputFolder applicators.Get args
              |> ignore

              let actualOutput = loadImages actualOutputFolder

              Expect.equal
                  actualOutput
                  expectedOutput
                  $"The output of processImagesUsingAgents (args = {args}) does not match results obtained by sequential processing"


              let args = [ Chain ]

              processImagesUsingAgents testInputFolder actualOutputFolder applicators.Get args
              |> ignore

              let actualOutput = loadImages actualOutputFolder

              Expect.equal
                  actualOutput
                  expectedOutput
                  $"The output of processImagesUsingAgents (args = {args}) does not match results obtained by sequential processing"



              let args = [ ReadFirst; Chain ]

              processImagesUsingAgents testInputFolder actualOutputFolder applicators.Get args
              |> ignore

              let actualOutput = loadImages actualOutputFolder

              Expect.equal
                  actualOutput
                  expectedOutput
                  $"The output of processImagesUsingAgents (args = {args}) does not match results obtained by sequential processing"


              processImagesParallelUsingAgents testInputFolder actualOutputFolder applicators.Get
              |> ignore

              let actualOutput = loadImages actualOutputFolder

              Expect.equal
                  actualOutput
                  expectedOutput
                  "The output of processImagesParallelUsingAgents does not match results obtained by sequential processing"

              generator.CleanUp(id) ]
