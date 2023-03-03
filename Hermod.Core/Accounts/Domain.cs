using System;

namespace Hermod.Core.Accounts {

    using Exceptions;
    using Newtonsoft.Json;
    using Serilog.Events;
    using System.Net;
    using System.Text.RegularExpressions;

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
    public partial class Domain {

        public const string IanaValidTldListUrl = "https://data.iana.org/TLD/tlds-alpha-by-domain.txt";

        /// <summary>
        /// Domain-validation RegEx inspired by <see href="https://regexr.com/3au3g"/>.
        /// </summary>
        /// <returns>A compiler-generated and optimised <see cref="Regex"/></returns>
        [GeneratedRegex(@"(?:[a-z0-9-](?:[a-z0-9-]{0,61}[a-z0-9-])?\.)+[a-z0-9-][a-z0-9-]{0,61}[a-z0-9-]", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        public static partial Regex DomainRegex();

        public static List<string> ValidTlds { get; } = new List<string>();

        /// <summary>
        /// Added default constructor for JSON.
        /// </summary>
        [JsonConstructor]
        public Domain() {
            Id = -1;
            Tld = string.Empty;
            DomainName = string.Empty;
            DomainUsers = new List<DomainUser>();
        }

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
        /// Copy-constructor.
        /// </summary>
        /// <param name="domain">The original domain to copy from</param>
        /// <param name="includeUsers" >Whether or not to shallow copy the user list</param>
        public Domain(Domain domain, bool includeUsers = false) {
            Id = domain.Id;
            Tld = domain.Tld;
            DomainName = domain.DomainName;

            if (includeUsers) {
                DomainUsers = new List<DomainUser>(domain.DomainUsers.ConvertAll(x => x)); // shallow copy
            } else {
                DomainUsers = new List<DomainUser>();
            }
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

        /// <summary>
        /// A list of users belonging to the domain.
        /// </summary>
        public List<DomainUser> DomainUsers { get; set; }

        /// <summary>
        /// Gets or sets the IMAP/POP3 server address.
        /// </summary>
        public string? ServerAddress { get; set; }

        /// <summary>
        /// Gets or sets the port the IMAP/POP3 server listens to.
        /// </summary>
        public ushort ServerPort { get; set; }

        /// <summary>
        /// Gets a string representation of this domain.
        /// </summary>
        /// <returns>A string representing this domain in the form of TLD.DOMAIN</returns>
        public override string ToString() => $"{ Tld }.{ DomainName }";

        public override bool Equals(object? obj) {
            if (obj is null || obj is not Domain d) { return false; }

            // Thanks Jon Skeet!
            var firstNotSecond = d.DomainUsers.Except(DomainUsers);
            var secondNotFirst = DomainUsers.Except(d.DomainUsers);

            return d.Tld == Tld &&
                   d.DomainName == DomainName &&
                   !firstNotSecond.Any() && !secondNotFirst.Any();
        }

        public static bool operator ==(Domain? a, object? b) => a?.Equals(b) == true;
        public static bool operator !=(Domain? a, object? b) => a?.Equals(b) == false;

        /// <inheritdoc/>
        public override int GetHashCode() {
            return base.GetHashCode();
        }

        /// <summary>
        /// Gets a value indicating whether or not a given domain is valid.
        /// </summary>
        /// <remarks >
        /// Invalid domains which at least match the domain pattern set by ICANN are allowed by Hermod!
        /// This means you can use your internal domain structure as well as your external domains!
        ///
        /// "local.example" is just as valid (in Hermod's eyes!) as "org.example".
        ///
        /// If a truly invalid domain, such as "org-example", "orgexample", "" or "     " is found,
        /// <paramref name="domain"/>, <paramref name="tld"/>, and <paramref name="tld"/> WILL be <code>null</code>!
        /// </remarks>
        /// <param name="fqdn">The FQDN (fully-qualified domain name) to check.</param>
        /// <param name="tldLevels">
        /// The amount of TLDs (and subdomains thereof) found.
        /// E.g. org, com, de, eu all result in 1.
        /// org.uk, com.au, etc. will return 2 or more, depending on how many legal TLDs were found!
        /// </param>
        /// <param name="tld">The actual TLD (with legal subdomains) detected.</param>
        /// <param name="domain">The rest of the domain, separated by dots.</param>
        /// <returns><code >true</code> if a domain is truly valid (in ICANN terms).</returns>
        public static bool IsValidDomain(string fqdn, out int? tldLevels, out string? tld, out string? domain) {
            tldLevels = null;
            tld = null;
            domain = null;

            if (
                string.IsNullOrEmpty(fqdn)          ||
                string.IsNullOrWhiteSpace(fqdn)     ||
                !IsFqdnInBounds(fqdn)               ||
                fqdn.Any(ContainsInvalidChars)      ||
                !DomainRegex().IsMatch(fqdn)
            ) { return false; }

            fqdn = fqdn.ToLowerInvariant();

            var containsValidTld = false;
            var domainLevels = fqdn.Split('.').Where(x => x != ".").ToList();
            var firstNonTopLevel = -1;

            // Determine whether or not the fqdn contains valid TLDs.
            // Non-valid domains will also be accepted by Hermod
            for (int i = 0; i < domainLevels.Count; i++) {
                if (!IsValidTld(domainLevels.ElementAt(i))) {
                    firstNonTopLevel = i;
                    break;
                }

                containsValidTld = true;
            }

            if (firstNonTopLevel == 0) {
                // We've found a domain with invalid TLDs.
                // For Hermod's sake, this is perfectly acceptable!
                // We'll just use the first level as the TLD.
                firstNonTopLevel = 1;
            }

            tldLevels = firstNonTopLevel;
            tld = string.Join('.', domainLevels.GetRange(0, firstNonTopLevel));
            domain = string.Join('.', domainLevels.GetRange(firstNonTopLevel, domainLevels.Count - firstNonTopLevel));

            return containsValidTld;
        }

        private static bool ContainsInvalidChars(char c) => !char.IsDigit(c) && !char.IsLetter(c) && c != '-' && c != '.';

        /// <summary>
        /// Gets a value indicating whether or not an FQDN is within bounds set by the ICANN.
        /// </summary>
        /// <param name="fqdn">The FQDN (Fully-qualified domain name) to check.</param>
        /// <returns><code >true</code> if the FQDN is within bounds.</returns>
        public static bool IsFqdnInBounds(string fqdn) => fqdn.Length is (<= 255) and (> 0);

        /// <summary>
        /// Gets a value indicating whether or not the <paramref name="tld"/> is valid.
        /// </summary>
        /// <param name="tld">The TLD to check for validity.</param>
        /// <returns><code >true</code> if the TLD is valid.</returns>
        public static bool IsValidTld(string tld) {
            if (ValidTlds.Count == 0 && !DownloadCurrentValidTldListFromIana()) { return false; }

            return ValidTlds.Contains(tld);
        }

        /// <summary>
        /// Downloads the latest list of currently valid TLDs from IANA.
        /// </summary>
        /// <returns><code >true</code> if the list was downloaded successfully.</returns>
        public static bool DownloadCurrentValidTldListFromIana() {
            using var httpClient = new HttpClient { };
            using var httpResponse = httpClient.GetAsync(IanaValidTldListUrl).GetAwaiter().GetResult();

            if (httpResponse is null) { return ValidTlds.Count > 0; }

            using (var sReader = new StreamReader(httpResponse.Content.ReadAsStream())) {
                ValidTlds.Clear();
                while (sReader.Peek() != -1) {
                    var line = sReader.ReadLine();
                    if (line is null) { break; }
                    if (line.StartsWith("#") || line.Length == 0) { continue; }

                    ValidTlds.Add(line.ToLowerInvariant());
                }
            }

            return ValidTlds.Count > 0;
        }

        /// <summary>
        /// Asynchronously downloads the latest list of TLDs from the IANA.
        /// </summary>
        /// <returns>An awaitable <see cref="Task{Boolean}"/> equating <code >true</code> if the list was downloaded successfully.</returns>
        public static async Task<bool> DownloadCurrentValidTldListFromIanaAsync() {
            using var httpClient = new HttpClient { };
            using var httpResponse = await httpClient.GetAsync(IanaValidTldListUrl);

            if (httpResponse is null) { return ValidTlds.Count > 0; }

            using (var sReader = new StreamReader(await httpResponse.Content.ReadAsStreamAsync())) {
                ValidTlds.Clear();
                while (sReader.Peek() != -1) {
                    var line = await sReader.ReadLineAsync();
                    if (line is null) { break; }
                    if (line.StartsWith("#") || line.Length == 0) { continue; }

                    ValidTlds.Add(line.ToLowerInvariant());
                }
            }

            return ValidTlds.Count > 0;
        }

    }
}

