using System;

namespace Hermod.EmailImport.Data {

    using Core.Accounts;
    using Core.Exceptions;

    using Newtonsoft.Json;

    using System.IO;
    using System.Security.Cryptography;

    /// <summary>
    /// A <see cref="DatabaseConnector"/> which "connects" to an encrypted JSON file containing all domain and user information.
    /// </summary>
    public partial class JsonDatabaseConnector: DatabaseConnector {

        public class DomainContainer {
            public List<Domain> DomainList { get; set; } = new List<Domain>();
        }

        internal FileInfo JsonFile { get; set; }

        private DomainContainer m_jsonObj = new DomainContainer { };

        public JsonDatabaseConnector(FileInfo jsonFile, byte[] key, byte[] initVector) {
            if (jsonFile is null) {
                throw new ArgumentNullException(nameof(jsonFile));
            }
            if (key is null || key.Length == 0) {
                throw new ArgumentNullException(nameof(key));
            }
            if (initVector is null || initVector.Length == 0) {
                throw new ArgumentNullException(nameof(initVector));
            }

            JsonFile = jsonFile;
            m_encKey = key;
            m_initVector = initVector;
        }

        ~JsonDatabaseConnector() {
            DumpJson();
        }

        /// <inheritdoc/>
        public override void Connect() {
            if (!JsonFile.Exists) {
                DumpJson();
                return;
            }
            ReadFile();
        }

        /// <inheritdoc/>
        public override async Task ConnectAsync() {
            if (!JsonFile.Exists) {
                await DumpJsonAsync();
                return;
            }
            await ReadFileAsync();
        }

        internal void ReadFile() {
            var fContents = File.ReadAllBytes(JsonFile.FullName);
            if (fContents is null) {
                DumpJson();
                return;
            }

            string decryptedString = DecryptString(fContents);
            if (decryptedString is null) {
                DumpJson();
                return;
            }

            m_jsonObj = JsonConvert.DeserializeObject<DomainContainer>(decryptedString);
        }

        internal async Task ReadFileAsync() {
            var fContents = await File.ReadAllBytesAsync(JsonFile.FullName);
            if (fContents is null) {
                await DumpJsonAsync();
                return;
            }

            string decryptedString = await DecryptStringAsync(fContents);
            if (decryptedString is null) {
                await DumpJsonAsync();
                return;
            }

            m_jsonObj = JsonConvert.DeserializeObject<DomainContainer>(decryptedString);
        }

        internal void DumpJson() {
            if (!JsonFile.Exists) {
                JsonFile.Directory?.Create();
                JsonFile.Create().Close();
            }

            using var fStream = JsonFile.Open(FileMode.Truncate);
            fStream.Write(EncryptString(JsonConvert.SerializeObject(m_jsonObj, Formatting.Indented)));
        }

        internal async Task DumpJsonAsync() {
            if (!JsonFile.Exists) {
                JsonFile.Directory?.Create();
                JsonFile.Create().Close();
            }

            using var fStream = JsonFile.Open(FileMode.Truncate);
            var encryptedData = await EncryptStringAsync(JsonConvert.SerializeObject(m_jsonObj, Formatting.Indented));
            await fStream.WriteAsync(encryptedData);
        }

        /// <inheritdoc/>
        public override Task<List<Domain>> GetDomainsAsync(bool includeUsers = true, params string[] tlds) {
            if (tlds.Length == 0) {
                return Task.FromResult(
                    includeUsers ?
                        m_jsonObj.DomainList :
                        new List<Domain>(m_jsonObj.DomainList.Select(x => new Domain(x)))
                );
            }

            var filteredDomains = m_jsonObj.DomainList.Where(d => tlds.Contains(d.Tld)).ToList();

            return Task.FromResult(
                includeUsers ?
                    filteredDomains :
                    new List<Domain>(filteredDomains.Select(x => new Domain(x)))
            );
        }

        /// <inheritdoc/>
        public override Task GetUsersForDomainAsync(Domain domain) {
            domain.DomainUsers = m_jsonObj.DomainList.First(x => x.DomainName == domain.DomainName && domain.Tld == x.Tld).DomainUsers.ToList();

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override async Task InitialiseDatabaseAsync() {
            await DumpJsonAsync();
        }

        /// <inheritdoc/>
        public override Task<bool> IsInitialisedAsync() => Task.FromResult(true); // this is always true here

        /// <inheritdoc/>
        /// <remarks >
        /// This will dump the newest version to disk.
        /// </remarks>
        public override async Task<int> PurgeDatabasesAsync() {
            var domainCount = m_jsonObj.DomainList.Count;
            m_jsonObj = new DomainContainer();
            await DumpJsonAsync();

            return domainCount;
        }

        /// <inheritdoc/>
        /// <remarks >
        /// This will dump the newest version to disk.
        /// </remarks>
        public override async Task<int> PurgeUsersFromDomainAsync(Domain domain) {
            var domainToPurge = m_jsonObj.DomainList.First(d => d.Tld == domain.Tld && d.DomainName == domain.DomainName);
            var domainUsers = domainToPurge.DomainUsers.Count;
            domainToPurge.DomainUsers = new List<DomainUser>();
            await DumpJsonAsync();

            return domainUsers;
        }

        /// <inheritdoc/>
        public override async Task<bool> RemoveUserFromDomainAsync(Domain domain, DomainUser user) {
            var domainToEdit = m_jsonObj.DomainList.First(d => d.Tld == domain.Tld && d.DomainName == domain.DomainName);
            domainToEdit.DomainUsers.Remove(user);
            await DumpJsonAsync();

            return true;
        }

        /// <inheritdoc/>
        public override async Task<Domain> AddDomainAsync(string domainName) {
            string? tld;
            string? domain;

            if (!Domain.IsValidDomain(domainName, out _, out tld, out domain)) {
                if (tld is null || domain is null) {
                    throw new InvalidDomainNameException(domainName, "Encountered malformed domain name!");
                }
            } else if (tld is null || domain is null) {
                throw new Exception("An unknown exception has occurred!"); // this should never happen
            }

            if (m_jsonObj.DomainList.Any(d => d.DomainName == domain || d.Tld == tld)) {
                throw new DomainAlreadyExistsException($"The domain { domainName } already exists in the database!");
            }

            var newDomain = new Domain(m_jsonObj.DomainList.Count + 1, tld, domainName);
            m_jsonObj.DomainList.Add(newDomain);
            await DumpJsonAsync();

            return newDomain;
        }

        /// <inheritdoc/>
        public override async Task<bool> RemoveDomainAsync(Domain domain) {
            var removed = m_jsonObj.DomainList.Remove(domain);
            await DumpJsonAsync();

            return removed;
        }
    }
}

