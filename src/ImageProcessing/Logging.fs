module ImageProcessing.Logging

type Logger() =

    let agent =
        MailboxProcessor.Start(fun inbox ->

            let rec messageLoop () =
                async {
                    let! msg = inbox.Receive()
                    printfn $"{msg}"
                    return! messageLoop ()
                }

            messageLoop ())

    member this.Log(msg: string) = agent.Post msg
