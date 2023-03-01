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

        const string GetDomainTopicAllSuffix    = "all";
        const string AddDomainTopic             = "/hermod/domain/add"; /// The topic EmailImport subscribes to add a new domain
        const string GetDomainTopic             = "/hermod/domain/get/+"; /// The topic EmailImport subscribes when another plugin requests a domain
        const string RemoveDomainTopic          = "/hermod/domain/remove/+"; /// The topic EmailImport subscribes to when a domain shall be removed.
        const string GetDomainResponseTopic     = "/hermod/domain/response"; /// The topic EmailImport publishes to when a topic was requested
        const string GetDomainUserTopic         = "/hermod/user/get"; /// The topic EmailImport subscribes to when a user is requested
        const string AddDomainUserTopic         = "/hermod/user/add/+"; /// The topic EmailImport subscribes to when a user is to be added to a domain
        const string RemoveDomainUserTopic      = "/hermod/user/remove"; /// The topic EmailImport subscribes to when a user is to be removed from a domain

        private readonly string[] m_subscribeTopics = {
            AddDomainTopic, GetDomainTopic, RemoveDomainTopic, GetDomainUserTopic,
            AddDomainUserTopic, RemoveDomainUserTopic
        };

        volatile bool m_keepThreadAlive = false;
        DatabaseConnector? m_dbConnector;
        IPluginDelegator? m_pluginDelegator = null;
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
                )
            };
        }

        public override void OnConfigChanged(ConfigChangedEventArgs e) { }

        public override void OnConfigLoaded() { }

        public override void OnLoad(IPluginDelegator pluginDelegator) {
            m_pluginDelegator = pluginDelegator;

            if (m_pluginDelegator.GetApplicationConfig<bool>("Accounts.UseDatabase")) {
                // dynamic dbInfo = m_pluginDelegator.GetApplicationConfig<object>("Accounts.DatabaseInfo");
                // m_dbConnector = new MySqlDatabaseConnector(
                //     dbInfo.Host,
                //     dbInfo.DatabaseUser,
                //     dbInfo.DatabasePass,
                //     dbInfo.DatabaseName,
                //     pluginDelegator
                // );
                throw new Exception("MySqlDatabaseConnector is not usable in this version!");
            } else if (m_pluginDelegator.GetApplicationConfig<bool>("Accounts.UseJsonFile")) {
                var filePath = m_pluginDelegator.GetApplicationConfig<string?>("Accounts.JsonFileInfo.FilePath");
                if (filePath is null) {
                    filePath = AppInfo.GetLocalHermodDirectory().GetSubFile(".accounts.json").FullName;
                }

                byte[] encKey = null;
                byte[] initVec = null;

                void GetEncryptionData(ref byte[] encKey, ref byte[] initVec) {
                    var tmpKey = m_pluginDelegator?.GetApplicationConfig<byte[]>("Accounts.EncryptionKey");
                    if (tmpKey is null || tmpKey.Length == 0) {
                        m_pluginDelegator?.Information("Found invalid encryption keys! Generating new encryption data...");
                        JsonDatabaseConnector.GenerateNewAesKey(out encKey, out initVec);

                        m_pluginDelegator?.TrySetApplicationConfig("Accounts.EncryptionKey", encKey);
                        m_pluginDelegator?.TrySetApplicationConfig("Accounts.EncryptionInitVec", initVec);
                        return;
                    }

                    if (encKey is null) { encKey = new byte[tmpKey.Length]; }
                    Array.Copy(tmpKey, encKey, tmpKey.Length);

                    tmpKey = m_pluginDelegator?.GetApplicationConfig<byte[]>("Accounts.EncryptionInitVec");
                    if (tmpKey is null || tmpKey.Length == 0) {
                        m_pluginDelegator?.Information("Found invalid encryption keys! Generating new encryption data...");
                        JsonDatabaseConnector.GenerateNewAesKey(out encKey, out initVec);

                        m_pluginDelegator?.TrySetApplicationConfig("Accounts.EncryptionKey", encKey);
                        m_pluginDelegator?.TrySetApplicationConfig("Accounts.EncryptionInitVec", initVec);
                        return;
                    }

                    if (initVec is null) { initVec = new byte[tmpKey.Length]; }
                    Array.Copy(tmpKey, initVec, tmpKey.Length);
                }

                GetEncryptionData(ref encKey, ref initVec);
                m_dbConnector = new JsonDatabaseConnector(
                    new FileInfo(filePath),
                    encKey, initVec
                );
            } else {
                throw new InvalidDataSourceException();
            }

            m_pluginDelegator.Debug("Subscribing all topics...");
            pluginDelegator.SubscribeTopics(m_subscribeTopics);

            m_pluginDelegator.Information("Email Importer has loaded!");
        }

        public override void OnStart() {
            m_keepThreadAlive = true;
            m_importThread = new Thread(DoWork);
            m_importThread.Start();
        }

        public override void OnStop() {
            m_pluginDelegator?.Information("Stopping worker threads...");
            m_keepThreadAlive = false;
        }
    }
}

