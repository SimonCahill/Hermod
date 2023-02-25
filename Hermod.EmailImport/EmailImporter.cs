using System;

namespace Hermod.EmailImport {

    using Core.Attributes;
    using Config;
    using Core.Commands;
    using Core.Delegation;
    using PluginFramework;

    /// <summary>
    /// EmailImporter plugin.
    /// </summary>
    [Plugin(nameof(EmailImporter), "0.0.1", "Simon Cahill", "contact@simonc.eu", "https://simonc.eu")]
    public partial class EmailImporter: Plugin {

        IPluginDelegator? m_pluginDelegator = null;

        public EmailImporter(): base(nameof(EmailImporter), new Version(0, 0, 1)) {
            PluginCommands = new List<ICommand> {
                new TerminalCommand(
                    "get-domains", "Gets a list of all known domains",
                    "This command retrieves a list of all domains currently\n" +
                    "known to the Hermod EmailImporter.\n" +
                    "Usage: get-domains [tld [tld]] # only with these TLDs",
                    Handle_GetDomains
                ),
                new TerminalCommand(
                    "get-domain", "Gets information about a single domain",
                    "This command retrieves a single domain and information about it.\n" +
                    "Usage: get-domain <domain-name>",
                    Handle_GetSingleDomain
                ),
                new TerminalCommand(
                    "add-domain", "Adds a new domain to the system",
                    "This command allows a new domain to be added to Hermod.\n" +
                    "Usage: add-domain <domain-name [domain-name [...]]> # domain-name is expected as tld.domain[.subdomain]",
                    Handle_AddDomain
                ),
                new TerminalCommand(
                    "remove-domain", "Removes one or more domains from the system",
                    "This command removes one or more domains from Hermod.\n" +
                    "Usage: remove-domain <domain-name [domain-name [...]]> # domain-name is expected as tld.domain[.subdomain]",
                    Handle_RemoveDomain
                ),
                new TerminalCommand(
                    "get-users", "Retrieves a list of all users in a domain",
                    "This command retrieves a list of all users allocated to a domain.\n" +
                    "Usage: get-users [domain-name] # domain-name is expected as tld.domain[.subdomain]",
                    Handle_GetUsers
                ),
                new TerminalCommand(
                    "get-user", "Gets detailled information about a single user",
                    "This command allows retrievel of detailled information,\n" +
                    "such as email address, password hash, salt and account type\n" +
                    "in a single domain.\n" +
                    "Usage: get-user <domain> <user>",
                    Handle_GetUser
                ),
                new TerminalCommand(
                    "add-user", "Adds a single user to a domain",
                    "This command adds a single user to a single domain,\n" +
                    "provided info: user name, password, password salt, and account type.\n" +
                    "Usage: add-user <domain> <user> <password> <account type>\n" +
                    "Note: this is the only time Hermod will know of the user's cleartext password!",
                    Handle_AddUser
                ),
                new TerminalCommand(
                    "remove-user", "Removes a single user from a domain",
                    "This command allows a single user to be removed from a domain.\n" +
                    "!! WARNING !! This process is irreversible!\n" +
                    "Usage: remove-user <domain> <user>",
                    Handle_RemoveUser
                )
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

