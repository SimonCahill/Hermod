#if false

using System;

namespace Hermod.EmailImport {

    using Core.Accounts;
    using Core.Delegation;
    using Hermod.EmailImport.Data;
    using System.Data;
    using System.Data.Common;
    using System.Data.SqlClient;
    using System.Text;

    /// <summary>
    /// Handles connecting to MySQL and MariaDB databases for storing account information.
    ///
    /// This class is (very likely) incompatible with other databases!
    /// </summary>
    public class MySqlDatabaseConnector: DatabaseConnector {

        const string DomainTableName = "Hermod_Domains"; /// The name of the Domain table
        const string DomainUsersTableName = "Hermod_DomainUsers"; /// The name of the Domain User table
        const string TldTableName = "Hermod_Tld"; /// The name of the TLD table

        const string TldDb_IdColumnName = "ID"; /// The name of the ID column for the TLD table
        const string TldDb_TldColumnName = "TLD"; /// The name of the TLD column for the TLD table

        const string DomainDb_IdColumnName = "ID"; /// The name of the ID column for the Domain table
        const string DomainDb_TldColumnName = "TLD"; /// The name of the TLD ID column for the Domain table
        const string DomainDb_DomainColumnName = "DomainName"; /// The name of the Domain Name column for the Domain table

        const string DomainUserDb_IdColumnName = "ID";
        const string DomainUserDb_DomainColumnName = "Domain";
        const string DomainUserDb_AccountColumnName = "AccountName";
        const string DomainUserDb_AccountPasswdColumnName = "AccountPassword";
        const string DomainUserDb_PasswdSaltColumnName = "PasswordSalt";
        const string DomainUserDb_AccountType = "AccountType";

        private bool m_domainDbFound = false; /// indicates whether or not the domain table was found
        private bool m_domainUserDbFound = false; /// indicates whether or not the domain user table was found
        private bool m_tldDbFound = false; /// indicates whether or not the TLD table was found

        private string m_dbHost; /// The database host.
        private string m_dbUser; /// The database user.
        private string m_dbPass; /// The database password.
        private string m_dbName; /// The database name.

        private SqlConnection? m_dbConnection;

        private IPluginDelegator m_pluginDelegator;

        /// <summary>
        /// Constructs a new instance of this class
        /// </summary>
        internal MySqlDatabaseConnector(string dbHost, string dbUser, string dbPass, string dbName, IPluginDelegator pluginDelegator) {
            m_dbHost = dbHost;
            m_dbUser = dbUser;
            m_dbPass = dbPass;
            m_dbName = dbName;
            m_pluginDelegator = pluginDelegator;
        }

        public override void Dispose() {
            m_dbConnection?.Dispose();
        }

        private void M_dbConnection_StateChange(object sender, System.Data.StateChangeEventArgs e) {
            if (e.CurrentState == ConnectionState.Closed || e.CurrentState == ConnectionState.Broken) {
                m_pluginDelegator.Error($"Lost connection to database! Will reconnect!");
                Connect();
            }
        }

        /// <summary>
        /// Synchronously connects to the database.
        /// </summary>
        public override void Connect() {
            if (
                m_dbConnection is not null &&
                (
                    m_dbConnection?.State != ConnectionState.Broken ||
                    m_dbConnection?.State != ConnectionState.Closed
                )
            ) {
                return;
            } else if (m_dbConnection is null) {
                var connBuilder = new SqlConnectionStringBuilder() {
                    DataSource = m_dbHost,
                    UserID = m_dbUser,
                    Password = m_dbPass,
                    InitialCatalog = m_dbName
                };
                m_dbConnection = new SqlConnection(connBuilder.ConnectionString);
            }

            m_dbConnection.StateChange += M_dbConnection_StateChange;
            m_dbConnection.Open();
        }

        /// <summary>
        /// Asynchronously connects to the database.
        /// </summary>
        /// <returns></returns>
        public override async Task ConnectAsync() {
            if (
                m_dbConnection is not null &&
                (
                    m_dbConnection?.State != ConnectionState.Broken ||
                    m_dbConnection?.State != ConnectionState.Closed
                )
            ) {
                return;
            } else if (m_dbConnection is null) {
                var connBuilder = new SqlConnectionStringBuilder() {
                    DataSource = m_dbHost,
                    UserID = m_dbUser,
                    Password = m_dbPass,
                    InitialCatalog = m_dbName
                };
                m_dbConnection = new SqlConnection(connBuilder.ConnectionString);
            }

            m_dbConnection.StateChange += M_dbConnection_StateChange;
            await m_dbConnection.OpenAsync();
        }

        /// <summary>
        /// Gets a value indicating whether or not the database and its tables have been initialised.
        /// </summary>
        /// <returns>An awaitable <see cref="Task"/> with the return value; <code >true</code> if everything is initialised. <code >false</code> otherwise.</returns>
        /// <exception cref="Exception">If an error occurs.</exception>
        public override async Task<bool> IsInitialisedAsync() {
            if (m_dbConnection is null) {
                throw new Exception("Database not initialised!");
            }

            return await HasDatabases(); // for now this is the only check we need
        }

        /// <summary>
        /// Gets a value indicating whether or not all required databases exist or not.
        /// </summary>
        /// <returns><code >true</code> if all required databases exist. <code >false</code> otherwise.</returns>
        /// <exception cref="Exception">If an error occurs.</exception>
        protected async Task<bool> HasDatabases() {
            using var sqlCommand = m_dbConnection?.CreateCommand();

            if (sqlCommand is null) { throw new Exception("Connection not initialised!"); }

            sqlCommand.CommandText = "show tables;";

            using var sqlReader = await sqlCommand.ExecuteReaderAsync();

            while (await sqlReader.ReadAsync()) {
                switch (sqlReader.GetString(0)) {
                    case DomainTableName:
                        m_domainDbFound = true;
                        break;
                    case DomainUsersTableName:
                        m_domainUserDbFound = true;
                        break;
                    case TldTableName:
                        m_tldDbFound = true;
                        break;
                }
            }

            return m_domainDbFound && m_domainUserDbFound && m_tldDbFound;
        }

        /// <summary>
        /// Asynchronously initialises the database and any required tables.
        /// </summary>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        public override async Task InitialiseDatabaseAsync() => await CreateTablesAsync();

        /// <summary>
        /// Asynchronously creates all required tables.
        /// </summary>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        /// <exception cref="Exception">If an error occurs.</exception>
        protected async Task CreateTablesAsync() {
            var sqlCommand = m_dbConnection?.CreateCommand();

            if (sqlCommand is null) { throw new Exception("Database not initialised!"); }

            var sBuilder = new StringBuilder();

            if (!m_tldDbFound) {
                sBuilder.AppendLine(
                    $"""
                    CREATE TABLE {TldTableName} (
                        {TldDb_IdColumnName} int not null primary key auto_increment,
                        {TldDb_TldColumnName} varchar(30) not null
                    );
                    """
                );
            }

            if (!m_domainDbFound) {
                sBuilder.AppendLine(
                    $"""
                    CREATE TABLE {DomainTableName} (
                        {DomainDb_IdColumnName} int not null primary key auto_increment,
                        {DomainDb_TldColumnName} int not null,
                        {DomainDb_DomainColumnName} varchar(255) not null,
                        foreign key ({DomainDb_TldColumnName}) references {TldTableName}({TldDb_IdColumnName})
                    );
                    """
                );
            }

            if (!m_domainUserDbFound) {
                sBuilder.AppendLine(
                    $"""
                    CREATE TABLE {DomainUsersTableName} (
                        {DomainUserDb_IdColumnName} int not null primary key auto_increment,
                        {DomainUserDb_DomainColumnName} int not null,
                        {DomainUserDb_AccountColumnName} varchar(255) not null,
                        {DomainUserDb_AccountPasswdColumnName} varchar(2048) not null,
                        {DomainUserDb_PasswdSaltColumnName} varchar(4096) not null,
                        {DomainUserDb_AccountType} varchar(50) not null,
                        foreign key ({DomainUserDb_DomainColumnName}) references {DomainTableName}({DomainDb_IdColumnName})
                    );
                    """
                );
            }

            if (sBuilder.Length == 0) {
                return;
            }

            sqlCommand.CommandText = sBuilder.ToString();
            await sqlCommand.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Retrieves a (optionally filtered) list of known domains.
        /// </summary>
        /// <param name="includeUsers">Set to <code >true</code> to include all users for each domain.</param>
        /// <param name="tlds">An optional list of TLDs; if set, only domains within these TLDs will be returned.</param>
        /// <returns>A list of known domains.</returns>
        /// <exception cref="Exception">If an error occurs.</exception>
        public override async Task<List<Domain>> GetDomainsAsync(bool includeUsers = true, params string[] tlds) {
            if (m_dbConnection is null || m_dbConnection?.State == ConnectionState.Broken || m_dbConnection?.State == ConnectionState.Closed) {
                await ConnectAsync();
            }

            var domains = new List<Domain>();

            var sqlCommand = m_dbConnection?.CreateCommand();
            if (sqlCommand is null) { throw new Exception("Failed to create SqlCommand!"); }

            const string tldName = "TLDName";

            sqlCommand.CommandText =
            $"""
            SELECT *,
            	(
                    SELECT {TldTableName}.{TldDb_TldColumnName} FROM {TldTableName}
                    WHERE {TldTableName}.{TldDb_IdColumnName} = {DomainTableName}.{DomainDb_TldColumnName}
                ) As {tldName}
            FROM {DomainTableName};
            """;

            using var result = await sqlCommand.ExecuteReaderAsync();

            while (await result.ReadAsync()) {
                var domain = new Domain(
                    result.GetInt32(DomainDb_IdColumnName), // ID
                    result.GetString(tldName), // TLD ID
                    result.GetString(DomainDb_DomainColumnName) // domain name
                );

                if (includeUsers) {
                    await GetUsersForDomainAsync(domain);
                }
            }

            if (tlds.Length == 0) { return domains; }

            // only filter out domains when necessary.
            // this can and should be optimised in future so the SqlCommand does the filtering for us.
            // but I'm not good at SQL :)
            // - Simon
            return domains.Where(x => tlds.Contains($"{x.Tld}")).ToList();
        }

        /// <summary>
        /// Gets all users for a given <see cref="Domain"/>.
        /// </summary>
        /// <param name="domain">The <see cref="Domain"/> for which to retrieve all users.</param>
        /// <returns>An awaitable task.</returns>
        /// <exception cref="Exception">If an error occurs.</exception>
        public async Task GetUsersForDomainAsync(Domain domain, bool clearExisting = true) {
            if (m_dbConnection is null || m_dbConnection?.State == ConnectionState.Broken || m_dbConnection?.State == ConnectionState.Closed) {
                await ConnectAsync();
            }

            if (clearExisting) { domain.DomainUsers.Clear(); }

            var sqlCommand = m_dbConnection?.CreateCommand();
            if (sqlCommand is null) { throw new Exception("Failed to create SqlCommand!"); }

            sqlCommand.CommandText =
            $"""
            SELECT *
            FROM {DomainUsersTableName}
            WHERE Domain = {domain.Id}
            """;

            using var result = await sqlCommand.ExecuteReaderAsync();

            while (await result.ReadAsync()) {
                var user = new DomainUser(
                    result.GetInt32(DomainUserDb_IdColumnName), // ID
                    result.GetString(DomainUserDb_AccountColumnName), // AccountName
                    result.GetString(DomainUserDb_AccountPasswdColumnName), // AccountPassword
                    result.GetString(DomainUserDb_PasswdSaltColumnName),
                    Enum.Parse<AccountType>(result.GetString(DomainUserDb_AccountType))
                );

                domain.DomainUsers.Add(user);
            }
        }

        public override Task GetUsersForDomainAsync(Domain domain) => GetUsersForDomainAsync(domain);

        public override Task<int> PurgeDatabases() {
            throw new NotImplementedException();
        }

        public override Task<bool> RemoveUserFromDomain(Domain domain, DomainUser user) {
            throw new NotImplementedException();
        }

        public override Task<int> PurgeUsersFromDomain(Domain domain) {
            throw new NotImplementedException();
        }
    }
}

#endif // false