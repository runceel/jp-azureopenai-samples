using System.Text.RegularExpressions;

namespace CallCenter.Text;

public partial class AnswerExtractPatterns
{
    [GeneratedRegex("(Q1|A1)(.*?)(Q2|A2)", RegexOptions.CultureInvariant)]
    public static partial Regex PatternQ1();
    [GeneratedRegex("(Q2|A2)(.*?)(Q3|A3)", RegexOptions.CultureInvariant)]
    public static partial Regex PatternQ2();
    [GeneratedRegex("(Q3|A3)(.*?)(Q4|A4)", RegexOptions.CultureInvariant)]
    public static partial Regex PatternQ3();
    [GeneratedRegex("(Q4|A4)(.*?)", RegexOptions.CultureInvariant)]
    public static partial Regex PatternQ4();
}
