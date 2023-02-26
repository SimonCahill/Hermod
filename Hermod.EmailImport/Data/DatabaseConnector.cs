using System;

namespace Hermod.EmailImport.Data {

    using Core.Accounts;

    using System.Threading.Tasks;

    /// <summary>
    /// Defines a simple contract between the EmailImport plugin and any database connectors.
    /// </summary>
    /// <remarks >
    /// When I say database, I use it interchangeably with "table".
    /// </remarks>
    public abstract class DatabaseConnector: IDisposable {

        protected object m_lock;

        protected DatabaseConnector() {
            m_lock = new object();
        }

        /// <summary>
        /// Connect to the database.
        /// </summary>
        public abstract void Connect();

        /// <summary>
        /// Asynchronously connect to the database.
        /// </summary>
        /// <returns>An awaitable <see cref="Task"/></returns>
        public abstract Task ConnectAsync();

        /// <summary>
        /// Asynchronously gets a value indicating whether or not the database is initialised.
        /// </summary>
        /// <returns>An awaitable <see cref="Task{Boolean}"/> indicating whether or not the database was initialised.</returns>
        public abstract Task<bool> IsInitialisedAsync();

        /// <summary>
        /// Asynchronously initialises the database.
        /// </summary>
        /// <returns>An awaitable <see cref="Task"/></returns>
        public abstract Task InitialiseDatabaseAsync();

        /// <summary>
        /// Asynchronously gets a list of all known domains.
        /// </summary>
        /// <param name="includeUsers">Indicates whether or not to include user data for each domain.</param>
        /// <param name="tlds">A list of TLDs the domains must contain. E.g. "com", "eu", "us", "de", "co.uk"</param>
        /// <returns>An awaitable <see cref="Task{List{Domain}}"/> containing the search results.</returns>
        public abstract Task<List<Domain>> GetDomainsAsync(bool includeUsers = true, params string[] tlds);

        /// <summary>
        /// Asynchronously gets all the users for a given domain.
        /// </summary>
        /// <param name="domain">The domain object in which to push the users.</param>
        /// <returns>An awaitable <see cref="Task"/></returns>
        public abstract Task GetUsersForDomainAsync(Domain domain);

        /// <summary>
        /// Asynchronously purges all databases.
        /// </summary>
        /// <returns>An awaitable <see cref="Task{Int32}"/> indicating the amount of domains purged.</returns>
        public abstract Task<int> PurgeDatabases();

        /// <summary>
        /// Asynchronously removes a single user from a given domain.
        /// </summary>
        /// <param name="domain">The domain from which to remove the user.</param>
        /// <param name="user">The user account to remove.</param>
        /// <returns>An awaitable <see cref="Task{Boolean}"/> indicating whether or not the operation was successful.</returns>
        public abstract Task<bool> RemoveUserFromDomain(Domain domain, DomainUser user);

        /// <summary>
        /// Asynchronously purges all users from a given domain.
        /// </summary>
        /// <param name="domain">The domain to purge users from.</param>
        /// <returns>An awaitable <see cref="Task{Int32}"/> indicating the amount of users purged.</returns>
        public abstract Task<int> PurgeUsersFromDomain(Domain domain);

        /// <inheritdoc/>
        public abstract void Dispose();
    }
}

