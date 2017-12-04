using Discord.WebSocket;

namespace EBot.Drama
{
    class DramaCachedMessage
    {
        private SocketMessage _Message;
        private DramaInfo _Info;

        public SocketMessage Message { get => this._Message; set => this._Message = value; }
        public DramaInfo Info { get => this._Info; set => this._Info = value; }
    }
}
