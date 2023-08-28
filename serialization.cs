using System.Text.Json;

namespace Bakalar {
    public static class serialization {
        public const string serialization_file_name = "data.json";

        private static JsonSerializerOptions json_options = new JsonSerializerOptions() { IncludeFields = true, WriteIndented = true };

        public static bool check_serialization() {
            return File.Exists(serialization_file_name);
        }

        public async static Task serialize_data(List<repo_info> repos, string file_name) {
            Console.WriteLine("Serializing to " + file_name);

            var json = JsonSerializer.Serialize(repos, json_options);
            File.WriteAllText(file_name, json);

            using FileStream file_stream = File.Create(file_name);
            await JsonSerializer.SerializeAsync(file_stream, repos, json_options);
            await file_stream.DisposeAsync();

            Console.WriteLine("Serialized");
        }

        public async static Task<List<repo_info>> deserialize_data(string file_name) {
            Console.WriteLine("Deserializing from " + file_name);

            using FileStream file_stream = File.OpenRead(file_name);
            List<repo_info>? obj = await JsonSerializer.DeserializeAsync<List<repo_info>>(file_stream, json_options);

            foreach (var repo in obj)
            {
               
                repo.issues.ForEach(i => i.reg());
            } 

            if (obj != null) {
                Console.WriteLine("Deserialized");
                return obj;
            } else throw new Exception("Failed to deserialize file");
        }
    }
}
