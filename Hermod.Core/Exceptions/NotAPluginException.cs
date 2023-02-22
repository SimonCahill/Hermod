using System;

namespace Hermod.Core.Exceptions {

    /// <summary>
    /// Exception class that is thrown when an attempt is made to load a plugin which is not in an assembly.
    /// </summary>
    public class NotAPluginException: Exception {


        public NotAPluginException(FileInfo offendingFile): base("Attempted to load malformed or incompatible file type!") {
            OffendingFile = offendingFile;
        }

        /// <summary>
        /// The file that caused this error.
        /// </summary>
        public FileInfo OffendingFile { get; }

		public override string ToString() {
			return $"{ Message }\nOffending file: { OffendingFile.FullName }";
		}
	}
}

