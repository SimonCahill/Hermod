using System;

namespace Hermod.EmailImport {

    using Data;
    using Exceptions;

    using Config;
    using Core;
    using Core.Attributes;
    using Core.Commands;
    using Core.Delegation;
    using PluginFramework;

    using System.Text;
    using System.Buffers.Text;

    /// <summary>
    /// EmailImporter plugin.
    /// </summary>
    [Plugin(nameof(EmailImporter), "0.0.1", "Simon Cahill", "contact@simonc.eu", "https://simonc.eu")]
    public partial class EmailImporter: Plugin {

        #region Publish Topics
        const string DomainAddedTopic = "/hermod/domains/added";
        const string DomainRemovedTopic = "/hermod/domains/removed";
        const string UserAddedTopic = "/hermod/domains/user/added/{domain}";
        const string UserRemovedTopic = "/hermod/domains/user/removed/{domain}";
        #endregion

        #region Subscribe Topics
        const string AddDomainRequestTopic = "/hermod/domains/add";
        const string AddDomainUserRequestTopic = "/hermod/domains/user/add/+";
        const string RemoveDomainRequestTopic = "/hermod/domains/remove";
        const string RemoveDomainUserRequestTopic = "/hermod/domains/user/remove/+";
        #endregion

        private readonly string[] m_subscribeTopics = {
            AddDomainRequestTopic,
            AddDomainUserRequestTopic,
            RemoveDomainRequestTopic,
            RemoveDomainUserRequestTopic
        };

        volatile bool m_keepThreadAlive = false;
        DatabaseConnector? m_dbConnector;
        Thread? m_importThread = null;

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
                ),
                new TerminalCommand(
                    "load-account-cfg", "Loads the account configs into memory",
                    "(re-)Loads all accounts from the database into memory, so Hermod can use them.",
                    Handle_LoadAccountConfig
                ),
                new TerminalCommand(
                    "save-account-cfg", "Dumps the account configs from memory",
                    "Encrypts and them dumps all domains, accounts, and their current states to the\n" +
                    "configured data source.",
                    Handle_SaveAccountConfig
                ),
                new TerminalCommand(
                    "do-import", "Synchronously imports all known domains",
                    "If no arguments are passed, do-import will synchronously import all emails\n" +
                    "from all known domains and accounts.\n" +
                    "This process cannot be stopped once started!\n" +
                    "Usage: do-import [domains]",
                    Handle_DoImport
                )
            };
        }

        public override void OnConfigChanged(ConfigChangedEventArgs e) { }

        public override void OnConfigLoaded() { }

        public override void OnLoad(IPluginDelegator pluginDelegator) {
            base.OnLoad(pluginDelegator);

            if (PluginDelegator?.GetApplicationConfig<bool>("Accounts.UseDatabase") == true) {
                throw new Exception("MySqlDatabaseConnector is not usable in this version!");
            } else if (PluginDelegator?.GetApplicationConfig<bool>("Accounts.UseJsonFile") == true) {
                SetupJsonDbConnector();
                m_dbConnector?.Connect();
            } else {
                throw new InvalidDataSourceException();
            }

            PluginDelegator.Debug("Subscribing all topics...");
            pluginDelegator.SubscribeTopics(m_subscribeTopics);

            PluginDelegator.Information("Email Importer has loaded!");
        }

        public override void OnStart() {
            m_keepThreadAlive = true;
            m_importThread = new Thread(DoWork);
            m_importThread.Start();
        }

        public override void OnStop() {
            PluginDelegator?.Information("Stopping worker threads...");
            m_keepThreadAlive = false;
        }

        private void GetEncryptionData(ref byte[] encKey, ref byte[] initVec) {
            var tmpKey = PluginDelegator?.GetApplicationConfig<byte[]>("Accounts.EncryptionKey");
            if (tmpKey is null || tmpKey.Length == 0) {
                PluginDelegator?.Information("Found invalid encryption keys! Generating new encryption data...");
                JsonDatabaseConnector.GenerateNewAesKey(out encKey, out initVec);

                PluginDelegator?.TrySetApplicationConfig("Accounts.EncryptionKey", encKey);
                PluginDelegator?.TrySetApplicationConfig("Accounts.EncryptionInitVec", initVec);
                return;
            }

            if (encKey is null) { encKey = new byte[tmpKey.Length]; }
            Array.Copy(tmpKey, encKey, tmpKey.Length);

            tmpKey = PluginDelegator?.GetApplicationConfig<byte[]>("Accounts.EncryptionInitVec");
            if (tmpKey is null || tmpKey.Length == 0) {
                PluginDelegator?.Information("Found invalid encryption keys! Generating new encryption data...");
                JsonDatabaseConnector.GenerateNewAesKey(out encKey, out initVec);

                PluginDelegator?.TrySetApplicationConfig("Accounts.EncryptionKey", encKey);
                PluginDelegator?.TrySetApplicationConfig("Accounts.EncryptionInitVec", initVec);
                return;
            }

            if (initVec is null) { initVec = new byte[tmpKey.Length]; }
            Array.Copy(tmpKey, initVec, tmpKey.Length);
        }

        private void SetupJsonDbConnector() {
            var filePath = PluginDelegator?.GetApplicationConfig<string?>("Accounts.JsonFileInfo.FilePath");
            if (filePath is null) {
                filePath = AppInfo.GetLocalHermodDirectory().GetSubFile(".accounts.json").FullName;
            }

            byte[] encKey = null;
            byte[] initVec = null;

            GetEncryptionData(ref encKey, ref initVec);
            m_dbConnector = new JsonDatabaseConnector(
                new FileInfo(filePath),
                encKey, initVec
            );
        }
    }
}

