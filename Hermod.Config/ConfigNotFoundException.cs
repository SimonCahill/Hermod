using System;

namespace Hermod.Config {

    /// <summary>
    /// Occurs when a requested config could not be found.
    /// </summary>
    public class ConfigNotFoundException: ConfigException {


        public ConfigNotFoundException(string configName, string msg): base(msg) {
            OffendingConfig = configName;
        }

		public override string ToString() {
			return $"{ Message }\nOffending config: { OffendingConfig }";
		}

        public string OffendingConfig { get; private set; }
	}
}

