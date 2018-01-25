using Energize.Services.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Energize.Services.MemoryStream
{
    [Service(Name = "MemoryStream")]
    public class ClientMemoryStream
    {
        public delegate Task RequestCallback(StreamWriter writer, StreamReader reader);
        private EnergizeClient _Client;
        private Dictionary<string, RequestCallback> _Callbacks;

        public ClientMemoryStream(EnergizeClient client)
        {
            this._Client = client;
            this._Callbacks = new Dictionary<string, RequestCallback>
            {
                ["INFO_REQUEST"] = async (StreamWriter writer, StreamReader reader) =>
                {
                    this._Client.Log.Nice("API", ConsoleColor.Yellow, "Information request");
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
                }
            };
        }

        public void Initialize()
        {
            Task threadpipe = new Task(async () =>
            {
                using (NamedPipeServerStream server = new NamedPipeServerStream("Energize"))
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

        public async Task<ClientInfo> GetClientInfo()
        {
            CommandHandler handler = ServiceManager.GetService("Commands").Instance as CommandHandler;
            ClientInfo info = await new ClientInfo(this._Client,handler).Initialize();
            return info;
        }
    }
}
