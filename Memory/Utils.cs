using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

public static class Logger
{
    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config",
        "FLStudioRPC",
        "logs"
    );

    private static readonly string LogFilePath =
        Path.Combine(LogDir, "flrpc.log");

    private const long MaxLogSize = 512 * 1024;

    private static readonly object _lock = new object();

    public static void Log(string level, string message)
    {
        try
        {
            lock (_lock)
            {
                Directory.CreateDirectory(LogDir);

                if (File.Exists(LogFilePath))
                {
                    var fileInfo = new FileInfo(LogFilePath);

                    if (fileInfo.Length > MaxLogSize)
                    {
                        string oldLog = LogFilePath + ".old";

                        if (File.Exists(oldLog))
                        {
                            File.Delete(oldLog);
                        }

                        File.Move(LogFilePath, oldLog);
                    }
                }

                using (var writer = new StreamWriter(LogFilePath, true))
                {
                    string line =
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

                    writer.WriteLine(line);

                    System.Console.Error.WriteLine(line);
                }
            }
        }
        catch
        {
        }
    }

    public static void Info(string message)
    {
        Log("INFO", message);
    }

    public static void Warn(string message)
    {
        Log("WARN", message);
    }

    public static void Error(string message)
    {
        Log("ERROR", message);
    }

    public static void Error(string message, Exception ex)
    {
        Log(
            "ERROR",
            $"{message}: {ex.Message}"
        );

        Log(
            "ERROR",
            $"  Stack trace: {ex.StackTrace}"
        );
    }
}

public static class Utils
{
    private static string? _lastWindowTitle;
    private static string? _lastDetectedVersion;
    private static bool _xwininfoFailureLogged;

    private static readonly Regex FLStudioWindowTitlePattern =
        new Regex(
            @"^(?:(?<project>.+?)\s+-\s+)?(?<app>FL\s+Studio(?:\s+(?<version>(?:20\d{2}|\d{1,2})(?:\.\d+)*))?)\s*$",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant |
            RegexOptions.Compiled
        );

    private static string? GetFLStudioWindowTitle()
    {
        try
        {
            ProcessStartInfo psi =
                new ProcessStartInfo
                {
                    FileName = "xwininfo",
                    Arguments = "-root -tree",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

            using Process? process = Process.Start(psi);

            if (process == null)
            {
                LogXwininfoFailureOnce(
                    "Could not start xwininfo"
                );

                return null;
            }

            string output =
                process.StandardOutput.ReadToEnd();

            string error =
                process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                string message =
                    string.IsNullOrWhiteSpace(error)
                    ? $"xwininfo exited with code {process.ExitCode}"
                    : $"xwininfo exited with code {process.ExitCode}: {error.Trim()}";

                LogXwininfoFailureOnce(message);
                return null;
            }

            _xwininfoFailureLogged = false;

            foreach (string line in output.Split('\n'))
            {
                string trimmed = line.TrimStart();

                if (!trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int firstQuote = line.IndexOf('"');

                if (firstQuote == -1)
                {
                    continue;
                }

                int secondQuote =
                    line.IndexOf('"', firstQuote + 1);

                if (secondQuote == -1)
                {
                    continue;
                }

                string title =
                    line.Substring(
                        firstQuote + 1,
                        secondQuote - firstQuote - 1
                    ).Trim();

                if (string.IsNullOrWhiteSpace(title))
                {
                    continue;
                }

                if (!FLStudioWindowTitlePattern.IsMatch(title))
                {
                    continue;
                }

                if (!string.Equals(
                        title,
                        _lastWindowTitle,
                        StringComparison.Ordinal))
                {
                    Logger.Info(
                        $"FL Studio window title changed: '{_lastWindowTitle}' -> '{title}'"
                    );

                    _lastWindowTitle = title;
                }

                return title;
            }

            if (_lastWindowTitle != null)
            {
                Logger.Info(
                    "FL Studio window no longer detected by xwininfo"
                );
            }

            _lastWindowTitle = null;
            _lastDetectedVersion = null;

            return null;
        }
        catch (Exception ex)
        {
            if (!_xwininfoFailureLogged)
            {
                Logger.Error(
                    "FL Studio xwininfo detection failed",
                    ex
                );

                _xwininfoFailureLogged = true;
            }

            return null;
        }
    }

    private static void LogXwininfoFailureOnce(
        string message)
    {
        if (_xwininfoFailureLogged)
        {
            return;
        }

        Logger.Error(message);
        _xwininfoFailureLogged = true;
    }

    public static FLInfo GetFLInfo()
    {
        FLInfo info = new FLInfo();

        string? fullTitle =
            GetFLStudioWindowTitle();

        if (string.IsNullOrWhiteSpace(fullTitle))
        {
            info.ProjectName = null;
            info.AppName = null;

            return info;
        }

        Match match =
            FLStudioWindowTitlePattern.Match(fullTitle);

        if (!match.Success)
        {
            info.ProjectName = null;
            info.AppName = null;

            return info;
        }

        string appName =
            match.Groups["app"].Value.Trim();

        string? projectName =
            match.Groups["project"].Success
            ? match.Groups["project"].Value.Trim()
            : null;

        string? version =
            match.Groups["version"].Success
            ? match.Groups["version"].Value.Trim()
            : null;

        if (
            !string.IsNullOrWhiteSpace(version)
            &&
            !string.Equals(
                version,
                _lastDetectedVersion,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            Logger.Info(
                $"Detected FL Studio version {version} from xwininfo window title"
            );

            _lastDetectedVersion = version;
        }

        info.ProjectName =
            string.IsNullOrWhiteSpace(projectName)
            ? null
            : projectName;

        info.AppName = appName;

        return info;
    }

    public struct FLInfo
    {
        public string? AppName { get; set; }

        public string? ProjectName { get; set; }
    }
}
