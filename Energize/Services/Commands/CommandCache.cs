using System;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;

namespace Energize.Services.Commands
{    
    public class CommandCache
    {
        private SocketMessage _LastMessage;
        private string _LastPictureURL;
        private SocketMessage _LastDeletedMessage;
        private List<SocketMessage> _LastMessages;

        public SocketMessage LastMessage { get => this._LastMessage; set => this._LastMessage = value; }
        public string LastPictureURL { get => this._LastPictureURL; set => this._LastPictureURL = value; }
        public SocketMessage LastDeletedMessage { get => this._LastDeletedMessage; set => this._LastDeletedMessage = value; }
        public List<SocketMessage> LastMessages { get => this._LastMessages; set => this._LastMessages = value; }
    }
}