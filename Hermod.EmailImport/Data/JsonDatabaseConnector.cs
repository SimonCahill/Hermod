﻿using System;

namespace Hermod.EmailImport.Data {

    using Core.Accounts;

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
            } if (key is null || key.Length == 0) {
                throw new ArgumentNullException(nameof(key));
            } if (initVector is null || initVector.Length == 0) {
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
            using var fStream = JsonFile.Create();
            using var sWriter = new StreamWriter(fStream);
            sWriter.Write(EncryptString(JsonConvert.SerializeObject(m_jsonObj, Formatting.Indented)));
        }

        internal async Task DumpJsonAsync() {
            using var fStream = JsonFile.Create();
            using var sWriter = new StreamWriter(fStream);
            sWriter.Write(await EncryptStringAsync(JsonConvert.SerializeObject(m_jsonObj, Formatting.Indented)));
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
        public override async Task<int> PurgeDatabases() {
            var domainCount = m_jsonObj.DomainList.Count;
            m_jsonObj = new DomainContainer();
            await DumpJsonAsync();

            return domainCount;
        }

        /// <inheritdoc/>
        /// <remarks >
        /// This will dump the newest version to disk.
        /// </remarks>
        public override async Task<int> PurgeUsersFromDomain(Domain domain) {
            var domainToPurge = m_jsonObj.DomainList.First(d => d.Tld == domain.Tld && d.DomainName == domain.DomainName);
            var domainUsers = domainToPurge.DomainUsers.Count;
            domainToPurge.DomainUsers = new List<DomainUser>();
            await DumpJsonAsync();

            return domainUsers;
        }

        /// <inheritdoc/>
        public override async Task<bool> RemoveUserFromDomain(Domain domain, DomainUser user) {
            var domainToEdit = m_jsonObj.DomainList.First(d => d.Tld == domain.Tld && d.DomainName == domain.DomainName);
            domainToEdit.DomainUsers.Remove(user);
            await DumpJsonAsync();

            return true;
        }
    }
}

