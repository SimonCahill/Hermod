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
        public abstract Task<int> PurgeDatabasesAsync();

        /// <summary>
        /// Asynchronously removes a single user from a given domain.
        /// </summary>
        /// <param name="domain">The domain from which to remove the user.</param>
        /// <param name="user">The user account to remove.</param>
        /// <returns>An awaitable <see cref="Task{Boolean}"/> indicating whether or not the operation was successful.</returns>
        public abstract Task<bool> RemoveUserFromDomainAsync(Domain domain, DomainUser user);

        /// <summary>
        /// Asynchronously purges all users from a given domain.
        /// </summary>
        /// <param name="domain">The domain to purge users from.</param>
        /// <returns>An awaitable <see cref="Task{Int32}"/> indicating the amount of users purged.</returns>
        public abstract Task<int> PurgeUsersFromDomainAsync(Domain domain);

        /// <inheritdoc/>
        public abstract void Dispose();

        /// <summary>
        /// Adds a single domain to the database.
        /// </summary>
        /// <param name="domainName">The name of the domain to add; must follow the tld.domain nomenclature!</param>
        /// <returns>An awaitable <see cref="Task{Domain}"/> containing the newly generated domain.</returns>
        public abstract Task<Domain> AddDomainAsync(string domainName);

        /// <summary>
        /// Removes a single domain from the database.
        /// </summary>
        /// <param name="domain">The domain to remove.</param>
        /// <returns><code >true</code> if the domain was removed. <code >false</code> otherwise.</returns>
        /// <exception cref="Core.Exceptions.DomainAlreadyExistsException" >If the domain already exists.</exception>
        public abstract Task<bool> RemoveDomainAsync(Domain domain);

        /// <summary>
        /// Creates a new <see cref="DomainUser"/> instance and adds it to the <paramref name="domain"/>.
        /// </summary>
        /// <param name="domain">The domain to which to add the user.</param>
        /// <param name="user">The user account name.</param>
        /// <param name="password">The password for the user.</param>
        /// <param name="accountType">The account type.</param>
        /// <returns>A reference to the newly created user.</returns>
        public abstract Task<DomainUser> AddUserToDomainAsync(Domain domain, string user, string password, AccountType accountType);
    }
}

