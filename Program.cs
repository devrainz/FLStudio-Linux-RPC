using DiscordRPC;
using DiscordRPC.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Console = Colorful.Console;

using static ConfigSettings;

using static ConfigValues;

using static Utils;

public static class Program
{
    public static DiscordRpcClient? _Client;

    private static readonly object
        RpcClientLock = new object();

    private static DateTime
        _nextRpcAttemptUtc = DateTime.MinValue;

    private static bool
        _rpcConnectionEstablished;

    private static readonly TimeSpan
        RpcRetryDelay = TimeSpan.FromSeconds(5);

    public static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(
            Environment.SpecialFolder.UserProfile
        ),
        ".config",
        "FLStudioRPC",
        "fls_rpc_config.json"
    );

    private static Mutex? _mutex;

    private static LinuxTray? _tray;

    private static readonly RichPresence _RPC = new RichPresence()
    {
        Details = "",
        State = "",
        Assets = new Assets()
        {
            LargeImageKey = "fl_studio_logo",
        }
    };


    public static void
        ResetRPCClient(
            string reason)
    {
        DiscordRpcClient? client;

        lock (RpcClientLock)
        {
            client = _Client;
            _Client = null;
            _rpcConnectionEstablished = false;
            _nextRpcAttemptUtc =
                DateTime.UtcNow + RpcRetryDelay;
        }

        Logger.Warn(
            "Resetting Discord RPC client: " + reason
        );

        try
        {
            client?.ClearPresence();
        }
        catch (Exception ex)
        {
            Logger.Error(
                "Failed to clear Discord presence during reset",
                ex
            );
        }

        try
        {
            client?.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Error(
                "Failed to dispose Discord RPC client during reset",
                ex
            );
        }
    }

    public static void
        MarkRPCConnectionEstablished()
    {
        lock (RpcClientLock)
        {
            _rpcConnectionEstablished = true;
        }
    }

    static void InitializeRPC()
    {
        lock (RpcClientLock)
        {
            if (
                _Client != null &&
                _rpcConnectionEstablished
            )
            {
                return;
            }

            if (
                _Client != null &&
                DateTime.UtcNow <
                _nextRpcAttemptUtc
            )
            {
                return;
            }
        }

        if (_Client != null)
        {
            ResetRPCClient(
                "client was no longer initialized"
            );
        }

        DiscordRpcClient client =
            new DiscordRpcClient(
                ClientID,
                -1,
                null,
                false
            );

        client.SkipIdenticalPresence = true;

        string discordLogPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile
            ),
            ".config",
            "FLStudioRPC",
            "logs",
            "discord_rpc.log"
        );

        string? discordLogDirectory =
            Path.GetDirectoryName(discordLogPath);

        if (!string.IsNullOrEmpty(discordLogDirectory))
        {
            Directory.CreateDirectory(
                discordLogDirectory
            );
        }

        client.Logger =
            new DiscordRPC.Logging.FileLogger(
                discordLogPath
            )
            {
                Level =
                    DiscordRPC.Logging.LogLevel.Warning
            };


        client.OnReady += Events.OnReady;
        client.OnClose += Events.OnClose;
        client.OnError += Events.OnError;
        client.OnConnectionEstablished +=
            Events.OnConnectionEstablished;
        client.OnConnectionFailed +=
            Events.OnConnectionFailed;
        client.OnPresenceUpdate +=
            Events.OnPresenceUpdate;

        lock (RpcClientLock)
        {
            _Client = client;
            _nextRpcAttemptUtc =
                DateTime.UtcNow + RpcRetryDelay;
        }

        try
        {
            client.Initialize();
        }
        catch (Exception ex)
        {
            Logger.Error(
                "Discord RPC initialization failed",
                ex
            );

            ResetRPCClient(
                "initialization exception"
            );
        }
    }


    static void SetupTray()
    {
        try
        {
            Logger.Info(
                "Starting Linux StatusNotifier tray..."
            );

            _tray = new LinuxTray();

            _tray.Activated += () =>
            {
                Logger.Info(
                    "Tray icon activated"
                );
            };

            _tray.SecondaryActivated += () =>
            {
                Logger.Info(
                    "Tray icon secondary activated"
                );
            };

            _tray.MenuRequested += () =>
            {
                Logger.Info(
                    "Tray context menu requested"
                );
            };

            _tray.QuitRequested += () =>
            {
                Logger.Info(
                    "Quit requested from tray"
                );

                Environment.Exit(0);
            };

            /*
             * Start the StatusNotifierItem.
             */
            _tray.StartAsync()
                .GetAwaiter()
                .GetResult();

            Logger.Info(
                "Linux StatusNotifier tray initialization complete"
            );
        }
        catch (Exception ex)
        {
            Logger.Error(
                "Failed to start Linux tray",
                ex
            );
        }
    }


    static void RunRPCLoop()
    {
        SaveConfig(ConfigPath);

        Logger.Info(
            "RPC loop started"
        );

        bool wasRunning = false;


        while (true)
        {
            try
            {
                FLInfo FLStudioData =
                    GetFLInfo();


                bool isRunning =
                    !string.IsNullOrEmpty(
                        FLStudioData.AppName
                    )
                    ||
                    !string.IsNullOrEmpty(
                        FLStudioData.ProjectName
                    );


                if (isRunning)
                {
                    if (!wasRunning)
                    {
                        Logger.Info(
                            "FL Studio detected, initializing Discord RPC"
                        );

                        if (ShowTimestamp)
                        {
                            _RPC.Timestamps =
                                new Timestamps()
                                {
                                    Start =
                                        DateTime.UtcNow
                                };
                        }


                        wasRunning = true;
                    }

                    InitializeRPC();


                    _RPC.Details =
                        FLStudioData.AppName;

                    _RPC.State =
                        FLStudioData.ProjectName
                        ??
                        "Empty project";


                    if (SecretMode)
                    {
                        _RPC.State =
                            "Working on a hidden project";
                    }


                    if (_Client != null)
                    {
                        try
                        {
                            _Client.Invoke();

                            _Client.SetPresence(
                                _RPC
                            );
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(
                                "Discord RPC update failed",
                                ex
                            );

                            ResetRPCClient(
                                "presence update exception"
                            );
                        }
                    }
                }
                else
                {
                    if (wasRunning)
                    {
                        Logger.Info(
                            "FL Studio closed, clearing Discord presence"
                        );


                        ResetRPCClient(
                            "FL Studio closed"
                        );

                        wasRunning = false;
                    }
                }


                Thread.Sleep(
                    UpdateInterval
                );
            }
            catch (Exception ex)
            {
                Logger.Error(
                    "RPC loop exception",
                    ex
                );
            }
        }
    }


    static void Main(string[] args)
    {
        bool createdNew;


        _mutex = new Mutex(
            true,
            "FLStudioRPC_SingleInstance",
            out createdNew
        );


        if (!createdNew)
        {
            Console.WriteLine(
                "FL Studio Discord RPC is already running."
            );

            return;
        }


        /*
         * Initialize the Linux tray FIRST.
         *
         * This is independent from FL Studio.
         *
         * Therefore:
         *
         * FL Studio open   -> tray exists
         * FL Studio closed -> tray still exists
         */
        SetupTray();


        /*
         * Run the existing Discord RPC detection loop.
         */
        Thread rpcThread =
            new Thread(
                RunRPCLoop
            )
            {
                IsBackground = true
            };


        rpcThread.Start();


        rpcThread.Join();


        /*
         * Cleanup.
         */
        _tray?.Dispose();

        _Client?.Dispose();

        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
    }
}
