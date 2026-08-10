// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Azure.Monitor.OpenTelemetry.Profiler.HostingStartup;

/// <summary>
/// Minimal, dependency-free logger used before the application's logging pipeline exists (the StartupHook
/// runs before <c>Main</c>; the HostingStartup runs while the host is still being built). Always writes to
/// stdout (so App Service log streaming picks it up) and mirrors to <see cref="Trace"/>. Never throws.
///
/// Optionally also appends to a diagnostic file when the <c>SP_STARTUP_LOG</c> environment variable is set -
/// the codeless injection is hard to debug precisely because it runs this early, and the file captures the
/// full StartupHook -> HostingStartup trace even when the log stream is not being watched. Enablement:
/// <list type="bullet">
///   <item><c>SP_STARTUP_LOG</c> unset/empty -> file logging off (stdout/Trace only).</item>
///   <item><c>SP_STARTUP_LOG=1</c> or <c>true</c> -> on, default path
///     <c>&lt;HOME&gt;/LogFiles/AzureMonitorProfiler/startup_&lt;pid&gt;.log</c> (HOME is the App Service
///     persistent root: <c>D:\home</c> on Windows, <c>/home</c> on Linux; falls back to the temp dir).</item>
///   <item>any other value -> treated as an explicit log file path.</item>
/// </list>
/// The file is per-PID so the app worker and the always-.NET SCM/Kudu worker never contend. All file IO is
/// fail-safe: a failure disables the file sink and falls back to stdout/Trace - it must never affect host
/// startup.
/// </summary>
internal static class BootstrapLog
{
    private const string DefaultComponent = "HostingStartup";
    private const string StartupLogEnvVar = "SP_STARTUP_LOG";

    private static readonly object s_fileLock = new();
    private static bool s_fileResolved;
    private static string? s_filePath;

    public static void Info(string message) => Write(DefaultComponent, "info", message);

    public static void Error(string message, Exception? exception = null) =>
        Write(DefaultComponent, "error", exception is null ? message : $"{message} {exception}");

    /// <summary>
    /// Writes a line tagged with <paramref name="component"/> (e.g. "HostingStartup" or "StartupHook") to
    /// stdout, <see cref="Trace"/>, and - when enabled - the diagnostic file. Never throws.
    /// </summary>
    public static void Write(string component, string level, string message)
    {
        string line = $"[Azure.Monitor.OpenTelemetry.Profiler.{component}] {level}: {message}";

        try { Console.WriteLine(line); } catch { /* Logging must never break host startup. */ }
        try { Trace.WriteLine(line); } catch { /* Ditto. */ }

        WriteToFile(component, level, message);
    }

    private static void WriteToFile(string component, string level, string message)
    {
        try
        {
            string? path = GetLogFilePath();
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
            string fileLine = $"{timestamp} [PID {GetProcessId()}] {level} {component}: {message}{Environment.NewLine}";

            lock (s_fileLock)
            {
                File.AppendAllText(path!, fileLine);
            }
        }
        catch
        {
            // On any file failure, disable the file sink for the rest of the process (avoid repeated throws).
            lock (s_fileLock)
            {
                s_fileResolved = true;
                s_filePath = null;
            }
        }
    }

    /// <summary>Resolves (once) the diagnostic log file path, creating its directory. Null when disabled.</summary>
    private static string? GetLogFilePath()
    {
        lock (s_fileLock)
        {
            if (s_fileResolved)
            {
                return s_filePath;
            }

            s_fileResolved = true;
            s_filePath = null;

            string? envValue = Environment.GetEnvironmentVariable(StartupLogEnvVar);
            string? home = Environment.GetEnvironmentVariable("HOME");
            if (TryResolveLogFilePath(envValue, home, GetProcessId(), out string path))
            {
                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir!);
                }
                s_filePath = path;
            }

            return s_filePath;
        }
    }

    /// <summary>
    /// Pure resolution of the diagnostic log file path from the <c>SP_STARTUP_LOG</c> value. Returns
    /// <see langword="false"/> (disabled) when the value is unset/empty; <c>1</c>/<c>true</c> yields the
    /// default path under <paramref name="homeDir"/> (or the temp dir); any other value is used verbatim as
    /// the file path. Testable and side-effect free (does not touch the filesystem).
    /// </summary>
    internal static bool TryResolveLogFilePath(string? envValue, string? homeDir, int pid, out string path)
    {
        path = string.Empty;

        string trimmed = envValue?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return false;
        }

        if (trimmed.Equals("1", StringComparison.Ordinal) ||
            trimmed.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            string root = string.IsNullOrEmpty(homeDir) ? Path.GetTempPath() : homeDir!;
            path = Path.Combine(root, "LogFiles", "AzureMonitorProfiler", $"startup_{pid}.log");
            return true;
        }

        path = trimmed;
        return true;
    }

    internal static int GetProcessId()
    {
        try { return Environment.ProcessId; } catch { return 0; }
    }
}
