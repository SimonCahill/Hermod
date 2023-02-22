using System;

namespace Hermod.EmailImport {

    using Core.Attributes;
    using Hermod.Config;
    using Hermod.Core.Delegation;
    using PluginFramework;

    /// <summary>
    /// EmailImporter plugin.
    /// </summary>
    [Plugin(nameof(EmailImporter), "0.0.1", "Simon Cahill", "contact@simonc.eu", "https://simonc.eu")]
    public class EmailImporter: Plugin {

        IPluginDelegator? m_pluginDelegator = null;

        public EmailImporter(): base(nameof(EmailImporter), new Version(0, 0, 1)) {
            PluginCommands = new List<Core.Commands.ICommand> {

            };
        }

        public override void OnConfigChanged(ConfigChangedEventArgs e) { }

        public override void OnConfigLoaded() { }

        public override void OnLoad(IPluginDelegator pluginDelegator) {
            m_pluginDelegator = pluginDelegator;


            m_pluginDelegator.Information("Email Importer has loaded!");
        }

        public override void OnStart() {
            
        }

        public override void OnStop() {
            
        }
    }
}

