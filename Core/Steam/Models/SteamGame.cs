using Newtonsoft.Json;

namespace Core.Steam.Models;

public class SteamGame
{
    [JsonProperty("id")] public int GameId { get; set; }
    [JsonProperty("name")] public string? Name { get; set; }
    [JsonProperty("tiny_image")] public string? ImageUrl { get; set; }
}
