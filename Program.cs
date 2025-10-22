using hng_stage1_backend.Models;
using hng_stage1_backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<StringAnalysisService>();
builder.Services.AddSingleton<NaturalLanguageParser>();

var app = builder.Build();

// Configure the HTTP request pipeline.
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
    var query = context.Request.Query;
    var filters = new Dictionary<string, object>();
    bool invalid = false;

    if (query.TryGetValue("is_palindrome", out var pal))
    {
        if (bool.TryParse(pal, out var p)) filters["is_palindrome"] = p;
        else invalid = true;
    }
    if (query.TryGetValue("min_length", out var minl))
    {
        if (int.TryParse(minl, out var ml)) filters["min_length"] = ml;
        else invalid = true;
    }
    if (query.TryGetValue("max_length", out var maxl))
    {
        if (int.TryParse(maxl, out var mxl)) filters["max_length"] = mxl;
        else invalid = true;
    }
    if (query.TryGetValue("word_count", out var wc))
    {
        if (int.TryParse(wc, out var w)) filters["word_count"] = w;
        else invalid = true;
    }
    if (query.TryGetValue("contains_character", out var cc))
    {
        if (!string.IsNullOrEmpty(cc)) filters["contains_character"] = cc.ToString();
        else invalid = true;
    }

    if (invalid)
    {
        return Results.BadRequest(new { error = "Invalid query parameter values or types" });
    }

    var filtered = service.Filter(s =>
    {
        if (filters.TryGetValue("is_palindrome", out var p) && s.Properties.IsPalindrome != (bool)p) return false;
        if (filters.TryGetValue("min_length", out var ml) && s.Properties.Length < (int)ml) return false;
        if (filters.TryGetValue("max_length", out var mxl) && s.Properties.Length > (int)mxl) return false;
        if (filters.TryGetValue("word_count", out var wc) && s.Properties.WordCount != (int)wc) return false;
        if (filters.TryGetValue("contains_character", out var cc) && !s.Value.Contains((string)cc)) return false;
        return true;
    });

    var data = filtered.ToList();
    return Results.Ok(new
    {
        data,
        count = data.Count,
        filters_applied = filters
    });
});

// GET /strings/filter-by-natural-language
app.MapGet("/strings/filter-by-natural-language", (StringAnalysisService service, NaturalLanguageParser parser, string query) =>
{
    var filters = parser.ParseQuery(query);
    if (filters.Count == 0)
    {
        return Results.BadRequest(new { error = "Unable to parse natural language query" });
    }

    // Check for conflicting filters, e.g., min_length > max_length
    if (filters.TryGetValue("min_length", out var minl) && filters.TryGetValue("max_length", out var maxl) && (int)minl > (int)maxl)
    {
        return Results.UnprocessableEntity(new { error = "Query parsed but resulted in conflicting filters" });
    }

    var filtered = service.Filter(s =>
    {
        if (filters.TryGetValue("is_palindrome", out var p) && s.Properties.IsPalindrome != (bool)p) return false;
        if (filters.TryGetValue("min_length", out var ml) && s.Properties.Length < (int)ml) return false;
        if (filters.TryGetValue("max_length", out var mxl) && s.Properties.Length > (int)mxl) return false;
        if (filters.TryGetValue("word_count", out var wc) && s.Properties.WordCount != (int)wc) return false;
        if (filters.TryGetValue("contains_character", out var cc) && !s.Value.Contains((string)cc)) return false;
        return true;
    });

    var data = filtered.ToList();
    return Results.Ok(new
    {
        data,
        count = data.Count,
        interpreted_query = new
        {
            original = query,
            parsed_filters = filters
        }
    });
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
