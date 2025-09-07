using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Newtonsoft.Json;
using Core.Contracts;
using Core.Models;
using Core.Steam.Models;
using Core.Steam.Interfaces;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;

namespace Core.Steam.Services;


public class SteamGamesPreLoader(HttpClient httpClient, ILogger<SteamGamesPreLoader> logger) : ISteamStoreGamesFetcher
{
    private readonly ILogger<SteamGamesPreLoader> _logger = logger;
    private const string FetchGamesBaseUrl = "https://api.steampowered.com/ISteamApps/GetAppList/v2/";

    public async IAsyncEnumerable<Game> SearchGamesAsync(string query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var items = await QueryStoreSearchAsync(httpClient, query, cancellationToken);
        foreach (var item in items)
        {
            yield return new Game(){Id = item.GameId.ToString(), Name = item.Name, ImageUrl = item.ImageUrl};
        }
    }

    public async IAsyncEnumerable<Game> DeepSearchGamesAsync(string query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var (appId, name) in StreamSearchFromUrlAsync(httpClient, FetchGamesBaseUrl, query, cancellationToken))
        {
            yield return new Game() { Id = appId.ToString(), Name = name };
        }
    }
    

    public static async Task<List<SteamGame>> QueryStoreSearchAsync(HttpClient http, string term, CancellationToken cancellationToken = default)
    {
        var q = Uri.EscapeDataString(term);
        var url = $"https://store.steampowered.com/api/storesearch/?term={q}&cc=US&l=en&limit=10";
        var json = await http.GetStringAsync(url, cancellationToken);

        // quick parsing via JsonDocument to be robust
        var doc = JsonDocument.Parse(json);
        var items = new List<SteamGame>();

        if (!doc.RootElement.TryGetProperty("items", out var itemsElem) || itemsElem.ValueKind != JsonValueKind.Array)
        {
            return items;
        }

        items.AddRange(itemsElem.EnumerateArray().Select(it => new SteamGame { 
            GameId = it.GetProperty("id").GetInt32(), 
            Name = it.GetProperty("name").GetString(), 
            ImageUrl = it.TryGetProperty("tiny_image", out var imgProp) ? imgProp.GetString() : null
        }));
        return items;
    }
    

    public static async IAsyncEnumerable<(int AppId, string Name)> StreamSearchFromUrlAsync(
        HttpClient http,
        string url,
        string query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var stream = await http.GetStreamAsync(url, cancellationToken).ConfigureAwait(false);
        await foreach (var hit in StreamSearchWithChannelAsync(stream, query, cancellationToken))
        {
            yield return hit;
        }
    }

    public static async IAsyncEnumerable<(int AppId, string Name)> StreamSearchWithChannelAsync(
        Stream stream,
        string query,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        query ??= string.Empty;
        query = query.Trim().ToLowerInvariant();

        var channel = Channel.CreateUnbounded<(int, string)>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        // Start the parser in background (Task). It may await ReadAsync, but it MUST NOT yield while Utf8JsonReader is alive.
        var parseTask = Task.Run(async () =>
        {
            try
            {
                const int BufferSize = 16 * 1024;
                var buffer = new byte[BufferSize];
                var jsonState = new JsonReaderState();
                byte[] leftover = Array.Empty<byte>();

                string currentProperty = null;
                bool inAppsArray = false, inAppObject = false;
                int currentAppId = 0;
                string currentName = null;

                while (true)
                {
                    int read = await stream.ReadAsync(buffer.AsMemory(0, BufferSize), cancellationToken).ConfigureAwait(false);
                    bool isFinalBlock = read == 0;

                    ReadOnlyMemory<byte> chunk;
                    if (leftover.Length > 0)
                    {
                        if (isFinalBlock)
                            chunk = leftover;
                        else
                        {
                            var combined = new byte[leftover.Length + read];
                            Buffer.BlockCopy(leftover, 0, combined, 0, leftover.Length);
                            Buffer.BlockCopy(buffer, 0, combined, leftover.Length, read);
                            chunk = combined;
                        }
                    }
                    else
                    {
                        if (isFinalBlock) break;
                        chunk = new ReadOnlyMemory<byte>(buffer, 0, read);
                    }

                    // Parse synchronously within this scope
                    var reader = new Utf8JsonReader(chunk.Span, isFinalBlock, jsonState);

                    while (reader.Read())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        switch (reader.TokenType)
                        {
                            case JsonTokenType.PropertyName:
                                currentProperty = reader.GetString();
                                break;
                            case JsonTokenType.StartArray:
                                if (string.Equals(currentProperty, "apps", StringComparison.OrdinalIgnoreCase))
                                    inAppsArray = true;
                                currentProperty = null;
                                break;
                            case JsonTokenType.EndArray:
                                if (inAppsArray) inAppsArray = false;
                                break;
                            case JsonTokenType.StartObject:
                                if (inAppsArray)
                                {
                                    inAppObject = true;
                                    currentAppId = 0;
                                    currentName = null;
                                }
                                break;
                            case JsonTokenType.EndObject:
                                if (inAppObject)
                                {
                                    inAppObject = false;
                                    if (!string.IsNullOrEmpty(currentName) && currentName.ToLowerInvariant().Contains(query))
                                    {
                                        // write to channel synchronously (no await here)
                                        channel.Writer.TryWrite((currentAppId, currentName));
                                    }
                                }
                                break;
                            case JsonTokenType.Number:
                                if (inAppObject && string.Equals(currentProperty, "appid", StringComparison.OrdinalIgnoreCase))
                                    reader.TryGetInt32(out currentAppId);
                                currentProperty = null;
                                break;
                            case JsonTokenType.String:
                                if (inAppObject && string.Equals(currentProperty, "name", StringComparison.OrdinalIgnoreCase))
                                    currentName = reader.GetString();
                                currentProperty = null;
                                break;
                            default:
                                currentProperty = null;
                                break;
                        }
                    }

                    // preserve state + leftover bytes
                    jsonState = reader.CurrentState;
                    var consumed = (int)reader.BytesConsumed;
                    if (consumed < chunk.Length)
                    {
                        int remaining = (int)(chunk.Length - consumed);
                        leftover = new byte[remaining];
                        chunk.Span.Slice(consumed, remaining).CopyTo(leftover);
                    }
                    else leftover = Array.Empty<byte>();

                    if (isFinalBlock) break;
                }

                // final leftover pass
                if (leftover.Length > 0)
                {
                    var finalReader = new Utf8JsonReader(leftover, true, jsonState);
                    while (finalReader.Read())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        switch (finalReader.TokenType)
                        {
                            case JsonTokenType.PropertyName:
                                currentProperty = finalReader.GetString();
                                break;
                            case JsonTokenType.StartArray:
                                if (string.Equals(currentProperty, "apps", StringComparison.OrdinalIgnoreCase))
                                    inAppsArray = true;
                                currentProperty = null;
                                break;
                            case JsonTokenType.StartObject:
                                if (inAppsArray)
                                {
                                    inAppObject = true;
                                    currentAppId = 0;
                                    currentName = null;
                                }
                                break;
                            case JsonTokenType.EndObject:
                                if (inAppObject)
                                {
                                    inAppObject = false;
                                    if (!string.IsNullOrEmpty(currentName) && currentName.ToLowerInvariant().Contains(query))
                                        channel.Writer.TryWrite((currentAppId, currentName));
                                }
                                break;
                            case JsonTokenType.Number:
                                if (inAppObject && string.Equals(currentProperty, "appid", StringComparison.OrdinalIgnoreCase))
                                    finalReader.TryGetInt32(out currentAppId);
                                currentProperty = null;
                                break;
                            case JsonTokenType.String:
                                if (inAppObject && string.Equals(currentProperty, "name", StringComparison.OrdinalIgnoreCase))
                                    currentName = finalReader.GetString();
                                currentProperty = null;
                                break;
                            default:
                                currentProperty = null;
                                break;
                        }
                    }
                }

                channel.Writer.Complete();
            }
            catch (Exception ex)
            {
                channel.Writer.Complete(ex);
            }
        }, cancellationToken);

        // Consumer: read from the channel asynchronously
        await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return item;
        }

        // ensure parse exceptions are observed
        await parseTask.ConfigureAwait(false);
    }
    

    // Now accepts a Stream so you can pass any source (http, file, IBrowserFile, etc.)
    public static async IAsyncEnumerable<(int AppId, string Name)> StreamSearchAppListAsync(
        Stream stream,
        string query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (query is null) query = string.Empty;
        query = query.Trim().ToLowerInvariant();

        const int BufferSize = 16 * 1024;
        var buffer = new byte[BufferSize];

        var jsonState = new JsonReaderState();
        byte[] leftover = Array.Empty<byte>();

        string currentProperty = null;
        bool inAppsArray = false;
        bool inAppObject = false;
        int currentAppId = 0;
        string currentName = null;

        while (true)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(0, BufferSize), cancellationToken).ConfigureAwait(false);
            bool isFinalBlock = read == 0;

            ReadOnlyMemory<byte> chunk;
            if (leftover.Length > 0)
            {
                if (isFinalBlock)
                {
                    chunk = leftover;
                }
                else
                {
                    var combined = new byte[leftover.Length + read];
                    Buffer.BlockCopy(leftover, 0, combined, 0, leftover.Length);
                    Buffer.BlockCopy(buffer, 0, combined, leftover.Length, read);
                    chunk = combined;
                }
            }
            else
            {
                if (isFinalBlock) break;
                chunk = new ReadOnlyMemory<byte>(buffer, 0, read);
            }

            var hitsThisChunk = new List<(int, string)>();

            // Parse synchronously within this scope (Utf8JsonReader is a ref struct)
            {
                var reader = new Utf8JsonReader(chunk.Span, isFinalBlock, jsonState);

                while (reader.Read())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    switch (reader.TokenType)
                    {
                        case JsonTokenType.PropertyName:
                            currentProperty = reader.GetString();
                            break;

                        case JsonTokenType.StartArray:
                            if (string.Equals(currentProperty, "apps", StringComparison.OrdinalIgnoreCase))
                                inAppsArray = true;
                            currentProperty = null;
                            break;

                        case JsonTokenType.EndArray:
                            if (inAppsArray) inAppsArray = false;
                            break;

                        case JsonTokenType.StartObject:
                            if (inAppsArray)
                            {
                                inAppObject = true;
                                currentAppId = 0;
                                currentName = null;
                            }
                            break;

                        case JsonTokenType.EndObject:
                            if (inAppObject)
                            {
                                inAppObject = false;
                                if (!string.IsNullOrEmpty(currentName) && currentName.ToLowerInvariant().Contains(query))
                                    hitsThisChunk.Add((currentAppId, currentName));
                            }
                            break;

                        case JsonTokenType.Number:
                            if (inAppObject && string.Equals(currentProperty, "appid", StringComparison.OrdinalIgnoreCase))
                                reader.TryGetInt32(out currentAppId);
                            currentProperty = null;
                            break;

                        case JsonTokenType.String:
                            if (inAppObject && string.Equals(currentProperty, "name", StringComparison.OrdinalIgnoreCase))
                                currentName = reader.GetString();
                            currentProperty = null;
                            break;

                        default:
                            currentProperty = null;
                            break;
                    }
                }

                jsonState = reader.CurrentState;
                var consumed = (int)reader.BytesConsumed;
                if (consumed < chunk.Length)
                {
                    int remaining = (int)(chunk.Length - consumed);
                    leftover = new byte[remaining];
                    chunk.Span.Slice(consumed, remaining).CopyTo(leftover);
                }
                else
                {
                    leftover = Array.Empty<byte>();
                }
            } // reader out of scope

            // yield results after reader is disposed
            foreach (var hit in hitsThisChunk) yield return hit;

            if (isFinalBlock) break;
        }

        // Final leftover pass
        if (leftover.Length > 0)
        {
            var hitsFinal = new List<(int, string)>();
            {
                var finalReader = new Utf8JsonReader(leftover, true, jsonState);
                while (finalReader.Read())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    switch (finalReader.TokenType)
                    {
                        case JsonTokenType.PropertyName:
                            currentProperty = finalReader.GetString();
                            break;

                        case JsonTokenType.StartArray:
                            if (string.Equals(currentProperty, "apps", StringComparison.OrdinalIgnoreCase))
                                inAppsArray = true;
                            currentProperty = null;
                            break;

                        case JsonTokenType.EndArray:
                            if (inAppsArray) inAppsArray = false;
                            break;

                        case JsonTokenType.StartObject:
                            if (inAppsArray)
                            {
                                inAppObject = true;
                                currentAppId = 0;
                                currentName = null;
                            }
                            break;

                        case JsonTokenType.EndObject:
                            if (inAppObject)
                            {
                                inAppObject = false;
                                if (!string.IsNullOrEmpty(currentName) && currentName.ToLowerInvariant().Contains(query))
                                    hitsFinal.Add((currentAppId, currentName));
                            }
                            break;

                        case JsonTokenType.Number:
                            if (inAppObject && string.Equals(currentProperty, "appid", StringComparison.OrdinalIgnoreCase))
                                finalReader.TryGetInt32(out currentAppId);
                            currentProperty = null;
                            break;

                        case JsonTokenType.String:
                            if (inAppObject && string.Equals(currentProperty, "name", StringComparison.OrdinalIgnoreCase))
                                currentName = finalReader.GetString();
                            currentProperty = null;
                            break;

                        default:
                            currentProperty = null;
                            break;
                    }
                }
            } // finalReader out of scope

            foreach (var hit in hitsFinal) yield return hit;
        }
    }

}