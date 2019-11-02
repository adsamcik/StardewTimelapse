using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewTimelapse {
    /// <summary>The main entry point for the mod.</summary>
    public class ModEntry : Mod {
        private const string timelapseDirectoryBaseName = "timelapse";
        private const string farmMapName = "Farm";
        private const string captureExtension = "png";

        private DirectoryInfo exportDirectory;
        private DirectoryInfo timelapseDirectory;

        private bool isFarmLoaded;

        private FileSystemWatcher watcher = new FileSystemWatcher();

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper) {
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        }

        private void Initialize() {
            InitializeExportDirectory();
            SelectTimelapseDirectory();
            StartWatchingChanges();
        }

        private string GetDirectoryName() {
            var playerName = Game1.player.Name;
            var gameId = Game1.uniqueIDForThisGame;
            return $"{playerName}-{ gameId}";
        }

        private string GetScreenshotName() {
            var date = Game1.Date;
            var season = date.Season;
            var day = date.DayOfMonth;
            return $"{season}-{day}";
        }

        private void InitializeExportDirectory() {
            var stardewRoot = new DirectoryInfo(Constants.ExecutionPath);
            exportDirectory = stardewRoot.EnumerateDirectories("MapExport").FirstOrDefault();
            if (exportDirectory == null) {
                exportDirectory = stardewRoot.CreateSubdirectory("MapExport");
            }
        }

        private void SelectTimelapseDirectory() {
            var directoryName = $"{timelapseDirectoryBaseName}-{GetDirectoryName()}";
            timelapseDirectory = exportDirectory.CreateSubdirectory(directoryName);
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e) {
            Initialize();
            Helper.Events.Player.Warped += OnWarped;
        }


        private void OnWarped(object sender, WarpedEventArgs e) {
            if (e.NewLocation.Name == farmMapName) {
                Helper.Events.Player.Warped -= OnWarped;
                isFarmLoaded = true;
                CaptureMap();
            }
        }


        /// <summary>The method called after a new day starts.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDayStarted(object sender, DayStartedEventArgs e) {
            if (isFarmLoaded) {
                CaptureMap();
            }
        }

        private void MoveMapCapture(FileInfo capture) {
            var extension = capture.Extension;
            var desiredPath = Path.Combine(timelapseDirectory.FullName, $"{GetScreenshotName()}{extension}");
            var desiredFile = new FileInfo(desiredPath);
            if (!desiredFile.Exists) {
                capture.CopyTo(desiredFile.FullName);
            }
        }

        private void StartWatchingChanges() {
            //var mapExport = exportDirectory.EnumerateFiles().FirstOrDefault((file) => Path.GetFileNameWithoutExtension(file.Name) == farmMapName);
            watcher.Path = exportDirectory.FullName;
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite;
            watcher.Filter = $"{farmMapName}.{captureExtension}";
            watcher.Changed += OnFarmCaptureChanged;
            watcher.Created += OnFarmCaptureChanged;
            watcher.EnableRaisingEvents = true;
        }

        private void OnFarmCaptureChanged(object sender, FileSystemEventArgs e) {
            var fileName = Path.GetFileNameWithoutExtension(e.FullPath);
            if (fileName == farmMapName &&
                (e.ChangeType == WatcherChangeTypes.Created || e.ChangeType == WatcherChangeTypes.Changed)) {
                try {
                    MoveMapCapture(new FileInfo(e.FullPath));
                } catch (IOException) { }
            }
        }

        private void CaptureMap() {
            Helper.ConsoleCommands.Trigger("export", new[] { "Farm", "all" });
        }
    }
}
