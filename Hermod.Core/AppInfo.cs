using System;

namespace Hermod.Core {

    using System.IO;

    /// <summary>
    /// Static class containing basic information for and about the application.
    /// </summary>
    public static partial class AppInfo {

        /// <summary>
        /// The file extension used for freshly imported emails.
        /// </summary>
        public static string ImportedEmailExtension = ".eml";

        /// <summary>
        /// Gets the name of this application's main data directory.
        /// </summary>
        public static string HermodAppDirName => ".hermod";

        /// <summary>
        /// Gets the name of the application's config directory.
        /// </summary>
        public static string HermodAppCfgDirName => "cfg";

        /// <summary>
        /// Gets the name of the application's plugins directory.
        /// </summary>
        public static string HermodAppPluginDirName => "plugins";

        /// <summary>
        /// Gets the name of the application's log directory.
        /// Typically this is stored
        /// </summary>
        public static string HermodLogDirName => "log";

        /// <summary>
        /// Gets the application's base data directory.
        /// </summary>
        /// <returns>The base directory for the application's data.</returns>
        public static DirectoryInfo GetBaseHermodDirectory() {
            string? basePath = null;
            switch (Environment.OSVersion.Platform) {
                case PlatformID.Win32NT:
                    basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    break;
                default:
                    basePath = "/etc";
                    break;
                // leave room to diversify later
            }

            return new DirectoryInfo(Path.Combine(basePath, HermodAppDirName));
        }

        /// <summary>
        /// Gets the application's local data directory.
        /// </summary>
        /// <remarks>
        /// Data stored here will remain local to the computer and will not roam with the user.
        /// </remarks>
        /// <returns>A <see cref="DirectoryInfo"/ object pointing to the directory.></returns>
        public static DirectoryInfo GetLocalHermodDirectory() {
            string? basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            return new DirectoryInfo(Path.Combine(basePath, HermodAppDirName));
        }

    }
}

