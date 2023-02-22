using System;

namespace Hermod.Core.Accounts {

    using Exceptions;

    /// <summary>
    /// Represents a single domain.
    ///
    /// Each domain can have N <see cref="DomainUser"/> objects associated with it.
    /// This object can be deserialised directly from a JSON file or database entry.
    ///
    /// Each domain object splits the domain into its TLD and the domain name itself.
    ///
    /// This is used both while storing domains and accounts, and while storing and indexing emails.
    /// </summary>
    public sealed class Domain {

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="id">The domain's ID.</param>
        /// <param name="tld">The domain's TLD.</param>
        /// <param name="domainName">The domain name.</param>
        /// <param name="users">An initial list of <see cref="DomainUser"/> accounts.</param>
        /// <exception cref="MalformedDomainException">If a malformed FQDN was encountered.</exception>
        public Domain(int id, string tld, string domainName, params DomainUser[] users) {
            if (string.IsNullOrEmpty(tld) || string.IsNullOrEmpty(domainName)) { throw new MalformedDomainException($"{ domainName }.{ tld }"); }

            Id = id;
            Tld = tld;
            DomainName = domainName;
            DomainUsers = users.ToList();
        }

        /// <summary>
        /// Constructs a new instance of this class.
        /// </summary>
        /// <param name="id">The ID for the domain.</param>
        /// <param name="fqdn">The fully-qualified domain name.</param>
        /// <param name="users">An initial list of <see cref="DomainUser"/> accounts.</param>
        /// <exception cref="MalformedDomainException">If a malformed FQDN was encountered.</exception>
        public Domain(int id, string fqdn, params DomainUser[] users) {
            if (string.IsNullOrEmpty(fqdn) || !fqdn.Contains('.')) { throw new MalformedDomainException(fqdn); }

            Id = id;
            var splits = fqdn.Split('.').Where(x => x != ".").ToArray();
            Tld = splits[0];
            DomainName = string.Join('.', splits[1..]);
            DomainUsers = users.ToList();
        }

        /// <summary>
        /// The domain's ID.
        ///
        /// This is typically a number starting with zero.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The top-level domain.
        /// </summary>
        /// <example >
        /// com
        /// </example>
        public string Tld { get; set; }

        /// <summary>
        /// The name of the domain.
        /// </summary>
        /// <example >
        /// example
        /// </example>
        public string DomainName { get; set; }

        public List<DomainUser> DomainUsers { get; }

    }
}

