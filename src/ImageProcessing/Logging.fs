module ImageProcessing.Logging

type LogMessage =
    | Message of string
    | Off of AsyncReplyChannel<unit>


type Logger() =

    let agent =
        MailboxProcessor.Start(fun inbox ->

            let mutable isAgentRunning = true

            async {
                while isAgentRunning do
                    let! msg = inbox.Receive()

                    match msg with
                    | Message msg -> printfn $"{msg}"

                    | Off ch ->
                        isAgentRunning <- false
                        ch.Reply()

            })

    member this.Log(msg: string) = agent.Post(Message msg)
    member this.Terminate() = agent.PostAndReply(Off)

let logger = Logger()
