using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace MermaidDiagramApp.Services.AI
{
    public static class AiConfigStorageService
    {
        private const string FileName = "ai-config.json";

        public static async Task SaveAsync(AiConfiguration config)
        {
            var folder = ApplicationData.Current.LocalFolder;
            var file = await folder.CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting);
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            await FileIO.WriteTextAsync(file, json);
        }

        public static async Task<AiConfiguration?> LoadAsync()
        {
            try
            {
                var folder = ApplicationData.Current.LocalFolder;
                var file = await folder.GetFileAsync(FileName);
                var json = await FileIO.ReadTextAsync(file);
                return JsonSerializer.Deserialize<AiConfiguration>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
