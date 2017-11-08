using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace EBot.MemoryStream
{
    public class EBotMemoryStream
    {
        public delegate Task RequestCallback(StreamWriter writer, StreamReader reader);
        private static EBotClient _Client;
        private static Dictionary<string, RequestCallback> _Callbacks = new Dictionary<string, RequestCallback>
        {
            ["INFO_REQUEST"] = async (StreamWriter writer, StreamReader reader) =>
            {
                _Client.Log.Nice("API", ConsoleColor.Yellow, "Information request");
                EBotInfo info = await GetClientInfo();

                writer.Write(info.ID            + writer.NewLine);
                writer.Write(info.UserAmount    + writer.NewLine);
                writer.Write(info.GuildAmount   + writer.NewLine);
                writer.Write(info.CommandAmount + writer.NewLine);
                writer.Write(info.Status        + writer.NewLine);
                writer.Write(info.Prefix        + writer.NewLine);
                writer.Write(info.Owner         + writer.NewLine);
                writer.Write(info.OwnerStatus   + writer.NewLine);
                writer.Write(info.Name          + writer.NewLine);
                writer.Write(info.Avatar        + writer.NewLine);
                writer.Write(info.OwnerAvatar   + writer.NewLine);
            },
            ["STATE_REQUEST"] = async (StreamWriter writer,StreamReader reader) =>
            {
                _Client.Log.Nice("API", ConsoleColor.Yellow, "State request");

                writer.Write("1");
            },
            ["KILL_REQUEST"] = async (StreamWriter writer, StreamReader reader) =>
            {
                await _Client.Handler.EmbedReply.Disconnect(_Client.Discord);

                throw new Exception("EXTERNAL_KILL_REQUEST");
            }
        };

        public static void Initialize(EBotClient client)
        {
            _Client = client;

            Task threadpipe = new Task(async () =>
            {
                using (NamedPipeServerStream server = new NamedPipeServerStream("EBot"))
                {
                    server.WaitForConnection();

                    using (StreamReader reader = new StreamReader(server))
                    using (StreamWriter writer = new StreamWriter(server))
                    {
                        while (true)
                        {
                            string line = reader.ReadLine();
                            if(_Callbacks.TryGetValue(line, out RequestCallback callback))
                            {
                                await callback(writer,reader);
                            }

                            writer.Flush();
                        }
                    }
                }
            });

            threadpipe.Start();
        }

        public static async Task<EBotInfo> GetClientInfo()
        {
            EBotInfo info = await new EBotInfo(_Client).Initialize();
            return info;
        }
    }
}
