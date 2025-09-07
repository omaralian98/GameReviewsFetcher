namespace Core.Models;

public class Game
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? ImageUrl { get; set; } = null;
}
