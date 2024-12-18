using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace AmsiProvider;

internal class ProviderConfig
{
    private static ProviderConfig? _config = null;

    private readonly Stream _logStream;
    private readonly Func<ReadOnlySpan<byte>, string> _readBufferFunc;

    private ProviderConfig(
        Stream logStream,
        Func<ReadOnlySpan<byte>, string> readBufferFunc)
    {
        _logStream = logStream;
        _readBufferFunc = readBufferFunc;
    }

    public static ProviderConfig Load()
    {
        if (_config is not null)
        {
            return _config;
        }

        string? moduleDirectory = null;
        string? configFileName = null;
        if (Dll.TryGetDllPath(out string? modulePath, out int _))
        {
            moduleDirectory = Path.GetDirectoryName(modulePath);
            configFileName = Path.GetFileNameWithoutExtension(moduleDirectory);
        }

        if (string.IsNullOrWhiteSpace(moduleDirectory))
        {
            moduleDirectory = Environment.CurrentDirectory;
        }
        if (string.IsNullOrWhiteSpace(configFileName))
        {
            configFileName = Dll.ProviderName;
        }

        ConfigJson? config = null;
        string configPath = Path.Combine(moduleDirectory, $"{configFileName}.config.json");
        if (File.Exists(configPath))
        {
            using FileStream fs = File.OpenRead(configPath);
            config = JsonSerializer.Deserialize(fs, SourceGenerationContext.Default.ConfigJson);
        }

        string? contentEncoding = config?.ContentEncoding?.ToLowerInvariant();
        Func<ReadOnlySpan<byte>, string> readFunc;
        if (contentEncoding == "base64")
        {
            readFunc = static (b) => Convert.ToBase64String(b);
        }
        else if (contentEncoding == "hex")
        {
            readFunc = Convert.ToHexString;
        }
        else if (contentEncoding is null || contentEncoding == "unicode")
        {
            readFunc = static (b) => new(MemoryMarshal.Cast<byte, char>(b));
        }
        else
        {
            readFunc = Encoding.GetEncoding(contentEncoding).GetString;
        }

        Stream logStream;
        if (config?.LogPath?.Equals("stdout", StringComparison.OrdinalIgnoreCase) == true)
        {
            logStream = Console.OpenStandardOutput();
        }
        else if (config?.LogPath?.Equals("stderr", StringComparison.OrdinalIgnoreCase) == true)
        {
            logStream = Console.OpenStandardError();
        }
        else
        {
            string logDirectory = config?.LogPath ?? Path.Combine(moduleDirectory);
            string logFileName = config?.StoreByPid == true
                ? $"{configFileName}.{Environment.ProcessId}.log"
                : $"{configFileName}.log";
            string logPath = Path.Combine(logDirectory, logFileName);

            logStream = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.Read);
        }

        _config = new(logStream, readFunc);
        return _config;
    }

    public void WriteEntry(
        string action,
        string appName,
        string contentName,
        long sessionId,
        nint buffer,
        int length)
    {
        string content;
        if (buffer == nint.Zero || length == 0)
        {
            content = "";
        }

        unsafe
        {
            ReadOnlySpan<byte> view = new((void*)buffer, length);
            content = _readBufferFunc(view);
        }

        WriteEntry(action, appName, contentName, sessionId, content);
    }

    public void WriteEntry(
        string action,
        string appName,
        string contentName,
        long sessionId,
        string content)
    {
        ScanEntry entry = new(action, appName, contentName, sessionId, content);

        JsonSerializer.Serialize(
            _logStream,
            entry,
            SourceGenerationContext.Default.ScanEntry);
        _logStream.Write("\n"u8);
        _logStream.Flush();
    }

    public void WriteException(string context, Exception e)
    {
        string msg = $"{context} failed: {e.Message}\n{e.StackTrace}";
        using StreamWriter writer = new(_logStream, leaveOpen: true);
        writer.WriteLine(msg);
        writer.Flush();
    }
}
