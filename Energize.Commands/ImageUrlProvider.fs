namespace Energize.Commands

module ImageUrlProvider =
    open Discord
    open System.Text.RegularExpressions

    let getLastAttachmentUrl (msg : IMessage) = 
        let imgAttachs = msg.Attachments |> Seq.filter (fun attach -> attach.Width.HasValue) 
        match imgAttachs |> Seq.tryLast with
        | Some attach ->
            Some attach.ProxyUrl
        | None -> 
            None

    let getLastEmbedUrl (msg : IMessage) =
        let imgEmbeds = msg.Embeds |> Seq.filter (fun embed -> embed.Image.HasValue)
        match imgEmbeds |> Seq.tryLast with
        | Some embed ->
            Some embed.Image.Value.ProxyUrl
        | None ->
            None

    let getLastParsedImgUrl (msg : IMessage) =
        let imgPattern = "(https?:\/\/.+\.(jpg|png|gif))"
        match Regex.Matches(msg.Content, imgPattern) |> Seq.tryLast with
        | Some img ->
            Some img.Value
        | None ->
            None

    let getLastParsedGiphyUrl (msg : IMessage) = 
        let giphyPattern = "https:\/\/giphy\.com\/gifs\/(.+-)?([A-Za-z0-9]+)\s?"
        match Regex.Matches(msg.Content, giphyPattern) |> Seq.tryLast with
        | Some gif ->
            Some gif.Value
        | None ->
            None

    let getLastImgUrl (msg : IMessage) : string option = 
        let lastUrl =
            [
                (getLastAttachmentUrl msg);
                (getLastEmbedUrl msg);
                (getLastParsedImgUrl msg);
                (getLastParsedGiphyUrl msg);
            ] |> List.tryLast
        match lastUrl with
        | Some url ->
            url
        | None ->
            None

