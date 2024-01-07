using System.Text.Json.Serialization;

namespace ZoaReference.Features.Docs.Models;

public readonly record struct Document(string Name, string Url);