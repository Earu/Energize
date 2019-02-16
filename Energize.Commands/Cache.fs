namespace Energize.Commands

module Cache =
    open Discord.WebSocket
    open Discord

    type CommandCache = 
        {
            lastImageUrl : string option
            lastMessages : SocketMessage list
            lastMessage : SocketMessage option
            lastDeletedMessage : SocketMessage option
            guildUsers : SocketGuildUser list
        }

    let setLastMessage (cache : CommandCache) (msg : SocketMessage) : CommandCache =
        { cache with lastMessage = Some msg }

    let setLastDeletedMessage (cache: CommandCache) (msg : SocketMessage) : CommandCache =
        { cache with lastDeletedMessage = Some msg }

    let setLastImageUrl (cache : CommandCache) (url : string) : CommandCache =
        { cache with lastImageUrl = Some url }
