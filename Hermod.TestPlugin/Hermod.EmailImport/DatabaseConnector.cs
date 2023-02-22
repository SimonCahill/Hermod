using System;

namespace Hermod.EmailImport {

    using Core.Delegation;

    using System.Data.Common;
    using System.Data.SqlClient;

    /// <summary>
    /// Handles connecting to databases for storing account information.
    /// </summary>
    public class DatabaseConnector {

        private string  m_dbHost; /// The database host.
        private string  m_dbUser; /// The database user.
        private string  m_dbPass; /// The database password.
        private string  m_dbName; /// The database name.

        private SqlConnection? m_dbConnection;

        private IPluginDelegator m_pluginDelegator;

        /// <summary>
        /// Constructs a new instance of this class
        /// </summary>
        internal DatabaseConnector(string dbHost, string dbUser, string dbPass, string dbName, IPluginDelegator pluginDelegator) {
            m_dbHost = dbHost;
            m_dbUser = dbUser;
            m_dbPass = dbPass;
            m_dbName = dbName;
            m_pluginDelegator = pluginDelegator;
        }

        ~DatabaseConnector() {
            using var _ = m_dbConnection;
        }

        internal void Connect() {
            var connBuilder = new SqlConnectionStringBuilder() {
                DataSource = m_dbHost,
                UserID = m_dbUser,
                Password = m_dbPass,
                InitialCatalog = m_dbName
            };
            m_dbConnection = new SqlConnection(connBuilder.ConnectionString);
            m_dbConnection.Open();
        }

        internal async Task ConnectAsync() {
            var connBuilder = new SqlConnectionStringBuilder() {
                DataSource = m_dbHost,
                UserID = m_dbUser,
                Password = m_dbPass,
                InitialCatalog = m_dbName
            };
            m_dbConnection = new SqlConnection(connBuilder.ConnectionString);
            await m_dbConnection.OpenAsync();
        }


    }
}

