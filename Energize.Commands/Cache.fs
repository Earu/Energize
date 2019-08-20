namespace Energize.Commands

module Cache =
    open Discord.WebSocket

    type CommandCache = 
        {
            lastImageUrl : string option
            lastMessage : SocketMessage option
            lastDeletedMessage : SocketMessage option
        }