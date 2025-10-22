using System.Security.Cryptography;
using System.Text;
using hng_stage1_backend.Models;

namespace hng_stage1_backend.Services;

public class StringAnalysisService
{
    private readonly List<StringAnalysis> _strings = new();

    public StringProperties ComputeProperties(string value)
    {
        var length = value.Length;
        var isPalindrome = IsPalindrome(value);
        var uniqueChars = new HashSet<char>(value).Count;
        var words = value.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var wordCount = words.Length;
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        var sha256 = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        var freq = new Dictionary<char, int>();
        foreach (var c in value)
        {
            freq[c] = freq.GetValueOrDefault(c, 0) + 1;
        }
        return new StringProperties
        {
            Length = length,
            IsPalindrome = isPalindrome,
            UniqueCharacters = uniqueChars,
            WordCount = wordCount,
            Sha256Hash = sha256,
            CharacterFrequencyMap = freq
        };
    }

    private bool IsPalindrome(string s)
    {
        var lower = s.ToLower();
        return lower == new string(lower.Reverse().ToArray());
    }

    public StringAnalysis? GetByValue(string value) => _strings.FirstOrDefault(s => s.Value == value);

    public StringAnalysis? GetById(string id) => _strings.FirstOrDefault(s => s.Id == id);

    public IEnumerable<StringAnalysis> GetAll() => _strings;

    public bool Add(StringAnalysis analysis)
    {
        if (_strings.Any(s => s.Id == analysis.Id)) return false;
        _strings.Add(analysis);
        return true;
    }

    public bool Delete(string value)
    {
        var item = _strings.FirstOrDefault(s => s.Value == value);
        if (item == null) return false;
        _strings.Remove(item);
        return true;
    }

    public IEnumerable<StringAnalysis> Filter(Func<StringAnalysis, bool> predicate) => _strings.Where(predicate);
}