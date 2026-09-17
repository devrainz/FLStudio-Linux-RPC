using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

                using var writer = new StreamWriter(LogFilePath, true);

                string line =
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

                writer.WriteLine(line);
                System.Console.Error.WriteLine(line);
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
        Log("ERROR", $"{message}: {ex.Message}");
        Log("ERROR", $"  Stack trace: {ex.StackTrace}");
    }
}

public static class Utils
{
    private static string? _lastWindowTitle;
    private static string? _cachedFLStudioVersion;
    private static DateTime _nextVersionLookupUtc = DateTime.MinValue;
    private static bool _xwininfoFailureLogged;
    private static bool _xpropFailureLogged;
    private static FLInfo _lastKnownFLInfo;
    private static int _consecutiveMissingDetections;

    private const int RequiredMissingDetections = 3;

    private static readonly string[] FLStudioProcessNames =
    {
        "FL.exe",
        "FL32.exe",
        "FL64.exe",
        "FLStudio.exe"
    };

    private static readonly Regex FLStudioMainWindowPattern =
        new Regex(
            @"^(?:(?<project>.+?)\s+-\s+)?FL\s+Studio(?:\s+(?<version>(?:20\d{2}|\d{1,2})(?:\.\d+)*))?\s*$",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant |
            RegexOptions.Compiled
        );

    private static readonly Regex FLStudioVersionPattern =
        new Regex(
            @"\bFL\s+Studio(?:\s+|[/_-])(?<version>(?:20\d{2}|\d{1,2})(?:\.\d+)*)(?=$|[^\d])",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant |
            RegexOptions.Compiled
        );

    private static readonly Regex HexWindowIdPattern =
        new Regex(
            @"0x[0-9a-f]+",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant |
            RegexOptions.Compiled
        );

    private static readonly Regex ProcessIdPattern =
        new Regex(
            @"=\s*(?<pid>\d+)",
            RegexOptions.CultureInvariant |
            RegexOptions.Compiled
        );

    private sealed class WindowInfo
    {
        public ulong Id { get; init; }

        public string Title { get; init; } = string.Empty;

        public string WindowClass { get; init; } = string.Empty;
    }

    private sealed class FLWindowState
    {
        public string? SelectedTitle { get; init; }

        public string? MainTitle { get; init; }

        public bool HasFLStudioWindow { get; init; }
    }

    private static bool IsFLStudioProcessCommandLine(string commandLine)
    {
        string[] arguments =
            commandLine.Split(
                new[]
                {
                    '\0',
                    ' ',
                    '\t',
                    '\n',
                    '\r'
                },
                StringSplitOptions.RemoveEmptyEntries
            );

        foreach (string argument in arguments)
        {
            string fileName =
                Path.GetFileName(
                    argument.Trim('"')
                        .Replace('\\', '/')
                );

            foreach (string processName in FLStudioProcessNames)
            {
                if (string.Equals(
                        fileName,
                        processName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsFLStudioProcess(int processId)
    {
        try
        {
            string commandLine =
                File.ReadAllText(
                    Path.Combine(
                        "/proc",
                        processId.ToString(CultureInfo.InvariantCulture),
                        "cmdline"
                    )
                );

            return IsFLStudioProcessCommandLine(commandLine);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsFLStudioWindowClass(string windowClass)
    {
        string[] classNames =
            windowClass.Split(
                new[]
                {
                    ' ',
                    '\t',
                    ',',
                    '"',
                    '\''
                },
                StringSplitOptions.RemoveEmptyEntries
            );

        foreach (string className in classNames)
        {
            string fileName =
                Path.GetFileName(
                    className.Replace('\\', '/')
                );

            foreach (string processName in FLStudioProcessNames)
            {
                if (
                    string.Equals(
                        fileName,
                        processName,
                        StringComparison.OrdinalIgnoreCase
                    )
                    ||
                    string.Equals(
                        Path.GetFileNameWithoutExtension(fileName),
                        Path.GetFileNameWithoutExtension(processName),
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool IsFLStudioRunning()
    {
        try
        {
            foreach (string processDirectory in
                Directory.EnumerateDirectories("/proc"))
            {
                string processId = Path.GetFileName(processDirectory);

                if (!int.TryParse(processId, out _))
                {
                    continue;
                }

                string commandLinePath =
                    Path.Combine(processDirectory, "cmdline");

                try
                {
                    if (
                        File.Exists(commandLinePath)
                        &&
                        IsFLStudioProcessCommandLine(
                            File.ReadAllText(commandLinePath)
                        )
                    )
                    {
                        return true;
                    }
                }
                catch
                {
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(
                "Could not scan Linux processes for FL Studio",
                ex
            );
        }

        return false;
    }

    private static string? RunCommand(
        string fileName,
        string arguments,
        out int exitCode,
        out string standardError)
    {
        exitCode = -1;
        standardError = string.Empty;

        ProcessStartInfo startInfo =
            new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

        using Process? process = Process.Start(startInfo);

        if (process == null)
        {
            return null;
        }

        string standardOutput =
            process.StandardOutput.ReadToEnd();

        standardError =
            process.StandardError.ReadToEnd();

        process.WaitForExit();
        exitCode = process.ExitCode;

        return standardOutput;
    }

    private static int FindClosingQuote(string value, int openingQuote)
    {
        for (int index = openingQuote + 1; index < value.Length; index++)
        {
            if (value[index] != '"')
            {
                continue;
            }

            int slashCount = 0;

            for (
                int slash = index - 1;
                slash >= 0 && value[slash] == '\\';
                slash--
            )
            {
                slashCount++;
            }

            if (slashCount % 2 == 0)
            {
                return index;
            }
        }

        return -1;
    }

    private static WindowInfo? ParseXwininfoLine(string line)
    {
        string trimmed = line.TrimStart();

        if (!trimmed.StartsWith(
                "0x",
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        int idEnd =
            trimmed.IndexOfAny(
                new[]
                {
                    ' ',
                    '\t'
                }
            );

        if (idEnd == -1)
        {
            return null;
        }

        string idText =
            trimmed.Substring(
                2,
                idEnd - 2
            );

        if (!ulong.TryParse(
                idText,
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out ulong windowId))
        {
            return null;
        }

        int firstQuote = line.IndexOf('"');

        if (firstQuote == -1)
        {
            return null;
        }

        int secondQuote =
            FindClosingQuote(
                line,
                firstQuote
            );

        if (secondQuote == -1)
        {
            return null;
        }

        int classStart =
            line.IndexOf(
                ": (",
                secondQuote,
                StringComparison.Ordinal
            );

        if (classStart == -1)
        {
            return null;
        }

        classStart += 3;

        int classEnd =
            line.IndexOf(
                ')',
                classStart
            );

        if (classEnd == -1)
        {
            return null;
        }

        string title =
            line.Substring(
                firstQuote + 1,
                secondQuote - firstQuote - 1
            )
            .Replace("\\\"", "\"")
            .Replace("\\\\", "\\")
            .Trim();

        string windowClass =
            line.Substring(
                classStart,
                classEnd - classStart
            );

        return new WindowInfo
        {
            Id = windowId,
            Title = title,
            WindowClass = windowClass
        };
    }

    private static ulong? GetActiveWindowId()
    {
        try
        {
            string? output =
                RunCommand(
                    "xprop",
                    "-root _NET_ACTIVE_WINDOW",
                    out int exitCode,
                    out string error
                );

            if (
                exitCode != 0
                ||
                string.IsNullOrWhiteSpace(output)
            )
            {
                if (!_xpropFailureLogged)
                {
                    string reason =
                        string.IsNullOrWhiteSpace(error)
                        ? "xprop could not read the active X11/XWayland window"
                        : error.Trim();

                    Logger.Warn(
                        $"Focused FL Studio window detection is unavailable: {reason}"
                    );

                    _xpropFailureLogged = true;
                }

                return null;
            }

            Match match =
                HexWindowIdPattern.Match(output);

            if (
                !match.Success
                ||
                !ulong.TryParse(
                    match.Value.Substring(2),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out ulong windowId
                )
                ||
                windowId == 0
            )
            {
                return null;
            }

            _xpropFailureLogged = false;

            return windowId;
        }
        catch (Exception ex)
        {
            if (!_xpropFailureLogged)
            {
                Logger.Warn(
                    $"Focused FL Studio window detection is unavailable: {ex.Message}"
                );

                _xpropFailureLogged = true;
            }

            return null;
        }
    }

    private static int? GetWindowProcessId(ulong windowId)
    {
        try
        {
            string? output =
                RunCommand(
                    "xprop",
                    $"-id 0x{windowId:x} _NET_WM_PID",
                    out int exitCode,
                    out _
                );

            if (
                exitCode != 0
                ||
                string.IsNullOrWhiteSpace(output)
            )
            {
                return null;
            }

            Match match =
                ProcessIdPattern.Match(output);

            return
                match.Success
                &&
                int.TryParse(
                    match.Groups["pid"].Value,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int processId
                )
                ? processId
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsIgnoredFLStudioWindow(string title)
    {
        return
            string.IsNullOrWhiteSpace(title)
            ||
            title.Contains(
                "FLHintBarForm",
                StringComparison.OrdinalIgnoreCase
            )
            ||
            string.Equals(
                title,
                "Default IME",
                StringComparison.OrdinalIgnoreCase
            );
    }

    private static FLWindowState GetFLStudioWindowState()
    {
        try
        {
            string? output =
                RunCommand(
                    "xwininfo",
                    "-root -tree",
                    out int exitCode,
                    out string error
                );

            if (
                exitCode != 0
                ||
                string.IsNullOrWhiteSpace(output)
            )
            {
                if (!_xwininfoFailureLogged)
                {
                    string reason =
                        string.IsNullOrWhiteSpace(error)
                        ? $"xwininfo exited with code {exitCode}"
                        : error.Trim();

                    Logger.Error(
                        $"FL Studio xwininfo detection failed: {reason}"
                    );

                    _xwininfoFailureLogged = true;
                }

                return new FLWindowState();
            }

            _xwininfoFailureLogged = false;

            var ownedWindows =
                new List<WindowInfo>();

            WindowInfo? mainWindow = null;

            foreach (string line in output.Split('\n'))
            {
                WindowInfo? window =
                    ParseXwininfoLine(line);

                if (
                    window == null
                    ||
                    IsIgnoredFLStudioWindow(
                        window.Title
                    )
                    ||
                    !IsFLStudioWindowClass(
                        window.WindowClass
                    )
                )
                {
                    continue;
                }

                ownedWindows.Add(window);

                if (
                    mainWindow == null
                    &&
                    FLStudioMainWindowPattern.IsMatch(
                        window.Title
                    )
                )
                {
                    mainWindow = window;
                }
            }

            ulong? activeWindowId =
                GetActiveWindowId();

            WindowInfo? activeFLWindow = null;

            if (activeWindowId.HasValue)
            {
                foreach (WindowInfo window in ownedWindows)
                {
                    if (window.Id == activeWindowId.Value)
                    {
                        activeFLWindow = window;
                        break;
                    }
                }

                if (activeFLWindow == null)
                {
                    foreach (string line in output.Split('\n'))
                    {
                        WindowInfo? window =
                            ParseXwininfoLine(line);

                        if (
                            window == null
                            ||
                            window.Id != activeWindowId.Value
                            ||
                            IsIgnoredFLStudioWindow(
                                window.Title
                            )
                        )
                        {
                            continue;
                        }

                        int? ownerProcessId =
                            GetWindowProcessId(
                                window.Id
                            );

                        if (
                            ownerProcessId.HasValue
                            &&
                            IsFLStudioProcess(
                                ownerProcessId.Value
                            )
                        )
                        {
                            activeFLWindow = window;
                        }

                        break;
                    }
                }
            }

            WindowInfo? selectedWindow;

            if (activeFLWindow != null)
            {

                selectedWindow = activeFLWindow;
            }
            else if (activeWindowId.HasValue)
            {

                selectedWindow = mainWindow;
            }
            else
            {

                selectedWindow =
                    ownedWindows.Count > 0
                    ? ownedWindows[
                        ownedWindows.Count - 1
                    ]
                    : mainWindow;
            }

            string? selectedTitle =
                selectedWindow?.Title;

            if (!string.Equals(
                    selectedTitle,
                    _lastWindowTitle,
                    StringComparison.Ordinal))
            {
                Logger.Info(
                    $"FL Studio window changed: '{_lastWindowTitle}' -> '{selectedTitle}'"
                );

                _lastWindowTitle =
                    selectedTitle;
            }

            return new FLWindowState
            {
                SelectedTitle =
                    selectedTitle,

                MainTitle =
                    mainWindow?.Title,

                HasFLStudioWindow =
                    ownedWindows.Count > 0
                    ||
                    activeFLWindow != null
            };
        }
        catch (Exception ex)
        {
            if (!_xwininfoFailureLogged)
            {
                Logger.Error(
                    "FL Studio window detection failed",
                    ex
                );

                _xwininfoFailureLogged = true;
            }

            return new FLWindowState();
        }
    }

    public static string? GetMainWindowsTitleByProcessNames(
        params string[] processNames)
    {

        _ = processNames;

        return
            GetFLStudioWindowState()
                .SelectedTitle;
    }

    private static string? ExtractFLStudioVersion(
        string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        string normalizedSource =
            source.Replace("\\040", " ")
                .Replace('\\', '/');

        Match match =
            FLStudioVersionPattern.Match(
                normalizedSource
            );

        return match.Success
            ? match.Groups["version"].Value
            : null;
    }

    private static string? FindFLStudioVersionFromProcess()
    {
        try
        {
            foreach (string processDirectory in
                Directory.EnumerateDirectories("/proc"))
            {
                string processId =
                    Path.GetFileName(
                        processDirectory
                    );

                if (!int.TryParse(processId, out _))
                {
                    continue;
                }

                string commandLine;

                try
                {
                    commandLine =
                        File.ReadAllText(
                            Path.Combine(
                                processDirectory,
                                "cmdline"
                            )
                        );
                }
                catch
                {
                    continue;
                }

                if (!IsFLStudioProcessCommandLine(commandLine))
                {
                    continue;
                }

                string? version =
                    ExtractFLStudioVersion(
                        commandLine
                    );

                if (!string.IsNullOrWhiteSpace(version))
                {
                    return version;
                }

                try
                {
                    version =
                        ExtractFLStudioVersion(
                            File.ReadAllText(
                                Path.Combine(
                                    processDirectory,
                                    "maps"
                                )
                            )
                        );

                    if (!string.IsNullOrWhiteSpace(version))
                    {
                        return version;
                    }
                }
                catch
                {

                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(
                "Could not detect the FL Studio version from its process",
                ex
            );
        }

        return null;
    }

    private static string? ExtractProjectName(
        string? mainTitle)
    {
        if (string.IsNullOrWhiteSpace(mainTitle))
        {
            return null;
        }

        Match match =
            FLStudioMainWindowPattern.Match(
                mainTitle
            );

        if (
            !match.Success
            ||
            !match.Groups["project"].Success
        )
        {
            return null;
        }

        string projectName =
            match.Groups["project"]
                .Value
                .Trim();

        return
            string.IsNullOrWhiteSpace(projectName)
            ? null
            : projectName;
    }

    private static string? GetFocusedFLStudioContext(
        FLWindowState windowState)
    {
        string? selectedTitle =
            windowState.SelectedTitle;

        if (string.IsNullOrWhiteSpace(selectedTitle))
        {
            return ExtractProjectName(
                windowState.MainTitle
            );
        }

        Match selectedMainMatch =
            FLStudioMainWindowPattern.Match(
                selectedTitle
            );

        if (selectedMainMatch.Success)
        {
            return ExtractProjectName(
                selectedTitle
            );
        }

        return selectedTitle;
    }

    public static FLInfo GetFLInfo()
    {
        FLInfo info = new FLInfo();

        bool processDetected =
            IsFLStudioRunning();

        FLWindowState windowState =
            GetFLStudioWindowState();

        bool flStudioDetected =
            processDetected
            ||
            windowState.HasFLStudioWindow;

        if (!flStudioDetected)
        {
            _consecutiveMissingDetections++;

            if (
                _consecutiveMissingDetections
                    < RequiredMissingDetections
                &&
                !string.IsNullOrWhiteSpace(
                    _lastKnownFLInfo.AppName
                )
            )
            {
                return _lastKnownFLInfo;
            }

            _lastWindowTitle = null;
            _cachedFLStudioVersion = null;
            _nextVersionLookupUtc = DateTime.MinValue;
            _lastKnownFLInfo = default;

            return info;
        }

        _consecutiveMissingDetections = 0;

        string? version =
            ExtractFLStudioVersion(
                windowState.MainTitle
            )
            ??
            ExtractFLStudioVersion(
                windowState.SelectedTitle
            );

        if (!string.IsNullOrWhiteSpace(version))
        {
            if (!string.Equals(
                    version,
                    _cachedFLStudioVersion,
                    StringComparison.OrdinalIgnoreCase))
            {
                Logger.Info(
                    $"Detected FL Studio version {version} from its window title"
                );
            }

            _cachedFLStudioVersion =
                version;
        }
        else if (
            string.IsNullOrWhiteSpace(
                _cachedFLStudioVersion
            )
            &&
            DateTime.UtcNow >=
                _nextVersionLookupUtc
        )
        {
            _nextVersionLookupUtc =
                DateTime.UtcNow.AddSeconds(30);

            _cachedFLStudioVersion =
                FindFLStudioVersionFromProcess();

            if (!string.IsNullOrWhiteSpace(
                    _cachedFLStudioVersion))
            {
                Logger.Info(
                    $"Detected FL Studio version {_cachedFLStudioVersion} from its process"
                );
            }
        }

        info.AppName =
            string.IsNullOrWhiteSpace(
                _cachedFLStudioVersion
            )
            ? "FL Studio"
            : $"FL Studio {_cachedFLStudioVersion}";

        info.ProjectName =
            GetFocusedFLStudioContext(
                windowState
            );

        if (
            string.IsNullOrWhiteSpace(
                info.ProjectName
            )
            &&
            string.IsNullOrWhiteSpace(
                windowState.SelectedTitle
            )
            &&
            string.IsNullOrWhiteSpace(
                windowState.MainTitle
            )
            &&
            !string.IsNullOrWhiteSpace(
                _lastKnownFLInfo.ProjectName
            )
        )
        {
            info.ProjectName =
                _lastKnownFLInfo.ProjectName;
        }

        _lastKnownFLInfo = info;

        return info;
    }

    public struct FLInfo
    {
        public string? AppName { get; set; }

        public string? ProjectName { get; set; }
    }
}