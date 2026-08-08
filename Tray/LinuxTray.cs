using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Tmds.DBus;

[DBusInterface("org.kde.StatusNotifierWatcher")]
public interface IStatusNotifierWatcher : IDBusObject
{
    Task RegisterStatusNotifierItemAsync(string service);
}

[DBusInterface("org.kde.StatusNotifierItem")]
public interface IStatusNotifierItem : IDBusObject
{
    Task<object> GetAsync(string prop);
    Task<IDictionary<string, object>> GetAllAsync();
    Task SetAsync(string prop, object val);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    Task ActivateAsync(int x, int y);
    Task SecondaryActivateAsync(int x, int y);
    Task ContextMenuAsync(int x, int y);
}

[DBusInterface("com.canonical.dbusmenu")]
public interface IDbusMenu : IDBusObject
{
    Task<(uint revision, (int id, IDictionary<string, object> properties, object[] children) layout)>
        GetLayoutAsync(int parentId, int recursionDepth, string[] propertyNames);
    Task<(int id, IDictionary<string, object> properties)[]> GetGroupPropertiesAsync(int[] ids, string[] propertyNames);
    Task<object> GetPropertyAsync(int id, string property);
    Task EventAsync(int id, string eventId, object data, uint timestamp);
    Task<int[]> EventGroupAsync((int id, string eventId, object data, uint timestamp)[] events);
    Task<bool> AboutToShowAsync(int id);
    Task<(int[] updatesNeeded, int[] idErrors)> AboutToShowGroupAsync(int[] ids);
    Task<object> GetAsync(string prop);
    Task<IDictionary<string, object>> GetAllAsync();
    Task SetAsync(string prop, object val);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    Task<IDisposable> WatchLayoutUpdatedAsync(Action<(uint revision, int parent)> handler);
}

public sealed class LinuxTray : IDisposable
{
    private const string ItemObjectPath = "/StatusNotifierItem";
    private const string MenuObjectPath = "/MenuBar";
    private const string WatcherName = "org.kde.StatusNotifierWatcher";
    private const string WatcherPath = "/StatusNotifierWatcher";

    private Connection? _connection;
    private string? _busName;
    private StatusNotifierItem? _item;
    private DbusMenu? _menu;
    private IStatusNotifierWatcher? _watcherProxy;
    private IDisposable? _watcherSubscription;

    public event Action? Activated;
    public event Action? SecondaryActivated;
    public event Action? MenuRequested;
    public event Action? QuitRequested;

    public async Task StartAsync()
    {
        if (_connection != null)
            return;

        try
        {
            Logger.Info("Initializing Linux StatusNotifier tray...");

            _connection = new Connection(Address.Session);
            var connectionInfo = await _connection.ConnectAsync();

            Logger.Info("Connected to user D-Bus session");

            _busName = connectionInfo.LocalName;

            Logger.Info($"D-Bus unique name: {_busName}");

            _item = new StatusNotifierItem(this);
            _menu = new DbusMenu(_connection);
            _menu.QuitRequested += () => QuitRequested?.Invoke();

            await _connection.RegisterObjectAsync(_menu);
            Logger.Info($"DbusMenu object registered at {MenuObjectPath}");

            await _connection.RegisterObjectAsync(_item);
            Logger.Info($"StatusNotifierItem object registered at {ItemObjectPath}");

            _watcherProxy = _connection.CreateProxy<IStatusNotifierWatcher>(WatcherName, WatcherPath);

            _watcherSubscription = await _connection.ResolveServiceOwnerAsync(
                WatcherName,
                OnWatcherOwnerChanged,
                onError: ex => Logger.Error("StatusNotifierWatcher owner-watch error", ex)
            );

            Logger.Info("StatusNotifierWatcher owner monitoring established");

            await RegisterItemWithWatcherAsync();

            Logger.Info("Linux StatusNotifier tray started");
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to initialize Linux tray", ex);
            Dispose();
        }
    }

    private void OnWatcherOwnerChanged(ServiceOwnerChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.NewOwner))
        {
            Logger.Info($"StatusNotifierWatcher available (owner: {e.NewOwner}) - registering item");
            _ = RegisterItemWithWatcherAsync();
        }
        else
        {
            Logger.Info("StatusNotifierWatcher owner lost (host down or restarting)");
        }
    }

    private async Task RegisterItemWithWatcherAsync()
    {
        if (_watcherProxy == null)
        {
            Logger.Error("Cannot register StatusNotifierItem: watcher proxy is null");
            return;
        }

        if (_busName == null)
        {
            Logger.Error("Cannot register StatusNotifierItem: D-Bus bus name is null");
            return;
        }

        try
        {
            Logger.Info($"Registering StatusNotifierItem with watcher as {_busName}");
            await _watcherProxy.RegisterStatusNotifierItemAsync(_busName);
            Logger.Info($"StatusNotifierItem successfully registered: {_busName}");
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to register StatusNotifierItem with watcher", ex);
        }
    }

    public void Dispose()
    {
        try
        {
            _watcherSubscription?.Dispose();
            _watcherSubscription = null;
            _watcherProxy = null;
            _connection?.Dispose();
            _connection = null;
            _item = null;
            _menu = null;
            _busName = null;

            Logger.Info("Linux StatusNotifier tray stopped");
        }
        catch (Exception ex)
        {
            Logger.Error("Error shutting down Linux tray", ex);
        }
    }

    private sealed class StatusNotifierItem : IStatusNotifierItem, IDBusObject
    {
        private readonly LinuxTray _owner;

        public StatusNotifierItem(LinuxTray owner)
        {
            _owner = owner;
        }

        public ObjectPath ObjectPath => new ObjectPath(ItemObjectPath);

        private static readonly Lazy<(int width, int height, byte[] data)[]> IconPixmapCache = new(LoadIcon);

        private static (int width, int height, byte[] data)[] LoadIcon()
        {
            try
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Icons", "hicolor", "128x128", "apps", "flstudio.png");
                using var image = Image.Load<Rgba32>(iconPath);

                int width = image.Width;
                int height = image.Height;
                byte[] data = new byte[width * height * 4];
                int index = 0;

                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        var row = accessor.GetRowSpan(y);
                        for (int x = 0; x < row.Length; x++)
                        {
                            var pixel = row[x];
                            data[index++] = pixel.A;
                            data[index++] = pixel.R;
                            data[index++] = pixel.G;
                            data[index++] = pixel.B;
                        }
                    }
                });

                return new[] { (width, height, data) };
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to decode tray icon for IconPixmap", ex);
                return Array.Empty<(int, int, byte[])>();
            }
        }

        private IDictionary<string, object> GetAllProperties()
        {
            return new Dictionary<string, object>
            {
                { "Category", "ApplicationStatus" },
                { "Id", "FLStudioRPC" },
                { "Title", "FL Studio Discord RPC" },
                { "Status", "Active" },
                { "WindowId", 0 },
                { "IconName", "flstudio" },
                { "IconThemePath", Path.Combine(AppContext.BaseDirectory, "Icons") },
                { "IconPixmap", IconPixmapCache.Value },
                { "AttentionIconName", "" },
                { "AttentionMovieName", "" },
                { "OverlayIconName", "" },
                { "ToolTip", ("", Array.Empty<(int, int, byte[])>(), "FL Studio Discord RPC", "") },
                { "ItemIsMenu", false },
                { "Menu", new ObjectPath(MenuObjectPath) }
            };
        }

        public Task<object> GetAsync(string prop)
        {
            var all = GetAllProperties();
            return Task.FromResult(all.TryGetValue(prop, out var value) ? value : null!);
        }

        public Task<IDictionary<string, object>> GetAllAsync() => Task.FromResult(GetAllProperties());

        public Task SetAsync(string prop, object val) => Task.CompletedTask;

        public Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler)
            => Task.FromResult<IDisposable>(new NoopDisposable());

        public Task ActivateAsync(int x, int y)
        {
            Logger.Info($"StatusNotifierItem.Activate({x}, {y})");
            _owner.Activated?.Invoke();
            return Task.CompletedTask;
        }

        public Task SecondaryActivateAsync(int x, int y)
        {
            Logger.Info($"StatusNotifierItem.SecondaryActivate({x}, {y})");
            _owner.SecondaryActivated?.Invoke();
            return Task.CompletedTask;
        }

        public Task ContextMenuAsync(int x, int y)
        {
            Logger.Info($"StatusNotifierItem.ContextMenu({x}, {y})");
            _owner.MenuRequested?.Invoke();
            return Task.CompletedTask;
        }

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }

    private sealed class DbusMenu : IDbusMenu, IDBusObject
    {
        private readonly Connection _connection;

        public DbusMenu(Connection connection)
        {
            _connection = connection;
        }

        private const int IdSecretMode = 1;
        private const int IdAutostart = 2;
        private const int IdSeparator = 3;
        private const int IdAbout = 4;
        private const int IdExit = 5;

        private static readonly HashSet<int> KnownIds = new() { IdSecretMode, IdAutostart, IdSeparator, IdAbout, IdExit };
        private const string AboutUrl = "https://github.com/devrainz/FLStudio-Linux-RPC";

        private static readonly string AutostartDesktopPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "autostart", "flstudiorpc.desktop"
        );

        public ObjectPath ObjectPath => new ObjectPath(MenuObjectPath);

        public event Action? QuitRequested;
        public event Action<(uint revision, int parent)>? LayoutUpdated;

        public Task<IDisposable> WatchLayoutUpdatedAsync(Action<(uint revision, int parent)> handler)
        {
            LayoutUpdated += handler;
            return Task.FromResult<IDisposable>(new EventUnsubscriber(() => LayoutUpdated -= handler));
        }

        private static readonly object[] EmptyChildren = Array.Empty<object>();
        private uint _revision = 1;

        private static bool IsAutostartEnabled() => File.Exists(AutostartDesktopPath);

        private static void SetAutostartEnabled(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    var directory = Path.GetDirectoryName(AutostartDesktopPath);
                    if (!string.IsNullOrEmpty(directory))
                        Directory.CreateDirectory(directory);

                    string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";

                    string desktopEntry =
                        "[Desktop Entry]\n" +
                        "Type=Application\n" +
                        "Name=FL Studio Discord RPC\n" +
                        $"Exec=\"{exePath}\"\n" +
                        "Hidden=false\n" +
                        "NoDisplay=false\n" +
                        "X-GNOME-Autostart-enabled=true\n";

                    File.WriteAllText(AutostartDesktopPath, desktopEntry);
                    Logger.Info("Autostart enabled, wrote " + AutostartDesktopPath);
                }
                else
                {
                    if (File.Exists(AutostartDesktopPath))
                    {
                        File.Delete(AutostartDesktopPath);
                        Logger.Info("Autostart disabled, removed " + AutostartDesktopPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to update autostart entry", ex);
            }
        }

        private (int id, IDictionary<string, object> properties, object[] children) BuildLayout()
        {
            var secretMode = (
                id: IdSecretMode,
                properties: (IDictionary<string, object>)new Dictionary<string, object>
                {
                    { "label", "Secret Mode (Hide Project Name)" },
                    { "enabled", true },
                    { "visible", true },
                    { "type", "standard" },
                    { "toggle-type", "checkmark" },
                    { "toggle-state", ConfigValues.SecretMode ? 1 : 0 }
                },
                children: EmptyChildren
            );

            var autostart = (
                id: IdAutostart,
                properties: (IDictionary<string, object>)new Dictionary<string, object>
                {
                    { "label", "Start with Linux" },
                    { "enabled", true },
                    { "visible", true },
                    { "type", "standard" },
                    { "toggle-type", "checkmark" },
                    { "toggle-state", IsAutostartEnabled() ? 1 : 0 }
                },
                children: EmptyChildren
            );

            var separator = (
                id: IdSeparator,
                properties: (IDictionary<string, object>)new Dictionary<string, object>
                {
                    { "type", "separator" },
                    { "visible", true }
                },
                children: EmptyChildren
            );

            var about = (
                id: IdAbout,
                properties: (IDictionary<string, object>)new Dictionary<string, object>
                {
                    { "label", "About" },
                    { "enabled", true },
                    { "visible", true },
                    { "type", "standard" }
                },
                children: EmptyChildren
            );

            var exit = (
                id: IdExit,
                properties: (IDictionary<string, object>)new Dictionary<string, object>
                {
                    { "label", "Exit" },
                    { "enabled", true },
                    { "visible", true },
                    { "type", "standard" }
                },
                children: EmptyChildren
            );

            var rootProperties = new Dictionary<string, object> { { "children-display", "submenu" } };

            return (0, rootProperties, new object[] { secretMode, autostart, separator, about, exit });
        }

        public Task<(uint revision, (int id, IDictionary<string, object> properties, object[] children) layout)>
            GetLayoutAsync(int parentId, int recursionDepth, string[] propertyNames)
        {
            Logger.Info($"DbusMenu.GetLayout(parentId={parentId}, recursionDepth={recursionDepth})");
            var layout = BuildLayout();
            return Task.FromResult<(uint, (int, IDictionary<string, object>, object[]))>((_revision, layout));
        }

        public Task<(int id, IDictionary<string, object> properties)[]> GetGroupPropertiesAsync(int[] ids, string[] propertyNames)
        {
            Logger.Info($"DbusMenu.GetGroupProperties(ids=[{string.Join(",", ids)}])");
            var (_, _, children) = BuildLayout();
            var result = new List<(int, IDictionary<string, object>)>();

            foreach (var child in children)
            {
                var (id, properties, _) = ((int, IDictionary<string, object>, object[]))child;
                if (Array.IndexOf(ids, id) >= 0)
                    result.Add((id, properties));
            }

            return Task.FromResult(result.ToArray());
        }

        public Task<object> GetPropertyAsync(int id, string property)
        {
            Logger.Info($"DbusMenu.GetProperty(id={id}, property={property})");
            var (_, _, children) = BuildLayout();

            foreach (var child in children)
            {
                var (childId, properties, _) = ((int, IDictionary<string, object>, object[]))child;
                if (childId == id && properties.TryGetValue(property, out var value))
                    return Task.FromResult(value);
            }

            return Task.FromResult<object>(null!);
        }

        private void HandleClickEvent(int id, string eventId)
        {
            if (eventId != "clicked")
            {
                Logger.Info("DbusMenu event ignored (eventId != 'clicked'): " + eventId);
                return;
            }

            switch (id)
            {
                case IdSecretMode:
                    ConfigValues.SecretMode = !ConfigValues.SecretMode;
                    ConfigSettings.SaveCurrentConfig(Program.ConfigPath);
                    _revision++;
                    Logger.Info($"Secret Mode toggled: {ConfigValues.SecretMode}, revision={_revision}");
                    LayoutUpdated?.Invoke((_revision, 0));
                    break;

                case IdAutostart:
                    SetAutostartEnabled(!IsAutostartEnabled());
                    _revision++;
                    Logger.Info($"Autostart toggled, revision={_revision}");
                    LayoutUpdated?.Invoke((_revision, 0));
                    break;

                case IdAbout:
                    OpenPath(AboutUrl);
                    break;

                case IdExit:
                    QuitRequested?.Invoke();
                    break;
            }
        }

        public Task EventAsync(int id, string eventId, object data, uint timestamp)
        {
            Logger.Info($"DbusMenu.Event(id={id}, eventId={eventId})");
            HandleClickEvent(id, eventId);
            return Task.CompletedTask;
        }

        public Task<int[]> EventGroupAsync((int id, string eventId, object data, uint timestamp)[] events)
        {
            Logger.Info($"DbusMenu.EventGroup(count={events.Length})");
            var idErrors = new List<int>();

            foreach (var (id, eventId, data, timestamp) in events)
            {
                Logger.Info($"DbusMenu.EventGroup item: id={id}, eventId={eventId}");

                if (!KnownIds.Contains(id))
                {
                    idErrors.Add(id);
                    continue;
                }

                HandleClickEvent(id, eventId);
            }

            return Task.FromResult(idErrors.ToArray());
        }

        public Task<bool> AboutToShowAsync(int id)
        {
            Logger.Info($"DbusMenu.AboutToShow(id={id})");
            return Task.FromResult(true);
        }

        public Task<(int[] updatesNeeded, int[] idErrors)> AboutToShowGroupAsync(int[] ids)
        {
            Logger.Info($"DbusMenu.AboutToShowGroup(ids=[{string.Join(",", ids)}])");
            var idErrors = ids.Where(id => !KnownIds.Contains(id)).ToArray();
            var updatesNeeded = ids.Where(id => KnownIds.Contains(id)).ToArray();
            return Task.FromResult((updatesNeeded, idErrors));
        }

        public Task<object> GetAsync(string prop)
        {
            var all = GetAllPropertiesInternal();
            return Task.FromResult(all.TryGetValue(prop, out var value) ? value : null!);
        }

        public Task<IDictionary<string, object>> GetAllAsync() => Task.FromResult(GetAllPropertiesInternal());

        private IDictionary<string, object> GetAllPropertiesInternal()
        {
            return new Dictionary<string, object>
            {
                { "Version", (uint)3 },
                { "TextDirection", "ltr" },
                { "Status", "normal" },
                { "IconThemePath", Array.Empty<string>() }
            };
        }

        public Task SetAsync(string prop, object val) => Task.CompletedTask;

        public Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler)
            => Task.FromResult<IDisposable>(new NoopDisposable());

        private static void OpenPath(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = $"\"{path}\"",
                    UseShellExecute = false
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to open path via xdg-open: " + path, ex);
            }
        }

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose() { }
        }

        private sealed class EventUnsubscriber : IDisposable
        {
            private Action? _unsubscribe;

            public EventUnsubscriber(Action unsubscribe)
            {
                _unsubscribe = unsubscribe;
            }

            public void Dispose()
            {
                _unsubscribe?.Invoke();
                _unsubscribe = null;
            }
        }
    }
}
