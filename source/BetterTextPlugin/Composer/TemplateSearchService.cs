using FuzzySharp;

namespace BetterTextPlugin.Composer;

public interface ITemplateSearchService
{
    IReadOnlyList<TemplateResultViewModel> Search(
        string query,
        IReadOnlyList<Template> templates,
        FdrSnapshot fdr);
}

public class TemplateSearchService(IParameterResolver resolver) : ITemplateSearchService
{
    const int ScoreThreshold = 60;

    public IReadOnlyList<TemplateResultViewModel> Search(
        string query,
        IReadOnlyList<Template> templates,
        FdrSnapshot fdr)
    {
        if (string.IsNullOrWhiteSpace(query))
            return templates
                .Select(t => new TemplateResultViewModel(resolver.Resolve(t, fdr)))
                .ToList();

        return templates
            .Select(t => (Template: t, Score: Score(query, t)))
            .Where(x => x.Score >= ScoreThreshold)
            .OrderByDescending(x => x.Score)
            .Select(x => new TemplateResultViewModel(resolver.Resolve(x.Template, fdr)))
            .ToList();
    }

    static int Score(string query, Template template)
    {
        var scores = template.Tags
            .Select(tag => ScoreAgainst(query, tag))
            .Append(ScoreAgainst(query, template.Content));

        return scores.Max();
    }

    static int ScoreAgainst(string query, string target) =>
        query.Length <= target.Length
            ? Fuzz.PartialRatio(query, target)
            : Fuzz.Ratio(query, target);
}
