using System;
using System.Collections.Generic;

namespace hng_stage1_backend.Models;

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