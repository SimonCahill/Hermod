using System;

namespace Hermod.Config {

    /// <summary>
    /// Contains information about errors occuring during parsing and/or retrieving of configurations.
    /// </summary>
    public class ConfigException: Exception {

        public ConfigException(string msg): base(msg) { }
    }
}

