namespace Energize.Commands

module Cache =
    open Discord.WebSocket

    type CommandCache = 
        {
            lastImageUrl : string option
            lastMessages : SocketMessage list
            lastMessage : SocketMessage option
            lastDeletedMessage : SocketMessage option
        }