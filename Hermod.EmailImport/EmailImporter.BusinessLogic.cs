using System;

namespace Hermod.EmailImport {

    using Core;
    using Core.Accounts;

    using MailKit;

    partial class EmailImporter {

        const string DefaultEmailImportDirName = ".imports";
        const int FallbackSleepTimeMs = 120 * 60 * 1000;

        private async void DoWork() {

            if (m_dbConnector is null) {
                throw new NullReferenceException("No connection to account data source!");
            }

            while (m_keepThreadAlive) {
                var importDir = await GetImportDirectoryAsync();

                var domains = await m_dbConnector.GetDomainsAsync();

                if (domains is null) { goto SleepUntilNextInterval; }

                foreach (var domain in domains) {

                }

                SleepUntilNextInterval:
                Thread.Sleep(await GetSleepTimeAsync());
            }
        }

        private Task<DirectoryInfo> GetImportDirectoryAsync() {
            string? importPath = PluginDelegator?.GetApplicationConfig<string?>("EmailImporting.ImportLocation");
            DirectoryInfo importDirectory;

            if (importPath is null) {
                importDirectory = AppInfo.GetLocalHermodDirectory().CreateSubdirectory(DefaultEmailImportDirName);
                PluginDelegator?.SetApplicationConfig<string>("EmailImporting.ImportLocation", importDirectory.FullName);
            } else {
                importDirectory = new DirectoryInfo(importPath);
            }

            return Task.FromResult(importDirectory);
        }

        private Task<int> GetSleepTimeAsync() {
            return Task.FromResult(
                PluginDelegator?.GetApplicationConfig<int>("EmailImporting.ImportLocation") * 60 * 1000 ??
                FallbackSleepTimeMs
            );
        }

        /// <summary>
        /// Creates a new <see cref="DirectoryInfo"/> object for each domain passed.
        /// </summary>
        /// <param name="importDir">The root import directory.</param>
        /// <param name="domain">The domain currently being processed.</param>
        /// <returns>The newly created <see cref="DirectoryInfo"/> pointing to the directory for <paramref name="domain"/></returns>
        private Task<DirectoryInfo> CreateDirectoryForDomain(DirectoryInfo importDir, Domain domain) {
            return Task.FromResult(
                importDir.CreateSubdirectory(domain.Tld)
                         .CreateSubdirectory(domain.DomainName)
            );
        }

        private Task ImportEmailsFromDomain(Domain domain) {

            

            throw new NotImplementedException();
        }

    }
}

