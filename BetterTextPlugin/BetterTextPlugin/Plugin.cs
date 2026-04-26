using BetterTextPlugin.Composer;
using BetterTextPlugin.Configuration;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using vatsys;
using vatsys.Plugin;

namespace BetterTextPlugin;

public class Plugin : ILabelPlugin, IStripPlugin
{
#if DEBUG
    public const string Name = "Better Text Plugin - Debug";
#else
    public const string Name = "Better Text Plugin";
#endif

    readonly VoiceCapabilityLabelItemProvider _labelItemProvider;
    readonly VoiceCapabilityStripItemProvider _stripItemProvider;

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
