using System.Text.RegularExpressions;

namespace hng_stage1_backend.Services;

public class NaturalLanguageParser
{
    public Dictionary<string, object> ParseQuery(string query)
    {
        var filters = new Dictionary<string, object>();
        var lower = query.ToLower();

        if (lower.Contains("single word")) filters["word_count"] = 1;
        if (lower.Contains("palindromic")) filters["is_palindrome"] = true;

        // longer than X
        var match = Regex.Match(lower, @"longer than (\d+)");
        if (match.Success) filters["min_length"] = int.Parse(match.Groups[1].Value) + 1;

        // shorter than X
        match = Regex.Match(lower, @"shorter than (\d+)");
        if (match.Success) filters["max_length"] = int.Parse(match.Groups[1].Value) - 1;

        // containing the letter X
        match = Regex.Match(lower, @"containing the letter (\w)");
        if (match.Success) filters["contains_character"] = match.Groups[1].Value;

        // exact word count
        match = Regex.Match(lower, @"word count of (\d+)");
        if (match.Success) filters["word_count"] = int.Parse(match.Groups[1].Value);

        return filters;
    }
}