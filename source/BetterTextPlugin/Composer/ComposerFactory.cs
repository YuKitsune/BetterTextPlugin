using System.Windows.Forms;
using MediatR;
using vatsys;

namespace BetterTextPlugin.Composer;

public interface IComposerFactory
{
    void Open(FDP2.FDR fdr);
}

public class ComposerFactory(
    IReadOnlyList<Template> templates,
    int transitionAltitude,
    ITemplateSearchService search,
    IMediator mediator) : IComposerFactory
{
    ComposerWindow? _current;

    public void Open(FDP2.FDR fdr)
    {
        _current?.Close();

        var snapshot = BuildSnapshot(fdr);
        var inbox = BuildInboxMessages(fdr.Callsign);
        var viewModel = new ComposerViewModel(snapshot, templates, inbox, search, mediator);

        _current = new ComposerWindow(viewModel, Cursor.Position);
        _current.FormClosed += (_, _) => _current = null;
        _current.Show();
    }

    FdrSnapshot BuildSnapshot(FDP2.FDR fdr)
    {
        var @params = new Dictionary<string, string?>();
        // TODO: populate with FDR fields once vatSys API is confirmed
        @params["ACID"] = fdr.Callsign;
        @params["ADEP"] = fdr.DepAirport;
        @params["ADES"] = fdr.DesAirport;
        @params["ROUTE"] = fdr.Route;
        @params["TYPE"] = fdr.AircraftType;
        @params["DRWY"] = fdr.DepartureRunway?.Name;
        @params["SID"] = fdr.SID?.Name;
        @params["ARWY"] = fdr.ArrivalRunway?.Name;
        @params["STAR"] = fdr.STAR?.Name;
        @params["ASSR"] = fdr.AssignedSSRCode.ToString("0000");
        @params["CFLUPPER"] = FormatAltitude(fdr.CFLUpper);
        @params["CFLLOWER"] = FormatAltitude(fdr.CFLLower);
        @params["CFL"] = FormatAltitude(fdr.CFLLower);
        
        // TODO: Source more fields from https://www.euroscope.hu/wp/built-in-functions/
        
        return new FdrSnapshot(fdr.Callsign, @params);
    }

    string FormatAltitude(int altitude) =>
        altitude >= transitionAltitude
            ? $"FL{altitude / 100:000}"
            : $"{altitude} ft";

    static List<InboxMessageViewModel> BuildInboxMessages(string callsign)
    {
        return Network.GetRadioMessages?
            .Where(m => m.Address == callsign)
            .Select(m => new InboxMessageViewModel(m.TimeStamp, m.Address, m.Message))
            .ToList()
            ?? new List<InboxMessageViewModel>();
    }
}
