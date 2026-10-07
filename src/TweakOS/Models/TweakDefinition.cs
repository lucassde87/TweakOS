namespace TweakOS.Models;

public sealed class TweakDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int Version { get; set; }
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public string Risk { get; set; } = "";
    public bool RequiresAdmin { get; set; }
    public bool Enabled { get; set; }
    public string Script { get; set; } = "";
    public string ReviewStatus { get; set; } = "";
}
