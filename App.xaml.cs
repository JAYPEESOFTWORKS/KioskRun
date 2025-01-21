using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Text.Json;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace KioskRun {
    public class App : Application {
        private string? projectId;
        private string? apiUrl;
        private string? desiredVersion;
        private string? storagePath;
        private string? appExe;
        private int splashWaitSeconds;
        private bool configFileMissing = false;
        private string configFilePath = string.Empty;

        public override void Initialize() {
            LoadConfiguration();
            AvaloniaXamlLoader.Load(this);
        }

        private async void MsgBox(string message) {
            await MessageBoxManager.GetMessageBoxStandard("KioskRun", message).ShowAsync();
        }

        private void LoadConfiguration() {            
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            configFilePath = basePath + "\\kioskrun_settings.json";
            configFileMissing = !File.Exists(configFilePath);
            if (configFileMissing){                
                return;
            }
            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("kioskrun_settings.json")
                .Build();
            projectId = config["PROJECT_ID"] ?? string.Empty;
            apiUrl = config["API_URL"] ?? string.Empty;
            desiredVersion = config["DESIRED_VERSION"] ?? string.Empty;
            storagePath = config["STORAGE_PATH"] ?? string.Empty;
            storagePath = storagePath.Replace("\\", "/");
            appExe = config["APP_EXE"] ?? string.Empty;
            splashWaitSeconds = int.TryParse(config["SPLASH_WAIT_SECONDS"], out int waitSeconds) ? waitSeconds : 3;
        }

        public static void UpdateAppSettings(string filePath, string key, string value) {
            string json = File.ReadAllText(filePath);
            var jsonDocument = JsonDocument.Parse(json);
            using var jsonWriter = new Utf8JsonWriter(File.OpenWrite(filePath), new JsonWriterOptions { Indented = true });

            using JsonDocument document = JsonDocument.Parse(json);
            jsonWriter.WriteStartObject();
            foreach (JsonProperty element in document.RootElement.EnumerateObject()) {
                if (element.NameEquals(key))
                    jsonWriter.WriteString(key, value);
                else
                    element.WriteTo(jsonWriter);
            }
            jsonWriter.WriteEndObject();
        }

        private string GetLocalInstallPath() =>
            string.IsNullOrEmpty(storagePath) || string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(appExe)
                ? string.Empty
                : Path.Combine(storagePath, projectId, "local", appExe);

        public override void OnFrameworkInitializationCompleted() {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                desktop.MainWindow = new MainWindow();
                MainWindowLoaded();
            }

            base.OnFrameworkInitializationCompleted();
        }

        public MainWindow? GetMainWindow() {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow is MainWindow mainWindow) {
                return mainWindow;
            }

            return null;
        }

        private async void MainWindowLoaded() {
            await Task.Delay(splashWaitSeconds * 1000);

            if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(projectId) ||
                string.IsNullOrEmpty(desiredVersion) || string.IsNullOrEmpty(appExe)) {
                if (configFileMissing) {
                    Program.UpdateText("Missing " + configFilePath + "...");
                } else {
                    Program.UpdateText("Invalid config!");
                }                
                return;
            }

            await PerformDelayedOperations(apiUrl, GetVersionUrl(), projectId, desiredVersion,
                GetDownloadPath(), GetExePath(), appExe, GetLocalInstallPath());
        }

        private string GetVersionUrl() =>
            string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(projectId)
                ? string.Empty
                : $"{apiUrl}/{projectId}/version.html";

        private string GetBundleUrl() =>
            string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(desiredVersion)
                ? string.Empty
                : $"{apiUrl}/{projectId}/{desiredVersion}/bundle.zip";

        private string GetDownloadPath() =>
            string.IsNullOrEmpty(storagePath) || string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(desiredVersion)
                ? string.Empty
                : Path.Combine(storagePath, projectId, desiredVersion).Replace("\\", "/");

        private string GetExePath() =>
            string.IsNullOrEmpty(storagePath) || string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(desiredVersion) || string.IsNullOrEmpty(appExe)
                ? string.Empty
                : Path.Combine(GetDownloadPath(), appExe).Replace("\\", "/");

        public static async Task PerformDelayedOperations(string appUrl, string versionUrl, string projectId,
            string configVersion, string downloadPath, string executablePath, string appExe, string localExecutablePath) {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow is MainWindow mainWindow) {
                mainWindow.UpdateDisplayText("Checking...");
                string cfg = configVersion.Replace("\r", "").Replace("\n", "");
                string desiredVersion = await Program.CheckOnlineVersionAsync(versionUrl, cfg);

                if (cfg != desiredVersion) {
                    mainWindow.UpdateDisplayText($"Online version mismatch, updating config to{desiredVersion}...");
                    UpdateAppSettings("appsettings.json", "DESIRED_VERSION", desiredVersion);
                }
                else {
                    mainWindow.UpdateDisplayText($"Desired version {desiredVersion}...");
                }

                await Task.Delay(2000);

                bool downloadSuccess = !Directory.Exists(downloadPath) || !File.Exists(executablePath)
                    ? await Program.DownloadAndExtractZip(appUrl, projectId, desiredVersion, downloadPath)
                    : true;

                bool processStarted = false;
                if (downloadSuccess) {
                    try {
                        using var process = new Process {
                            StartInfo = { FileName = executablePath }
                        };
                        if (process.Start() && !process.HasExited) {
                            processStarted = true;
                            Program.SetLastKnownGoodVersion(downloadPath, projectId, desiredVersion);
                            Dispatcher.UIThread.Post(() => mainWindow?.Hide());
                            Environment.Exit(0);
                        }
                    }
                    catch (Exception ex) {
                        Console.WriteLine($"Failed to start the application: {ex.Message}");
                        if (!processStarted && !Program.RunLastKnownGoodVersion(downloadPath, projectId, appExe)) {
                            Program.RunLocalVersion(localExecutablePath);
                        }
                    }
                }
                else {
                    if (!processStarted && !Program.RunLastKnownGoodVersion(downloadPath, projectId, appExe)) {
                        Program.RunLocalVersion(localExecutablePath);
                    }
                }
            }
        }
    }
}