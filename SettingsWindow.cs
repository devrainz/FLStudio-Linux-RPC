using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Adw;
using Gio;
using Gtk;

public static class SettingsWindow
{
    private const string SettingsArgument = "--settings";
    private const string ApplicationId = "com.devrainz.FLStudioRPC.Settings";

    public static void Launch(
        bool firstRun = false)
    {
        try
        {
            string executablePath =
                Environment.ProcessPath
                ?? Process.GetCurrentProcess().MainModule?.FileName
                ?? throw new InvalidOperationException(
                    "Could not determine the FLStudioRPC executable path."
                );

            ProcessStartInfo startInfo = new()
            {
                FileName = executablePath,
                UseShellExecute = false
            };

            string executableName =
                Path.GetFileNameWithoutExtension(
                    executablePath
                );

            if (
                string.Equals(
                    executableName,
                    "dotnet",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                string? entryAssemblyPath =
                    Assembly.GetEntryAssembly()?.Location;

                if (!string.IsNullOrWhiteSpace(entryAssemblyPath))
                {
                    startInfo.ArgumentList.Add(
                        entryAssemblyPath
                    );
                }
            }

            startInfo.ArgumentList.Add(SettingsArgument);

            if (firstRun)
            {
                startInfo.ArgumentList.Add(
                    "--first-run"
                );
            }

            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to open settings window", ex);
        }
    }

    public static int Run(
        bool firstRun)
    {
        try
        {
            ConfigSettings.SaveConfig(Program.ConfigPath);

            var application =
                Gtk.Application.New(
                    ApplicationId,
                    Gio.ApplicationFlags.FlagsNone
                );

            application.OnActivate +=
                (sender, _) =>
                {
                    var gtkApplication =
                        (Gtk.Application)sender;

                    var window =
                        Adw.ApplicationWindow.New(
                            gtkApplication
                        );

                    window.Title =
                        "FL Studio Discord RPC";

                    window.SetDefaultSize(
                        460,
                        620
                    );

                    window.Resizable =
                        false;

                    var root =
                        Gtk.Box.New(
                            Orientation.Vertical,
                            16
                        );

                    root.SetMarginTop(28);
                    root.SetMarginBottom(24);
                    root.SetMarginStart(28);
                    root.SetMarginEnd(28);

                    var logo =
                        Gtk.Image.NewFromIconName(
                            "flstudio"
                        );

                    logo.SetPixelSize(96);
                    logo.Halign = Align.Center;
                    root.Append(logo);

                    var title =
                        Gtk.Label.New(
                            "FL Studio Discord RPC"
                        );

                    title.AddCssClass("title-1");
                    title.Halign = Align.Center;
                    title.Justify = Justification.Center;
                    root.Append(title);

                    var subtitle =
                        Gtk.Label.New(
                            "Configure how the Linux tray and Discord Rich Presence behave."
                        );

                    subtitle.AddCssClass("dim-label");
                    subtitle.Wrap = true;
                    subtitle.Justify = Justification.Center;
                    subtitle.Halign = Align.Center;
                    root.Append(subtitle);

                    var settingsLabel =
                        Gtk.Label.New(
                            "Settings"
                        );

                    settingsLabel.AddCssClass("heading");
                    settingsLabel.Halign = Align.Start;
                    root.Append(settingsLabel);

                    AddSwitchRow(
                        root,
                        "Secret mode",
                        "Hide the current FL Studio project name from Discord.",
                        ConfigValues.SecretMode,
                        active => ConfigValues.SecretMode = active
                    );

                    AddSwitchRow(
                        root,
                        "Show timestamp",
                        "Show when the current FL Studio session started.",
                        ConfigValues.ShowTimestamp,
                        active => ConfigValues.ShowTimestamp = active
                    );

                    AddSwitchRow(
                        root,
                        "Use accurate FL Studio version",
                        "Try to include the detected FL Studio version in the activity.",
                        ConfigValues.AccurateVersion,
                        active => ConfigValues.AccurateVersion = active
                    );

                    AddSwitchRow(
                        root,
                        "Show this window on startup",
                        "Open these settings the first time the app starts.",
                        ConfigValues.ShowSettingsOnStartup,
                        active => ConfigValues.ShowSettingsOnStartup = active
                    );

                    var closeButton =
                        Gtk.Button.NewWithLabel(
                            "Close"
                        );

                    closeButton.AddCssClass(
                        "suggested-action"
                    );

                    closeButton.Halign =
                        Align.End;

                    closeButton.OnClicked +=
                        (_, _) =>
                        {
                            SaveSettings();

                            CompleteFirstRun(
                                firstRun
                            );

                            window.Close();
                        };

                    root.Append(closeButton);

                    window.OnCloseRequest +=
                        (_, _) =>
                        {
                            SaveSettings();

                            CompleteFirstRun(
                                firstRun
                            );

                            return false;
                        };

                    window.Content =
                        root;

                    window.Present();
                };

            return application.RunWithSynchronizationContext(null);
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to run libadwaita settings window", ex);
            return 1;
        }
    }

    private static void AddSwitchRow(
        Gtk.Box parent,
        string title,
        string description,
        bool initialValue,
        Action<bool> onChanged)
    {
        var row =
            Gtk.Box.New(
                Orientation.Horizontal,
                12
            );

        row.MarginTop = 4;
        row.MarginBottom = 4;

        var text =
            Gtk.Box.New(
                Orientation.Vertical,
                2
            );

        text.Hexpand = true;

        var titleLabel =
            Gtk.Label.New(title);

        titleLabel.Halign = Align.Start;
        titleLabel.Xalign = 0;

        var descriptionLabel =
            Gtk.Label.New(description);

        descriptionLabel.AddCssClass("dim-label");
        descriptionLabel.Wrap = true;
        descriptionLabel.Halign = Align.Start;
        descriptionLabel.Xalign = 0;

        text.Append(titleLabel);
        text.Append(descriptionLabel);

        var toggle =
            Gtk.Switch.New();

        toggle.Active =
            initialValue;

        toggle.Valign =
            Align.Center;

        toggle.OnNotify +=
            (_, args) =>
            {
                if (args.Pspec.GetName() != "active")
                {
                    return;
                }

                onChanged(toggle.Active);
                SaveSettings();
            };

        row.Append(text);
        row.Append(toggle);
        parent.Append(row);
    }

    private static void SaveSettings()
    {
        ConfigSettings.SaveCurrentConfig(
            Program.ConfigPath
        );
    }

    private static void CompleteFirstRun(
        bool firstRun)
    {
        if (!firstRun)
        {
            return;
        }

        ConfigValues.HasCompletedInitialSetup =
            true;

        SaveSettings();
    }
}
