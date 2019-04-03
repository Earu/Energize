namespace Energize.Markov

module MarkovChain =
    open System
    open Discord.WebSocket
    open Energize.Essentials
    open Discord
    open System.IO
    open System.Text.RegularExpressions
    open System.Runtime.InteropServices

    let private path = Config.Instance.URIs.MarkovDirectory
    let private prefix = Config.Instance.Discord.Prefix
    let private separators = [| ' '; '.'; ','; '!'; '?'; ';'; '_'; '\n' |]
    let private winInvalidChars = [| '<'; '>'; '*'; '|'; ':'; '\"'; '`' |]
    let private maxDepth = 3
    let private rand = Random()

    let mutable private stateLogger : Logger option = None
    
    let Initialize (logger : Logger) =
        if not (Directory.Exists(path)) then
            Directory.CreateDirectory(path) |> ignore
        stateLogger <- Some logger

    let rec private winSanitizeData (data : string) (index : int) =
        if index > winInvalidChars.Length - 1 then
            data
        else
            winSanitizeData (data.Replace(winInvalidChars.[index].ToString(), String.Empty)) (index + 1)

    let private sanitizeData (data : string) =
        let linkFreeData =
            Regex.Replace(data.Trim().ToLower(), "https?://\S*", "LINK-REMOVED ")
                .Replace("\\", "/")
                .Replace("/", " ")
                .Replace("\"", String.Empty)
        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            winSanitizeData linkFreeData 0
        else
            linkFreeData

    let private saveWord (path : string) (text : string) =
        try
            File.AppendAllText(path, sprintf "%s\n" text)
        with _ -> 
            let msg = sprintf "Could not save markov data [ word: %s; path: %s ]" text path
            match stateLogger with
            | Some logger ->
                logger.Nice("Markov", ConsoleColor.Red, msg)
            | None -> 
                Console.WriteLine(msg)

    let Learn (data : string) =
        match data with
        | null -> ()
        | _ ->
            let words = (sanitizeData data).Split(separators) |> Seq.toList
            for i in 0..(words.Length - 1) do 
                let word = words.[i].ToLower().Trim()
                let next = if i + 1 >= words.Length then "END_SEQUENCE" else words.[i + 1].ToLower().Trim()
                
                let mutable filePath = sprintf "%s%s.markov" path word
                saveWord filePath next  
                let mutable left = sprintf "%s.markov" word
                for depth in 1..maxDepth do 
                    if i - depth >= 0 then
                        left <- sprintf "%s_%s" (words.[i - depth].Trim()) left
                        filePath <- sprintf "%s%s" path left
                        saveWord filePath next

    let rec private generateSentence (input : string list) (limit : int) (index : int) (out : string) =
        if index >= limit then
            out.TrimStart()
        else
            let filePath = sprintf "%s%s.markov" path (String.Join('_', input))
            if File.Exists(filePath) then
                let nexts = File.ReadAllLines(filePath)
                match nexts.[rand.Next(0, nexts.Length)].Trim() with
                | "END_SEQUENCE" -> out.TrimStart()
                | next ->
                    let newInput = 
                        if input.Length > maxDepth then
                            List.append input.[1..]  [ next ]
                        else
                            List.append input [ next ]
                    generateSentence newInput limit (index + 1) (sprintf "%s %s" out next)
            else
                out.TrimStart()

    let Generate (data : string) =
        let chunks = (data.ToLower().Split(separators)) |> Seq.toList
        if chunks.Length > maxDepth then
            let head = String.Join(' ', chunks, chunks.Length - maxDepth, maxDepth) 
            let input = head.Split(separators) |> Seq.toList
            generateSentence input 40 0 head
        else
            let head = String.Join(' ', chunks)
            let input = head.Split(separators) |> Seq.toList
            generateSentence input 40 0 head

    let private isChannelNsfw (chan : IChannel) =
        match chan with
        | :? IDMChannel -> true
        | _ ->
            let textChan = chan :?> ITextChannel
            textChan.IsNsfw

    let private isLearnableMessage (msg : SocketMessage) =
        not (isChannelNsfw msg.Channel) && not msg.Author.IsBot && not (msg.Content.StartsWith(prefix))

    let HandleMessageReceived (msg : SocketMessage) =
        if isLearnableMessage msg then
            Learn msg.Content