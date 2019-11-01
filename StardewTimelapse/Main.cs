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
        private const string timelapseDirectoryName = "timelapse";
        private const string farmMapName = "Farm";

        private DirectoryInfo exportDirectory;
        private DirectoryInfo timelapseDirectory;

        private int nextIndex;
        private bool farmLoaded;

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
            SelectTimelapseDirectory();
            UpdateLastIndex();
        }

        private void SelectTimelapseDirectory() {
            var stardewRoot = new DirectoryInfo(Constants.ExecutionPath);
            exportDirectory = stardewRoot.EnumerateDirectories("MapExport").FirstOrDefault();
            if (exportDirectory == null) {
                exportDirectory = stardewRoot.CreateSubdirectory("MapExport");
            }

            var playerName = Game1.player.Name;
            timelapseDirectory = exportDirectory.CreateSubdirectory($"{timelapseDirectoryName}-{playerName}");
            UpdateLastIndex();
        }

        private void UpdateLastIndex() {
            var fileEnumeration = timelapseDirectory.EnumerateFiles();
            int nextIndex = 0;

            if (fileEnumeration.Any()) {
                var nextIndexCalc = fileEnumeration.Max((file) => {
                    var nameNoExtension = Path.GetFileNameWithoutExtension(file.Name);

                    if (int.TryParse(nameNoExtension, out var index)) {
                        return index;
                    } else {
                        return int.MinValue;
                    }
                });

                if (nextIndexCalc > 0) {
                    nextIndex = nextIndexCalc;
                }
            }

            this.nextIndex = nextIndex;
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e) {
            Initialize();
            Helper.Events.Player.Warped += OnWarped;
        }


        private void OnWarped(object sender, WarpedEventArgs e) {
            if (e.NewLocation.Name == farmMapName) {
                Helper.Events.Player.Warped -= OnWarped;
                farmLoaded = true;
                CaptureMap();
            }
        }


        /// <summary>The method called after a new day starts.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDayStarted(object sender, DayStartedEventArgs e) {
            if (farmLoaded) {
                CaptureMap();
            }
        }

        private void CaptureMap() {
            Helper.ConsoleCommands.Trigger("export", new[] { "Farm", "all" });
            var mapExport = exportDirectory.EnumerateFiles().FirstOrDefault((file) => Path.GetFileNameWithoutExtension(file.Name) == farmMapName);
            if (mapExport != null) {
                var extension = mapExport.Extension;
                mapExport.MoveTo(Path.Combine(timelapseDirectory.FullName, $"{nextIndex++}{extension}"));
            }
        }
    }
}
