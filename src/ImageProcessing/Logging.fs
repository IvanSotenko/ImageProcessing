module ImageProcessing.Logging

type LogMessage =
    | Message of string
    | Off of AsyncReplyChannel<unit>


type Logger() =

    let agent =
        MailboxProcessor.Start(fun inbox ->

            let rec messageLoop () =
                async {
                    let! msg = inbox.Receive()

                    match msg with
                    | Message msg ->
                        printfn $"{msg}"
                        return! messageLoop ()
                    | Off ch -> ch.Reply()

                }

            messageLoop ())

    member this.Log(msg: string) = agent.Post(Message msg)
    member this.Terminate() = agent.PostAndReply(Off)

let logger = Logger()
