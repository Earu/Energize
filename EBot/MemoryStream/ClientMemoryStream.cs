using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace EBot.MemoryStream
{
    public class ClientMemoryStream
    {
        public delegate Task RequestCallback(StreamWriter writer, StreamReader reader);
        private static EBotClient _Client;
        private static Dictionary<string, RequestCallback> _Callbacks = new Dictionary<string, RequestCallback>
        {
            ["INFO_REQUEST"] = async (StreamWriter writer, StreamReader reader) =>
            {
                _Client.Log.Nice("API", ConsoleColor.Yellow, "Information request");
                ClientInfo info = await GetClientInfo();

                writer.WriteLine(info.ID);
                writer.WriteLine(info.UserAmount);
                writer.WriteLine(info.GuildAmount);
                writer.WriteLine(info.CommandAmount);
                writer.WriteLine(info.Status);
                writer.WriteLine(info.Prefix);
                writer.WriteLine(info.Owner);
                writer.WriteLine(info.OwnerStatus);
                writer.WriteLine(info.Name);
                writer.WriteLine(info.Avatar);
                writer.WriteLine(info.OwnerAvatar);
            },
            ["STATE_REQUEST"] = async (StreamWriter writer,StreamReader reader) =>
            {
                _Client.Log.Nice("API", ConsoleColor.Yellow, "State request");

                writer.WriteLine("1");
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
                            try
                            {
                                string line = reader.ReadLine().Trim();
                                if (_Callbacks.TryGetValue(line, out RequestCallback callback))
                                {
                                    await callback(writer, reader);
                                }

                                writer.Flush();
                            }
                            catch
                            {
                                server.Disconnect();
                                server.WaitForConnection();
                            }
                        }
                    }
                }
            });

            threadpipe.Start();
        }

        public static async Task<ClientInfo> GetClientInfo()
        {
            ClientInfo info = await new ClientInfo(_Client).Initialize();
            return info;
        }
    }
}
