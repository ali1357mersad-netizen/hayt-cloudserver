namespace Hayt.Models;

public class AchievementDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "🏅";
    public int SortOrder { get; set; }
}