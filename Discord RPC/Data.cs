using System.Text.Json;

namespace Discord {

    class JsonData {
        internal class Data {
            public string client_id { get; set; }
            public string client_secret { get; set; }
            public string scopes { get; set; }
            public string oauth2code { get; set; }
            public string code { get; set; }
            public string access_token { get; set; }
            public string channel_id { get; set; }
        }

        private Data data { get; }
        private static readonly string jsonFilePath = @"..\..\..\data.json";

        internal JsonData() {
            if (!File.Exists(jsonFilePath)) {
                throw new FileNotFoundException($"No JSON file found with user data: {jsonFilePath}");
            }

            Data? jsonData = JsonSerializer.Deserialize<Data>(File.ReadAllText(jsonFilePath));
            data = jsonData is null ? new() : jsonData;
        }

        internal Data GetData() {
            return data;
        }
    }
}