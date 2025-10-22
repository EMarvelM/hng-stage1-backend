# String Analyzer Service Implementation Guide

This guide provides a step-by-step explanation of how the String Analyzer Service was implemented in C# using ASP.NET Core. It's designed for beginners to understand the thought process, decisions, and code structure from the ground up.

## Step 1: Project Setup and Understanding Requirements

### Thought Process
Before writing any code, I analyzed the task requirements:
- Build a RESTful API for string analysis
- Compute properties: length, palindrome check, unique characters, word count, SHA-256 hash, character frequency
- Endpoints: POST to create, GET to retrieve, GET with filters, GET with natural language, DELETE
- Use C# and ASP.NET Core (inferred from the project structure)
- Store strings in memory (no database specified)

### Implementation
1. Created a new ASP.NET Core Web API project using `dotnet new webapi`
2. This gives us a minimal API setup with Swagger for testing
3. The project targets .NET 8.0 for modern features

## Step 2: Defining Data Models

### Thought Process
I needed to represent the string analysis data. In C#, records are perfect for immutable data structures like this. Records provide:
- Automatic equality comparison
- Immutable by default (init-only setters)
- Concise syntax
- Built-in ToString() for debugging

For the properties, I created a separate record to keep the model clean and allow reuse.

### Implementation
Created `Models/StringAnalysis.cs`:

```csharp
public record StringAnalysis
{
    public required string Id { get; init; }
    public required string Value { get; init; }
    public required StringProperties Properties { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public record StringProperties
{
    public required int Length { get; init; }
    public required bool IsPalindrome { get; init; }
    public required int UniqueCharacters { get; init; }
    public required int WordCount { get; init; }
    public required string Sha256Hash { get; init; }
    public required Dictionary<char, int> CharacterFrequencyMap { get; init; }
}
```

**Why records?**
- `required` ensures all properties are set during creation
- `init` makes them immutable after creation
- Perfect for API responses that shouldn't be modified

## Step 3: Implementing Core Business Logic

### Thought Process
The computation logic should be separate from the API layer. This follows the Single Responsibility Principle. I created a service class to handle:
- Computing all properties for a string
- Storing and retrieving analyses
- Filtering logic

For SHA-256 hashing, I used .NET's built-in `SHA256` class from `System.Security.Cryptography`.

For palindrome check, I compared the string with its reverse, ignoring case.

### Implementation
Created `Services/StringAnalysisService.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;

public class StringAnalysisService
{
    private readonly Dictionary<string, StringAnalysis> _analyses = new();

    public StringProperties ComputeProperties(string value)
    {
        var length = value.Length;
        var isPalindrome = IsPalindrome(value);
        var uniqueCharacters = value.Distinct().Count();
        var wordCount = value.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var sha256Hash = ComputeSha256Hash(value);
        var characterFrequencyMap = ComputeCharacterFrequency(value);

        return new StringProperties
        {
            Length = length,
            IsPalindrome = isPalindrome,
            UniqueCharacters = uniqueCharacters,
            WordCount = wordCount,
            Sha256Hash = sha256Hash,
            CharacterFrequencyMap = characterFrequencyMap
        };
    }

    private bool IsPalindrome(string value)
    {
        var reversed = new string(value.Reverse().ToArray());
        return string.Equals(value, reversed, StringComparison.OrdinalIgnoreCase);
    }

    private string ComputeSha256Hash(string value)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLower();
    }

    private Dictionary<char, int> ComputeCharacterFrequency(string value)
    {
        return value.GroupBy(c => c)
                   .ToDictionary(g => g.Key, g => g.Count());
    }

    public bool Add(StringAnalysis analysis)
    {
        if (_analyses.ContainsKey(analysis.Id)) return false;
        _analyses[analysis.Id] = analysis;
        return true;
    }

    public StringAnalysis? GetById(string id) => _analyses.GetValueOrDefault(id);
    public StringAnalysis? GetByValue(string value) => _analyses.Values.FirstOrDefault(a => a.Value == value);
    public IEnumerable<StringAnalysis> GetAll() => _analyses.Values;
    public IEnumerable<StringAnalysis> Filter(Func<StringAnalysis, bool> predicate) => _analyses.Values.Where(predicate);
    public bool Delete(string value) => _analyses.Remove(_analyses.FirstOrDefault(a => a.Value.Value == value).Key);
}
```

**Key Concepts:**
- **LINQ**: Used for character frequency (`GroupBy`, `ToDictionary`) and filtering
- **Hashing**: SHA-256 for unique identification
- **Thread Safety**: In a real app, we'd use concurrent collections, but for simplicity, we use Dictionary
- **Separation of Concerns**: Computation logic separate from storage

## Step 4: Implementing Natural Language Parsing

### Thought Process
For the natural language endpoint, I needed to parse simple English queries into filters. This is complex, but for the scope, I implemented basic pattern matching.

The parser looks for keywords like "single word", "palindromic", "longer than", etc.

### Implementation
Created `Services/NaturalLanguageParser.cs`:

```csharp
public class NaturalLanguageParser
{
    public Dictionary<string, object> ParseQuery(string query)
    {
        var filters = new Dictionary<string, object>();
        var lowerQuery = query.ToLower();

        // Parse word count
        if (lowerQuery.Contains("single word") || lowerQuery.Contains("one word"))
        {
            filters["word_count"] = 1;
        }

        // Parse palindrome
        if (lowerQuery.Contains("palindromic") || lowerQuery.Contains("palindrome"))
        {
            filters["is_palindrome"] = true;
        }

        // Parse length
        var lengthMatch = System.Text.RegularExpressions.Regex.Match(lowerQuery, @"longer than (\d+)");
        if (lengthMatch.Success)
        {
            filters["min_length"] = int.Parse(lengthMatch.Groups[1].Value) + 1;
        }

        // Parse character
        var charMatch = System.Text.RegularExpressions.Regex.Match(lowerQuery, @"contain.*letter ([a-z])");
        if (charMatch.Success)
        {
            filters["contains_character"] = charMatch.Groups[1].Value;
        }

        return filters;
    }
}
```

**Why Regex?**
- For pattern matching in text
- Simple way to extract numbers and characters from natural language

## Step 5: Building the API Endpoints

### Thought Process
Using ASP.NET Core Minimal APIs for simplicity. Each endpoint maps to a specific HTTP method and path.

I used dependency injection to inject the services into the endpoints.

For error handling, I returned appropriate HTTP status codes with error messages.

### Implementation
In `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<StringAnalysisService>();
builder.Services.AddSingleton<NaturalLanguageParser>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// POST /strings
app.MapPost("/strings", (StringAnalysisService service, CreateStringRequest request) =>
{
    if (string.IsNullOrEmpty(request.Value))
    {
        return Results.BadRequest(new { error = "Invalid request body or missing 'value' field" });
    }

    var properties = service.ComputeProperties(request.Value);
    var analysis = new StringAnalysis
    {
        Id = properties.Sha256Hash,
        Value = request.Value,
        Properties = properties,
        CreatedAt = DateTime.UtcNow
    };

    if (!service.Add(analysis))
    {
        return Results.Conflict(new { error = "String already exists in the system" });
    }

    return Results.Created($"/strings/{Uri.EscapeDataString(request.Value)}", analysis);
});

// GET /strings/{string_value}
app.MapGet("/strings/{string_value}", (StringAnalysisService service, string string_value) =>
{
    var decoded = Uri.UnescapeDataString(string_value);
    var item = service.GetByValue(decoded);
    if (item == null)
    {
        return Results.NotFound(new { error = "String does not exist in the system" });
    }
    return Results.Ok(item);
});

// GET /strings with filters
app.MapGet("/strings", (StringAnalysisService service, HttpContext context) =>
{
    // Parse query parameters and apply filters
    // ... (implementation as shown earlier)
});

// GET /strings/filter-by-natural-language
app.MapGet("/strings/filter-by-natural-language", (StringAnalysisService service, NaturalLanguageParser parser, string query) =>
{
    // Parse query and filter results
    // ... (implementation as shown earlier)
});

// DELETE /strings/{string_value}
app.MapDelete("/strings/{string_value}", (StringAnalysisService service, string string_value) =>
{
    var decoded = Uri.UnescapeDataString(string_value);
    if (service.Delete(decoded))
    {
        return Results.NoContent();
    }
    return Results.NotFound(new { error = "String does not exist in the system" });
});

app.Run();

record CreateStringRequest(string Value);
```

**Key Concepts:**
- **Minimal APIs**: No controllers, just lambda functions
- **Dependency Injection**: Services injected as parameters
- **Result Objects**: `Results.Created`, `Results.Ok`, etc. for proper HTTP responses
- **URL Encoding**: Handle spaces in string values

## Step 6: Configuration and Launch Settings

### Thought Process
For development, I enabled Swagger UI for easy testing. The launch settings define the ports and environment.

### Implementation
In `appsettings.json` and `launchSettings.json`:
- Default settings for development
- Swagger enabled in dev mode

## Step 7: Testing and Validation

### Thought Process
After implementation, I tested each endpoint manually using curl to ensure:
- Correct responses
- Proper error handling
- Data persistence

### Testing Commands
```bash
# Start the server
dotnet run

# Test POST
curl -X POST http://localhost:5121/strings -H "Content-Type: application/json" -d '{"value":"hello world"}'

# Test GET
curl -X GET "http://localhost:5121/strings/hello%20world"

# Test filters
curl -X GET "http://localhost:5121/strings?word_count=2"

# Test natural language
curl -X GET "http://localhost:5121/strings/filter-by-natural-language?query=all%20single%20word%20palindromic%20strings"

# Test DELETE
curl -X DELETE "http://localhost:5121/strings/hello%20world"
```

## Key C# Concepts Learned

1. **Records**: Immutable data structures
2. **LINQ**: Querying collections (GroupBy, Where, Select)
3. **Dependency Injection**: Registering and injecting services
4. **Minimal APIs**: Modern way to build APIs in ASP.NET Core
5. **Async/Await**: Though not used here, important for I/O operations
6. **Exception Handling**: Try-catch for robust code
7. **Regular Expressions**: Pattern matching for parsing
8. **Hashing and Cryptography**: Using built-in security classes

## Architecture Decisions

- **In-Memory Storage**: Simple for this task, but in production, use a database
- **Singleton Services**: Fine for demo, but consider scoped services for real apps
- **No Authentication**: Not required for this task
- **Basic NLP**: Simple regex-based parsing; could use ML for better accuracy

## Next Steps for Learning

1. Add a database (Entity Framework Core)
2. Implement proper logging
3. Add unit tests (xUnit)
4. Deploy to a cloud service
5. Add authentication and authorization
6. Improve natural language parsing with better algorithms

This implementation demonstrates a complete, working REST API while introducing key C# and ASP.NET Core concepts. Start by understanding each step, then try modifying the code to add new features!</content>
<parameter name="filePath">/home/mavel/Code/HNG/hng-stage1-backend/ImplementationGuide.md