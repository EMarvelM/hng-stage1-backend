# String Analyzer API

A RESTful API service that analyzes strings and computes various properties including length, palindrome check, unique characters, word count, SHA-256 hash, and character frequency.

## Features

- **POST /strings**: Analyze and store a new string
- **GET /strings/{value}**: Retrieve analysis for a specific string
- **GET /strings**: Filter strings with query parameters
- **GET /strings/filter-by-natural-language**: Natural language query filtering
- **DELETE /strings/{value}**: Remove a string from storage

## Tech Stack

- **Framework**: ASP.NET Core 8.0
- **Language**: C# 11
- **Architecture**: Minimal APIs
- **Container**: Docker

## Quick Start

### Local Development

```bash
# Clone the repository
git clone https://github.com/EMarvelM/hng-stage1-backend.git
cd hng-stage1-backend

# Run the application
dotnet run
```

The API will be available at `http://localhost:5121` with Swagger UI at `http://localhost:5121/swagger`.

### Docker

```bash
# Build the image
docker build -t string-analyzer .

# Run the container
docker run -p 8080:8080 string-analyzer
```

## API Examples

```bash
# Analyze a string
curl -X POST http://localhost:5121/strings \
  -H "Content-Type: application/json" \
  -d '{"value":"hello world"}'

# Get all 2-word strings
curl "http://localhost:5121/strings?word_count=2"

# Natural language query
curl "http://localhost:5121/strings/filter-by-natural-language?query=single%20word%20palindromic%20strings"
```

## Project Structure

- `Models/`: Data models (StringAnalysis, StringProperties)
- `Services/`: Business logic (StringAnalysisService, NaturalLanguageParser)
- `Program.cs`: API endpoints and configuration
- `Dockerfile`: Containerization setup

## Submission

This project was submitted as part of HNG Stage 1 Backend Task.

**Repository**: https://github.com/EMarvelM/hng-stage1-backend
**Deadline**: October 22, 2025, 11:59 PM WAT</content>
<parameter name="filePath">/home/mavel/Code/HNG/hng-stage1-backend/README.md