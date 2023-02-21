using System;

namespace Hermod.Core {

    using System.IO;

    public static class Extensions {

        public static FileInfo GetSubFile(this DirectoryInfo dirInfo, params string[] fileNames) {
            return new FileInfo(Path.Combine(dirInfo.FullName, Path.Combine(fileNames)));
        }

    }
}

