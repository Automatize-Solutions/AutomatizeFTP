using System;
using System.Collections.Concurrent;
using AutomatizeFTP.Presentation.Interfaces;
using Avalonia.Input;

namespace AutomatizeFTP.Presentation.Avalonia.Views;

internal sealed record FileDragPayload(
    ICloudViewModel SourceProvider,
    string Path,
    string Name,
    bool IsFolder)
{
    private const string TokenPrefix = "automatizeftp-file:";
    private static readonly ConcurrentDictionary<string, FileDragPayload> ActivePayloads = new();

    public static string Register(FileDragPayload payload)
    {
        var token = Guid.NewGuid().ToString("N");
        ActivePayloads[token] = payload;
        return TokenPrefix + token;
    }

    public static FileDragPayload From(IDataTransfer dataTransfer)
    {
        var value = dataTransfer.TryGetText();
        if (value is null || !value.StartsWith(TokenPrefix, StringComparison.Ordinal))
            return null;

        var token = value[TokenPrefix.Length..];
        return ActivePayloads.TryGetValue(token, out var payload) ? payload : null;
    }

    public static FileDragPayload Take(IDataTransfer dataTransfer)
    {
        var value = dataTransfer.TryGetText();
        if (value is null || !value.StartsWith(TokenPrefix, StringComparison.Ordinal))
            return null;

        var token = value[TokenPrefix.Length..];
        return ActivePayloads.TryRemove(token, out var payload) ? payload : null;
    }

    public static void Release(string token)
    {
        if (token is not null && token.StartsWith(TokenPrefix, StringComparison.Ordinal))
            ActivePayloads.TryRemove(token[TokenPrefix.Length..], out _);
    }
}
