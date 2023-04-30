module ImageProcessing.Logging

type Logger() =

    let agent =
        MailboxProcessor.Start(fun inbox ->

            let rec messageLoop () =
                async {
                    let! msg = inbox.Receive()
                    // match msg with
                    // | text, (channel: AsyncReplyChannel<obj>) ->
                    //     text |> printfn
                    //     channel.Reply()
                    printfn "%s" msg
                    return! messageLoop ()
                }

            messageLoop ())

    member this.Log(msg: string) =
        // agent.PostAndReply (fun replyChannel -> (msg, replyChannel)) |> ignore
        agent.Post msg

let logger = Logger()
