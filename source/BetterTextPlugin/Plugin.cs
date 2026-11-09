using System.ComponentModel.Composition;
using System.Windows.Forms;
using BetterTextPlugin.Composer;
using BetterTextPlugin.Configuration;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using vatsys;
using vatsys.Plugin;

namespace BetterTextPlugin;

[Export(typeof(IPlugin))]
public class Plugin : ILabelPlugin, IStripPlugin
{
#if DEBUG
    public const string Name = "Better Text Plugin - Debug";
#else
    public const string Name = "Better Text Plugin";
#endif

    readonly VoiceCapabilityLabelItemProvider _labelItemProvider;
    readonly VoiceCapabilityStripItemProvider _stripItemProvider;
    readonly IErrorReporter _errorReporter;

    ToolStripMenuItem? _installationMenuItem;
    bool _labelsInstalled;

    const string InstallMenuLabel = "Install Better Text label items";
    const string UninstallMenuLabel = "Uninstall Better Text label items";

    string IPlugin.Name => Name;

    public Plugin()
    {
        var services = new ServiceCollection();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Plugin).Assembly));

        services.AddSingleton<IConfigurationLoader, ConfigurationLoader>();
        services.AddSingleton<ITemplateSearchService, TemplateSearchService>();
        services.AddSingleton<IParameterResolver, ParameterResolver>();
        services.AddSingleton<IComposerFactory>(sp =>
        {
            var config = sp.GetRequiredService<IConfigurationLoader>().Load();
            var templates = config.ToTemplates();
            return new ComposerFactory(
                templates,
                config.TransitionAltitude,
                sp.GetRequiredService<ITemplateSearchService>(),
                sp.GetRequiredService<IMediator>());
        });
        services.AddSingleton<IErrorReporter>(_ => new ErrorReporter(Name));
        services.AddSingleton(_ => new ColourCache
        {
            UnreadMessageColour = new CustomColour(255, 255, 165, 0)
        });
        services.AddSingleton<VoiceCapabilityLabelItemProvider>();
        services.AddSingleton<VoiceCapabilityStripItemProvider>();

        var provider = services.BuildServiceProvider();

        _labelItemProvider = provider.GetRequiredService<VoiceCapabilityLabelItemProvider>();
        _stripItemProvider = provider.GetRequiredService<VoiceCapabilityStripItemProvider>();
        _errorReporter = provider.GetRequiredService<IErrorReporter>();

        CheckXmlInstallation();
        AddToolbarItems();
    }

    void AddToolbarItems()
    {
        var status = XmlInstaller.GetStatus();
        if (!string.IsNullOrEmpty(status.ErrorMessage))
            return;

        _labelsInstalled = status.IsInstalled;

        var toolStripItem = new ToolStripMenuItem(_labelsInstalled ? UninstallMenuLabel : InstallMenuLabel);
        toolStripItem.Click += (_, _) =>
        {
            if (_labelsInstalled)
                RunUninstall();
            else
                RunInstall();
        };

        _installationMenuItem = toolStripItem;

        var menuItem = new CustomToolStripMenuItem(
            CustomToolStripMenuItemWindowType.Main,
            CustomToolStripMenuItemCategory.Tools,
            toolStripItem);
        MMI.AddCustomMenuItem(menuItem);
    }

    void CheckXmlInstallation()
    {
        try
        {
            var status = XmlInstaller.GetStatus();
            if (!string.IsNullOrEmpty(status.ErrorMessage))
            {
                _errorReporter.ReportError(new Exception($"Could not verify the Labels.xml and Strips.xml installation: {status.ErrorMessage}"));
                return;
            }

            if (!status.IsInstalled)
            {
                _errorReporter.ReportError(new Exception("Better Text label and strip items are not installed. Install them from the Tools menu."));
            }
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex, "Failed to verify the Labels.xml and Strips.xml installation");
        }
    }

    void RunInstall()
    {
        try
        {
            var result = XmlInstaller.Install();
            if (result.Succeeded)
            {
                SetInstallationMenuState(installed: true);
                MessageBox.Show(
                    "Better Text label & strip items installed successfully.\n\nPlease restart vatSys to apply the changes.",
                    Name,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    $"Failed to install Better Text label & strip items.\n\n{result.ErrorMessage}",
                    Name,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex, "Failed to install label & strip items");
        }
    }

    void RunUninstall()
    {
        try
        {
            var result = XmlInstaller.Uninstall();
            if (result.Succeeded)
            {
                SetInstallationMenuState(installed: false);
                MessageBox.Show(
                    "Better Text label & strip items uninstalled successfully.\n\nPlease restart vatSys to apply the changes.",
                    Name,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    $"Failed to uninstall Better Text label & strip items.\n\n{result.ErrorMessage}",
                    Name,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex, "Failed to uninstall label & strip items");
        }
    }

    void SetInstallationMenuState(bool installed)
    {
        _labelsInstalled = installed;
        if (_installationMenuItem is not null)
            _installationMenuItem.Text = installed ? UninstallMenuLabel : InstallMenuLabel;
    }

    public void OnFDRUpdate(FDP2.FDR updated) { }

    public void OnRadarTrackUpdate(RDP.RadarTrack updated) { }

    public CustomStripItem? GetCustomStripItem(
        string itemType,
        Track? track,
        FDP2.FDR? flightDataRecord,
        RDP.RadarTrack? radarTrack)
    {
        if (flightDataRecord is null)
            return null;

        return _stripItemProvider.GetStripItem(itemType, flightDataRecord);
    }

    public CustomLabelItem? GetCustomLabelItem(
        string itemType,
        Track? track,
        FDP2.FDR? flightDataRecord,
        RDP.RadarTrack? radarTrack)
    {
        if (flightDataRecord is null)
            return null;

        return _labelItemProvider.GetLabelItem(itemType, flightDataRecord);
    }

    public CustomColour? SelectASDTrackColour(Track track) => null;

    public CustomColour? SelectGroundTrackColour(Track track) => null;
}
