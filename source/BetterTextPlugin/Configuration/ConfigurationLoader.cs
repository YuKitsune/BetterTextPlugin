using System.IO;
using BetterTextPlugin.Composer;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BetterTextPlugin.Configuration;

public interface IConfigurationLoader
{
    PluginConfiguration Load();
}

public class ConfigurationLoader : IConfigurationLoader
{
    static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(PascalCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public PluginConfiguration Load()
    {
        var path = ConfigurationLocator.LocateConfigurationFile();
        var yaml = File.ReadAllText(path);
        return Deserializer.Deserialize<PluginConfiguration>(yaml);
    }
}

public static class PluginConfigurationExtensions
{
    public static IReadOnlyList<Template> ToTemplates(this PluginConfiguration config) =>
        config.Templates
            .Select(t => new Template(t.Content, t.Tags))
            .ToList();
}
