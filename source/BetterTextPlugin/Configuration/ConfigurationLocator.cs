using System.IO;
using System.Reflection;

namespace BetterTextPlugin.Configuration;

public static class ConfigurationLocator
{
    public static string LocateConfigurationFile()
    {
        const string configFileName = "BetterTextPlugin.yaml";

        var searchDirectories = new List<string>();

        // Search the profile first
        if (ProfileDirectoryResolver.TryGetProfileDirectory(out var profileDirectory) && profileDirectory is not null)
        {
            searchDirectories.AddRange([
                Path.Combine(profileDirectory.FullName, "Plugins", "Configs", "BetterTextPlugin"),
                Path.Combine(profileDirectory.FullName, "Plugins", "Configs"),
                Path.Combine(profileDirectory.FullName, "Plugins"),
                profileDirectory.FullName
            ]);
        }

        // Search the assembly directory last
        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        searchDirectories.Add(assemblyDirectory);

        foreach (var searchDirectory in searchDirectories)
        {
            var filePath = Path.Combine(searchDirectory, configFileName);
            if (!File.Exists(filePath))
                continue;

            return filePath;
        }

        throw new Exception($"Unable to locate {configFileName}");
    }
}
