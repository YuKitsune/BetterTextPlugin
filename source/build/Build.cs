using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.Versioning;
using JetBrains.Annotations;
using Fallout.Common;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.Git;
using Fallout.Common.IO;
using Fallout.Common.Tools.Docker;
using Fallout.Common.Tools.DotNet;
using Fallout.Common.Tools.Git;
using Fallout.Common.Tools.GitHub;
using Fallout.Common.Tools.GitVersion;
using Fallout.Common.Tools.ILRepack;
using Octokit;
using Serilog;

[GitHubActions(
    "build",
    GitHubActionsImage.WindowsLatest,
    OnPushBranches = ["main"],
    OnPullRequestBranches = ["main"],
    InvokedTargets = [nameof(CompilePlugin)],
    FetchDepth = 0)]
[GitHubActions(
    "release",
    GitHubActionsImage.WindowsLatest,
    OnPushTags = ["v*"],
    InvokedTargets = [nameof(Release)],
    ImportSecrets = [nameof(GitHubToken)],
    EnableGitHubToken = true,
    FetchDepth = 0,
    WritePermissions = [GitHubActionsPermissions.Contents])]
class Build : FalloutBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>();

    [Parameter("Configuration to build - Default is 'Debug' for local development, 'Release' will be used in CI")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("The name of the vatSys profile where the plugin should be (un)installed")]
    string ProfileName { get; }

    [Parameter("The GitHub token to use when creating releases")]
    [Secret]
    readonly string GitHubToken;

    [GitRepository]
    readonly GitRepository GitRepository;

    [GitVersion]
    readonly GitVersion GitVersion;

    const string ReleasePluginName = "BetterTextPlugin";
    const string DebugPluginName = "BetterTextPlugin - Debug";
    const string PluginAssemblyFileName = "BetterTextPlugin.dll";

    string PluginName => Configuration == Configuration.Debug ? DebugPluginName : ReleasePluginName;

    // Plugin paths
    AbsolutePath PluginProjectPath => RootDirectory / "source" / "BetterTextPlugin" / "BetterTextPlugin.csproj";
    AbsolutePath PluginBuildOutputDirectory => TemporaryDirectory / "build-plugin";
    AbsolutePath PluginZipPath => TemporaryDirectory / $"BetterTextPlugin.{GetSemanticVersion()}.zip";
    AbsolutePath PluginPackageDirectory => TemporaryDirectory / "package-plugin";

    // vatSys paths
    [Parameter("Path to the vatSys installation")]
    AbsolutePath VatSysPath { get; }
    AbsolutePath VatSysSetupDirectory => TemporaryDirectory / "vatsys-setup";
    AbsolutePath VatSysExePath => VatSysPath ?? VatSysSetupDirectory / "bin" / "vatSys.exe";

    [UsedImplicitly]
    public Target CheckVersion => _ => _
        .Description("Prints the semantic version that will be assigned to the binaries.")
        .Unlisted()
        .Executes(() => Log.Information("Version: {Version}", GetSemanticVersion()));

    [UsedImplicitly]
    public Target CreateRelease => _ => _
        .Description("Tags the current commit with an appropriate version, triggering the release workflow in GitHub.")
        .Requires(() => IsLocalBuild)
        .Executes(() =>
        {
            var semanticVersion = GetSemanticVersion();
            var tagName = $"v{semanticVersion}";
            Log.Information("Tagging {CommitSha} with version {Version}", GitVersion.Sha, tagName);

            GitTasks.Git($"tag {tagName}");
            GitTasks.Git($"push origin {tagName}");

            Log.Information("{Version} pushed", GitVersion.Sha, tagName);
        });

    Target DownloadVatSys => _ => _
        .Description("Downloads and extracts the vatSys executable so the plugin can be built in a headless environment (i.e. a CI system)")
        .Unlisted()
        .OnlyWhenStatic(() => VatSysPath == null && !VatSysExePath.FileExists())
        .Executes(async () =>
        {
            var vatSysSetupUrl = "https://vatsys.sawbe.com/downloads/vatSysSetup.zip";
            var zipPath = TemporaryDirectory / "vatSysSetup.zip";
            var msiPath = TemporaryDirectory / "vatSysSetup.msi";

            Log.Information("Downloading vatSys from {Url}", vatSysSetupUrl);
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(vatSysSetupUrl);
            response.EnsureSuccessStatusCode();
            await using var fileStream = File.Create(zipPath);
            await response.Content.CopyToAsync(fileStream);
            fileStream.Close();

            Log.Information("Extracting vatSysSetup.zip");
            ZipFile.ExtractToDirectory(zipPath, TemporaryDirectory, overwriteFiles: true);

            Log.Information("Extracting vatSysSetup.msi");
            VatSysSetupDirectory.CreateOrCleanDirectory();

            // Use msiexec to extract the MSI contents
            var msiExtractProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "msiexec",
                Arguments = $"/a \"{msiPath}\" /qn TARGETDIR=\"{VatSysSetupDirectory}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            if (msiExtractProcess != null)
            {
                await msiExtractProcess.WaitForExitAsync();
                if (msiExtractProcess.ExitCode != 0)
                {
                    var error = await msiExtractProcess.StandardError.ReadToEndAsync();
                    throw new Exception($"Failed to extract MSI: {error}");
                }
            }

            if (!VatSysExePath.FileExists())
                throw new Exception($"vatSys.exe not found at {VatSysExePath}");

            Log.Information("vatSys.exe extracted to {Path}", VatSysExePath);
        });

    Target CompilePlugin => _ => _
        .Description("Compiles the plugin")
        .DependsOn(DownloadVatSys)
        .Executes(() =>
        {
            var version = GetSemanticVersion();
            Log.Information(
                "Building version {Version} with configuration {Configuration} to {OutputDirectory}",
                version,
                Configuration,
                PluginBuildOutputDirectory);

            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(PluginProjectPath)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(PluginBuildOutputDirectory)
                .SetVersion(version)
                .SetAssemblyVersion(GitVersion.MajorMinorPatch)
                .SetFileVersion(GitVersion.MajorMinorPatch)
                .SetInformationalVersion(version)
                .SetProperty("VatSysPath", VatSysExePath.Parent.Parent));
        });

    Target RepackPlugin => _ => _
        .Description("Combines all plugin dependencies into a single assembly using ILRepack. This helps avoid dependency conflicts with other vatSys plugins, and vatSys itself.")
        .DependsOn(CompilePlugin)
        .Executes(() =>
        {
            var mainAssembly = PluginBuildOutputDirectory / PluginAssemblyFileName;
            var assembliesToMerge = PluginBuildOutputDirectory
                .GlobFiles("*.dll")
                .Except([mainAssembly])
                .ToArray();

            if (!mainAssembly.FileExists())
                throw new Exception($"Main assembly not found: {mainAssembly}");

            foreach (var assembly in assembliesToMerge.Where(a => !a.FileExists()))
                Log.Warning("Assembly not found (will be skipped): {Assembly}", assembly);

            var existingAssemblies = assembliesToMerge.Where(a => a.FileExists()).ToArray();
            if (existingAssemblies.Length == 0)
            {
                Log.Information("No assemblies found to repack, skipping");
                return;
            }

            var settings = new ILRepackSettings()
                .SetAssemblies([mainAssembly.ToString(), ..existingAssemblies.Select(a => a.ToString())])
                .SetInternalize(false)
                .SetParallel(true)
                .SetOutput(mainAssembly.ToString())
                .SetLib(PluginBuildOutputDirectory.ToString());  // Tell ILRepack where to find referenced assemblies

            Log.Information("Repacking {Count} assemblies into {MainAssembly}", existingAssemblies.Length, mainAssembly);
            foreach (var assembly in existingAssemblies)
                Log.Information("  - {Assembly}", assembly.Name);

            ILRepackTasks.ILRepack(settings);

            // Clean up original merged DLLs
            foreach (var assembly in existingAssemblies)
            {
                assembly.DeleteFile();
                Log.Information("Deleted {Assembly}", assembly);
            }

            Log.Information("Repack complete");
        });

    Target Uninstall => _ => _
        .Requires(() => ProfileName)
        .Description("Uninstalls the plugin from the specified profile.")
        .Executes(() =>
        {
            var pluginsDirectory = GetVatSysPluginsDirectory(ProfileName);
            AbsolutePath[] pluginDirectories =
            [
                pluginsDirectory / DebugPluginName,
                pluginsDirectory / ReleasePluginName
            ];

            foreach (var pluginDirectory in pluginDirectories)
            {
                pluginDirectory.DeleteDirectory();
                Log.Information("Plugin uninstalled from {Directory}", pluginDirectory);
            }

            var configFilePath = pluginsDirectory / "Configs" / "BetterTextPlugin" / "BetterTextPlugin.yaml";
            if (configFilePath.FileExists())
            {
                configFilePath.DeleteFile();
                Log.Information("Config file {ConfigFile} deleted", configFilePath);
            }
        });

    Target Install => _ => _
        .Description("Installs the plugin to the specified profile.")
        .Requires(() => ProfileName)
        .DependsOn(RepackPlugin)
        .DependsOn(Uninstall)
        .Executes(() =>
        {
            var pluginsDirectory = GetVatSysPluginsDirectory(ProfileName);
            Log.Information("Installing plugin to {TargetDirectory}", pluginsDirectory);

            if (!pluginsDirectory.Exists())
                pluginsDirectory.CreateDirectory();

            // Copy plugin assemblies
            var pluginDirectory = pluginsDirectory / PluginName;
            pluginDirectory.CreateOrCleanDirectory();
            foreach (var absolutePath in PluginBuildOutputDirectory.GetFiles())
            {
                absolutePath.CopyToDirectory(pluginDirectory, ExistsPolicy.MergeAndOverwrite);
            }

            // Copy config
            var configFile = RootDirectory / "BetterTextPlugin.yaml";
            var configDestinationDirectory = pluginsDirectory / "Configs" / "BetterTextPlugin";
            configDestinationDirectory.CreateOrCleanDirectory();

            configFile.CopyToDirectory(configDestinationDirectory, ExistsPolicy.MergeAndOverwrite);

            Log.Information("Plugin installed to {PluginDirectory}", pluginDirectory);
        });

    Target PackagePlugin => _ => _
        .Description("Bundles the plugin along with supplementary files into a zip archive.")
        .DependsOn(CompilePlugin)
        .DependsOn(RepackPlugin)
        .Requires(() => Configuration == Configuration.Release)
        .Executes(() =>
        {
            var dpiAwareFixScript = RootDirectory / "dpiawarefix.bat";
            var unblockDllsScript = RootDirectory / "unblock-dlls.bat";
            var configFile = RootDirectory / "BetterTextPlugin.yaml";

            PluginPackageDirectory.CreateOrCleanDirectory();

            // Copy plugin assemblies
            foreach (var absolutePath in PluginBuildOutputDirectory.GetFiles().Where(f => f.Extension != ".pdb"))
            {
                absolutePath.CopyToDirectory(PluginPackageDirectory, ExistsPolicy.MergeAndOverwrite);
            }

            // Temporary for testing - include the config with the package
            configFile.CopyToDirectory(PluginPackageDirectory, ExistsPolicy.FileOverwrite);

            dpiAwareFixScript.CopyToDirectory(PluginPackageDirectory, ExistsPolicy.FileOverwrite);
            unblockDllsScript.CopyToDirectory(PluginPackageDirectory, ExistsPolicy.FileOverwrite);

            if (PluginZipPath.FileExists())
                PluginZipPath.DeleteFile();

            Log.Information("Packaging {OutputDirectory} to {ZipPath}", PluginPackageDirectory, PluginZipPath);
            PluginPackageDirectory.ZipTo(PluginZipPath);
        });

    Target Release => _ => _
        .Description("Creates a GitHub release with the plugin and server binaries attached.")
        .DependsOn(PackagePlugin)
        .Requires(() => GitHubToken)
        .Requires(() => GitRepository)
        .Requires(() => Configuration == Configuration.Release)
        .Executes(async () =>
        {
            var version = GetSemanticVersion();
            var tagName = $"v{version}";

            Log.Information("Creating GitHub release {TagName}", tagName);

            var credentials = new Credentials(GitHubToken);
            var githubClient = new GitHubClient(new ProductHeaderValue("nuke-build"))
            {
                Credentials = credentials
            };

            var repositoryOwner = GitRepository.GetGitHubOwner();
            var repositoryName = GitRepository.GetGitHubName();

            var newRelease = new NewRelease(tagName)
            {
                Name = version,
                Draft = false,
                Prerelease = false,
                GenerateReleaseNotes = true
            };

            var release = await githubClient.Repository.Release.Create(repositoryOwner, repositoryName, newRelease);
            Log.Information("Release created: {ReleaseUrl}", release.HtmlUrl);

            // Upload the plugin zip as an asset
            using var pluginZipStream = File.OpenRead(PluginZipPath);
            var pluginAssetUpload = new ReleaseAssetUpload
            {
                FileName = PluginZipPath.Name,
                ContentType = "application/zip",
                RawData = pluginZipStream
            };

            var pluginAsset = await githubClient.Repository.Release.UploadAsset(release, pluginAssetUpload);
            Log.Information("Plugin asset uploaded: {AssetUrl}", pluginAsset.BrowserDownloadUrl);
        });

    static AbsolutePath GetVatSysPluginsDirectory(string profileName)
    {
        return GetVatSysProfilePath(profileName) / "Plugins";
    }

    static AbsolutePath GetVatSysProfilePath(string profileName)
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(documentsPath, "vatSys Files", "Profiles", profileName);
    }

    private string GetSemanticVersion()
    {
        // For main/master branch, or if we're on a detached head (building from a tag)
        // use major.minor.patch (e.g., "1.2.3")
        if (string.IsNullOrEmpty(GitVersion.BranchName) || GitVersion.BranchName is "main" or "master")
        {
            return GitVersion.MajorMinorPatch;
        }

        // For other branches (develop, hotfix, etc.): use SemVer format
        return GitVersion.SemVer;
    }
}
