using System.Text;
using System.Text.RegularExpressions;

namespace BetterTextPlugin.Composer;

public interface IParameterResolver
{
    ResolvedTemplate Resolve(Template template, FdrSnapshot fdr);
}

public class ParameterResolver : IParameterResolver
{
    // Matches $UPPERCASE_IDENTIFIER placeholders
    static readonly Regex ParamPattern = new(@"\$([A-Z_]+)", RegexOptions.Compiled);

    public ResolvedTemplate Resolve(Template template, FdrSnapshot fdr)
    {
        var content = template.Content;
        var paramList = new List<(string Name, string? Value)>();
        bool hasUnresolved = false;

        var sb = new StringBuilder();
        int contentPos = 0;

        foreach (Match match in ParamPattern.Matches(content))
        {
            sb.Append(content.Substring(contentPos, match.Index - contentPos));

            var name = match.Groups[1].Value;
            if (fdr.TryResolve(name, out var value))
            {
                paramList.Add((name, value));
                sb.Append(value ?? string.Empty);
            }
            else
            {
                hasUnresolved = true;
                paramList.Add((name, null));
                sb.Append(match.Value);
            }

            contentPos = match.Index + match.Length;
        }

        sb.Append(content.Substring(contentPos));

        return new ResolvedTemplate(sb.ToString(), hasUnresolved, paramList, content);
    }
}
