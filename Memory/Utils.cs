using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

using Console = Colorful.Console;

using static ConfigValues;

public static class Logger
{
    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config",
        "FLStudioRPC",
        "logs"
    );

    private static readonly string LogFilePath = Path.Combine(LogDir, "flrpc.log");
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
                        if (File.Exists(oldLog)) File.Delete(oldLog);
                        File.Move(LogFilePath, oldLog);
                    }
                }

                using (var writer = new StreamWriter(LogFilePath, true))
                {
                    writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");
                }
            }
        }
        catch
        {
        }
    }

    public static void Info(string message) => Log("INFO", message);
    public static void Warn(string message) => Log("WARN", message);
    public static void Error(string message) => Log("ERROR", message);

    public static void Error(string message, Exception ex)
    {
        Log("ERROR", $"{message}: {ex.Message}");
        Log("ERROR", $"  Stack trace: {ex.StackTrace}");
    }
}

public static class Utils
{
    private static string _lastWindowTitle = null;

    public static string GetMainWindowsTitleByProcessNames(params string[] processNames)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = "-c \"xwininfo -root -tree\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = Process.Start(psi);

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();


            foreach (string line in output.Split('\n'))
            {
                if (!line.Contains("\""))
                    continue;


                string trimmed = line.Trim();

                if (!trimmed.StartsWith("0x"))
                    continue;


                int firstQuote = line.IndexOf('"');

                if (firstQuote == -1)
                    continue;


                int secondQuote = line.IndexOf('"', firstQuote + 1);

                if (secondQuote == -1)
                    continue;


                string title = line.Substring(
                    firstQuote + 1,
                    secondQuote - firstQuote - 1
                );

                if (title.Contains(
                        "Visual Studio Code",
                        StringComparison.OrdinalIgnoreCase))
                    continue;


                if (title.Contains(
                        "FLHintBarForm",
                        StringComparison.OrdinalIgnoreCase))
                    continue;


                if (title.Contains(
                        "FL Studio",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (title != _lastWindowTitle)
                    {
                        Logger.Info(
                            $"Window title changed: '{_lastWindowTitle}' -> '{title}'"
                        );

                        _lastWindowTitle = title;
                    }

                    return title;
                }

                if (
                    title.Contains(
                        "Settings",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    title.Contains(
                        "credits",
                        StringComparison.OrdinalIgnoreCase)
                )
                {
                    if (title != _lastWindowTitle)
                    {
                        Logger.Info(
                            $"Window title changed: '{_lastWindowTitle}' -> '{title}'"
                        );

                        _lastWindowTitle = title;
                    }

                    return title;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(
                "Linux window detection failed",
                ex
            );
        }


        return null;
    }


    public static Version GetApplicationVersion(string processName)
    {
        return null;
    }


    public static FLInfo GetFLInfo()
    {
        FLInfo Info = new FLInfo();

        string fullTitle = GetMainWindowsTitleByProcessNames("FL");


        if (string.IsNullOrEmpty(fullTitle))
        {
            Info.ProjectName = null;
            Info.AppName = null;
        }
        else
        {
            if (AccurateVersion)
            {
                Version accurateVersion =
                    GetApplicationVersion("FL64")
                    ?? GetApplicationVersion("FL");

                Info.AppName =
                    accurateVersion != null
                    ? $"FL Studio {accurateVersion}"
                    : null;
            }
            else
            {
                int hyphenIndex = fullTitle.LastIndexOf(" - ");


                Info.ProjectName =
                    hyphenIndex == -1
                    ? null
                    : fullTitle.Substring(0, hyphenIndex).Trim();


                Info.AppName =
                    hyphenIndex == -1
                    ? fullTitle.Trim()
                    : fullTitle.Substring(hyphenIndex + 3).Trim();
            }
        }


        return Info;
    }


    public struct FLInfo
    {
        public string AppName { get; set; }
        public string ProjectName { get; set; }
        public string AccurateVersion { get; set; }
    }
}