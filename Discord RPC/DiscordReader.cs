using System.IO.Pipes;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Discord {
    class DiscordReader {

        private static readonly JsonData jsonData = new();

        static async Task Main() {
            NamedPipeClientStream client = connectToDiscord();

            handshake(client);
            authorize(client);
            await oauth();
            authenticate(client);
            // get_all_messages(client);
            start_real_time_message_reader(client);
            read_messages_live(client);
        }

        private static NamedPipeClientStream connectToDiscord() {
            for (int i = 0; i < 10; i++) {
                var client = new NamedPipeClientStream($"discord-ipc-{i}");

                try {
                    client.Connect(500);
                } catch (TimeoutException) {
                    Console.WriteLine($"discord-ipc-{i} unavailable");
                    continue;
                }
                Console.WriteLine($"Successfully connected to discord-ipc-{i}");
                if (client.IsConnected) {
                    return client;
                }
            }

            throw new FileNotFoundException("Discord not open");
        }

        private static void handshake(NamedPipeClientStream client) {
            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(new {
                v = 1,
                jsonData.GetData().client_id
            });

            client.Write(new Message(0, payload).GetMessage());

            byte[] buffer = new byte[4096];
            int bytesRead = client.Read(buffer);
            buffer = buffer[..bytesRead];

            Message received = new Message(buffer);
            Console.WriteLine(received.payloadAsString());
            Console.WriteLine();
        }

        private static void authorize(NamedPipeClientStream client) {
            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(new {
                nonce = 1,
                cmd = "AUTHORIZE",
                args = new {
                    jsonData.GetData().client_id,
                    jsonData.GetData().scopes
                }
            });

            client.Write(new Message(1, payload).GetMessage());

            byte[] buffer = new byte[4096];
            int bytesRead = client.Read(buffer);
            buffer = buffer[..bytesRead];

            Message received = new Message(buffer);
            Console.WriteLine(received.payloadAsString());
            Console.WriteLine();

            JsonNode? jsonNode = JsonObject.Parse(received.payloadAsString());
            jsonData.GetData().code = jsonNode["data"]["code"].ToString();
        }

        private static async Task<JsonNode> oauth() {
            using (var oauthClient = new HttpClient()) {
                var values = new Dictionary<string, string> {
                    ["client_id"] = jsonData.GetData().client_id,
                    ["client_secret"] = jsonData.GetData().client_secret,
                    ["code"] = jsonData.GetData().code,
                    ["grant_type"] = "authorization_code"
                };
                var content = new FormUrlEncodedContent(values);

                var response = await oauthClient.PostAsync("https://discord.com/api/oauth2/token", content);
                var responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseString);
                Console.WriteLine();

                JsonNode? jsonNode = JsonObject.Parse(responseString);
                jsonData.GetData().access_token = jsonNode["access_token"].ToString();

                return jsonNode;
            }
        }

        private static void authenticate(NamedPipeClientStream client) {
            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(new {
                nonce = 1,
                cmd = "AUTHENTICATE",
                args = new {
                    jsonData.GetData().access_token
                }
            });

            client.Write(new Message(1, payload).GetMessage());

            byte[] buffer = new byte[4096];
            int bytesRead = client.Read(buffer);
            buffer = buffer[..bytesRead];

            Message received = new Message(buffer);
            Console.WriteLine(received.payloadAsString());
            Console.WriteLine();
        }

        private static void get_all_messages(NamedPipeClientStream client) {
            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(new {
                nonce = 1,
                args = new {
                    jsonData.GetData().channel_id
                },
                cmd = "GET_CHANNEL"
            });

            client.Write(new Message(1, payload).GetMessage());

            byte[] buffer = new byte[uint.MaxValue >> 8];
            int bytesRead = client.Read(buffer);
            buffer = buffer[..bytesRead];

            Message received = new Message(buffer);
            Console.WriteLine(received.payloadAsString());
            Console.WriteLine();
        }

        private static void start_real_time_message_reader(NamedPipeClientStream client) {
            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(new {
                nonce = 1,
                cmd = "SUBSCRIBE",
                evt = "MESSAGE_CREATE",
                args = new {
                    jsonData.GetData().channel_id
                }
            });

            client.Write(new Message(1, payload).GetMessage());

            byte[] buffer = new byte[4096];
            int bytesRead = client.Read(buffer);
            buffer = buffer[..bytesRead];

            Message received = new Message(buffer);
            Console.WriteLine(received.payloadAsString());
            Console.WriteLine();
        }

        private static void read_messages_live(NamedPipeClientStream client) {
            while (client.CanRead) {
                byte[] buffer = new byte[ushort.MaxValue];
                int bytesRead = client.Read(buffer);
                buffer = buffer[..bytesRead];

                Message received = new Message(buffer);
                Console.WriteLine(received.payloadAsString());
                Console.WriteLine();
            }
        }
    }
}
