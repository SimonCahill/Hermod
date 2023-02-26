using System;

namespace Hermod.EmailImport.Exceptions {

    /// <summary>
    /// Exception class that is thrown when an invalid data source was set in the application configuration.
    /// </summary>
    public class InvalidDataSourceException: Exception {

        /// <summary>
        /// Default constructor; throws a standardised message.
        /// </summary>
        public InvalidDataSourceException(): base("An invalid data source was set in the configuration! Plugin cannot load!") { }

        public InvalidDataSourceException(string msg) : base(msg) { }
    }
}

