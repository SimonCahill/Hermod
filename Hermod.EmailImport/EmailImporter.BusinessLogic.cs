using System;

namespace Hermod.EmailImport {

    using Core;
    using Core.Accounts;

    using MailKit;
    using MailKit.Net.Imap;
    using MailKit.Net.Pop3;

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
                    LogInfo("Attempting to import emails from {domain}", domain.ToString());
                    try {
                        var importedMessages = await ImportEmailsFromDomain(importDir.CreateSubdirectory(domain.ToString()), domain);
                        LogInfo("Successfully imported {total} emails for {domain}!", importedMessages, domain.ToString());
                    } catch (Exception ex) {
                        LogError("Failed to import messages for {domain}! Error: {ex}", domain.ToString(), ex);
                    }
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

        /// <summary>
        /// Imports all emails from all users of a given domain via IMAP or POP3, depending on account configuration.
        /// </summary>
        /// <param name="importDir">The domain's import dir root.</param>
        /// <param name="domain">The domain for whose users to import emails.</param>
        /// <returns>The total amount of emails imported.</returns>
        private async Task<int> ImportEmailsFromDomain(DirectoryInfo importDir, Domain domain) {
            LogInfo("Importing emails from {domain}", domain.DomainName);
            var totalImported = 0;

            totalImported += await ImportImapEmailsFromUsers(importDir, domain, domain.DomainUsers.Where(u => u.AccountType == AccountType.Imap).ToArray());
            totalImported += await ImportPop3EmailsFromUsers(importDir, domain, domain.DomainUsers.Where(u => u.AccountType == AccountType.Pop3).ToArray());

            return totalImported;
        }

        /// <summary>
        /// Attempts to import all emails from all folders for all known domain users.
        /// </summary>
        /// <param name="importDir">The root import directory for each domain.</param>
        /// <param name="domain">The domain to import the emails for.</param>
        /// <param name="users">The users for which to retrieve emails via IMAP.</param>
        /// <returns>The total amount of emails imported.</returns>
        /// <exception cref="NullReferenceException">If <see cref="m_dbConnection"/> is null.</exception>
        private async Task<int> ImportImapEmailsFromUsers(DirectoryInfo importDir, Domain domain, params DomainUser[] users) {
            using var imapClient = new ImapClient { };

            if (m_dbConnector is null) {
                throw new NullReferenceException("Database connector not initialised!");
            }

            int totalImportedEmails = 0;

            foreach (var user in users) {
                if (!imapClient.IsConnected) {
                    try {
                        await imapClient.ConnectAsync(domain.ServerAddress, domain.ServerPort ?? 0);
                    } catch (Exception ex) {
                        LogError("Failed to connect to server {exception}", ex);
                        break;
                    }
                }

                try {
                    totalImportedEmails = await ImportMessagesFromImapUser(importDir, imapClient, user);
                } catch (Exception ex) {
                    LogError("Failed to import emails for {user}! Reason: {ex}", user.AccountName, ex);
                    continue;
                } finally {
                    if (imapClient.IsAuthenticated) { await imapClient.DisconnectAsync(true); }
                }
            }

            return totalImportedEmails;
        }

        /// <summary>
        /// Imports messages from a single <see cref="IMailFolder"/> to the local disk.
        /// </summary>
        /// <param name="imailFolder">The folder from which to import the emails.</param>
        /// <param name="userDir">The local directory in which to store the imported emails.</param>
        /// <param name="user">The user for whom the emails are being imported.</param>
        /// <returns>The total amount of files downloaded.</returns>
        private async Task<int> ImportMessagesFromFolder(IMailFolder imailFolder, DirectoryInfo userDir, DomainUser user) {
            int totalImported = 0;

            var messages = imailFolder.Where(e => e.Date >= user.LastEmailRetrieval);

            foreach (var msg in messages) {
                try {
                    using (msg)
                    using (var fStream = userDir.GetSubFile($"{msg.MessageId}{AppInfo.ImportedEmailExtension}").Open(FileMode.CreateNew)) {
                        await msg.WriteToAsync(fStream);
                        totalImported++;
                    }
                } catch (Exception ex) {
                    LogError("Failed to download message from account {user}! Error: {ex}", user.AccountName, ex);
                    continue;
                }
            }

            return totalImported;
        }

        /// <summary>
        /// Imports all the messages from a single IMAP user.
        /// </summary>
        /// <param name="importDir">The root directory for the domain in which the user resides.</param>
        /// <param name="imapClient">The IMAP client currently being used.</param>
        /// <param name="user">The user for whom to import the emails.</param>
        /// <returns>The total amount of emails imported.</returns>
        private async Task<int> ImportMessagesFromImapUser(DirectoryInfo importDir, ImapClient imapClient, DomainUser user) {
            int totalImported = 0;

            var passwd = await m_dbConnector.DecryptUserPassword(user);
            await imapClient.AuthenticateAsync(user.AccountName, passwd);

            if (!imapClient.IsAuthenticated) {
                LogError("Failed to authenticate {user}! Skipping...", user.AccountName);
                return totalImported;
            }

            var userDir = importDir.CreateSubdirectory(user.AccountName);

            var namespaces = imapClient.PersonalNamespaces;

            foreach (var @namespace in namespaces) {
                if (@namespace is null) { continue; }

                foreach (var imapFolder in await imapClient.GetFoldersAsync(@namespace)) {
                    if (!imapFolder.IsOpen) {
                        await imapFolder.OpenAsync(FolderAccess.ReadOnly);
                    }

                    totalImported += await ImportMessagesFromFolder(imapFolder, userDir, user);
                }
            }

            await imapClient.DisconnectAsync(true);

            return totalImported;
        }

        /// <summary>
        /// Imports all emails from a POP3 account.
        /// </summary>
        /// <param name="importDir">The root import directory for the domain.</param>
        /// <param name="domain">The domain in which the user resides.</param>
        /// <param name="users">A list of users to import from via POP3.</param>
        /// <returns>The total amount of imported emails.</returns>
        /// <exception cref="NullReferenceException">If <see cref="m_dbConnector"/> was null.</exception>
        private async Task<int> ImportPop3EmailsFromUsers(DirectoryInfo importDir, Domain domain, params DomainUser[] users) {
            using var pop3Client = new Pop3Client { };

            if (m_dbConnector is null) {
                throw new NullReferenceException("Database connector not initialised!");
            }

            int totalImportedEmails = 0;

            foreach (var user in users) {
                if (!pop3Client.IsConnected) {
                    try {
                        await pop3Client.ConnectAsync(domain.ServerAddress, domain.ServerPort ?? 0);
                    } catch (Exception ex) {
                        LogError("Failed to connect to server {exception}", ex);
                        break;
                    }
                }

                var userDir = importDir.CreateSubdirectory(user.AccountName);

                try {
                    var expectedMessages = await pop3Client.GetMessageCountAsync();

                    for (int i = 0; i < expectedMessages; i++) {
                        using var msg = await pop3Client.GetMessageAsync(i);
                        using (var fStream = userDir.GetSubFile($"{ msg.MessageId }{ AppInfo.ImportedEmailExtension }").Open(FileMode.CreateNew)) {
                            await msg.WriteToAsync(fStream);
                            totalImportedEmails++;
                        }
                    }
                } catch (Exception ex) {
                    LogError("Failed to import emails for {user}! Reason: {ex}", user.AccountName, ex);
                    continue;
                } finally {
                    if (pop3Client.IsAuthenticated) { await pop3Client.DisconnectAsync(true); }
                    await m_dbConnector.SetLastEmailRetrievalAsync(domain, user);
                }
            }

            return totalImportedEmails;
        }

    }
}

