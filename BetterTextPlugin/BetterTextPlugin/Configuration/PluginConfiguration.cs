namespace BetterTextPlugin.Configuration;

public class PluginConfiguration
{
    public int TransitionAltitude { get; set; } = 10000;
    public List<TemplateConfiguration> Templates { get; set; } = [];
}

public class TemplateConfiguration
{
    public string Content { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
}
