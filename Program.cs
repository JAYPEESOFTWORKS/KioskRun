using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace KioskRun {
    class Program {
        public static void Main(string[] args) {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            UpdateText("Initializing...");
        }

        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();

        public static async Task<string> CheckOnlineVersionAsync(string versionUrl, string fallbackVersion) {
            using HttpClient client = new HttpClient();
            try {
                string html = await client.GetStringAsync(versionUrl);
                return !string.IsNullOrEmpty(html) ? html.Replace("\r", "").Replace("\n", "") : fallbackVersion;
            }
            catch (HttpRequestException) {
                return fallbackVersion;
            }
            catch (Exception) {
                return fallbackVersion;
            }
        }

        public static void UpdateText(string text) {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                (desktop.MainWindow as MainWindow)?.UpdateDisplayText(text);
            }
        }

        public static void ExitApp() {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                desktop.Shutdown();
            }
        }

        public static async Task<bool> DownloadAndExtractZip(string apiUrl, string projectId, string version, string downloadPath) {
            string zipUrl = $"{apiUrl}/{projectId}/{version}/bundle.zip";
            string zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");

            try {
                UpdateText($"Downloading {version}...");
                await Task.Delay(2000);

                using HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(zipUrl);
                response.EnsureSuccessStatusCode();

                await using FileStream fileStream = new FileStream(zipPath, FileMode.Create);
                await response.Content.CopyToAsync(fileStream);

                Directory.CreateDirectory(downloadPath);
                ZipFile.ExtractToDirectory(zipPath, downloadPath);
                File.Delete(zipPath);

                Console.WriteLine("Successfully downloaded and extracted the ZIP file.");
                return true;
            }
            catch (Exception ex) {
                Console.WriteLine($"Failed to download or extract ZIP file: {ex.Message}");
                return false;
            }
        }

        public static bool RunLastKnownGoodVersion(string storagePath, string projectId, string appExe) {
            string lastKnownGoodVersion = GetLastKnownGoodVersion(storagePath, projectId);

            if (!string.IsNullOrEmpty(lastKnownGoodVersion)) {
                string lastKnownGoodPath = Path.Combine(storagePath, projectId, lastKnownGoodVersion, appExe);
                if (File.Exists(lastKnownGoodPath)) {
                    try {
                        Console.WriteLine($"Running last known good version: {lastKnownGoodVersion}");
                        Process.Start(lastKnownGoodPath);
                        HideMainWindow();
                        Environment.Exit(0);
                        return true;
                    }
                    catch (Exception ex) {
                        Console.WriteLine($"Failed to start the last known good version: {ex.Message}");
                    }
                }
                else {
                    Console.WriteLine("No last known good version found.");
                }
            }
            else {
                Console.WriteLine("No last known good version found.");
            }
            return false;
        }

        public static void RunLocalVersion(string localExecutablePath) {
            if (File.Exists(localExecutablePath)) {
                try {
                    Console.WriteLine("Running local version.");
                    Process.Start(localExecutablePath);
                    HideMainWindow();
                    Environment.Exit(0);
                }
                catch (Exception ex) {
                    Console.WriteLine($"Failed to start the local version: {ex.Message}");
                }
            }
            else {
                Console.WriteLine("No local version found.");
            }
        }

        private static void HideMainWindow() {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                (desktop.MainWindow as MainWindow)?.Hide();
            }
        }

        public static string GetLastKnownGoodVersion(string storagePath, string projectId) {
            string filePath = Path.Combine(storagePath, projectId, "lastKnownGoodVersion.txt");

            return File.Exists(filePath) ? File.ReadAllText(filePath) : string.Empty;
        }

        public static void SetLastKnownGoodVersion(string storagePath, string projectId, string version) {
            string directoryPath = Path.Combine(storagePath, projectId);
            string filePath = Path.Combine(directoryPath, "lastKnownGoodVersion.txt");

            if (!Directory.Exists(directoryPath)) {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(filePath, version);
        }
    }
}
