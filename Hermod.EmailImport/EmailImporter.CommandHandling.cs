using System;

namespace Hermod.EmailImport {
    using System.Text;
    using Core.Commands.Results;
    using Hermod.Core.Accounts;

    partial class EmailImporter {

        private ICommandResult Handle_GetDomains(params string[] args) {
            var domains = m_dbConnector?.GetDomainsAsync(true, args).GetAwaiter().GetResult();

            if (domains is null || !domains.Any()) { return new CommandErrorResult("No domains found matching criteria!"); }

            var sBuilder = new StringBuilder().AppendLine("Got domain(s):");

            foreach (var domain in domains) {
                sBuilder.Append($"\t{domain.ToString()}");

                if (domain.DomainUsers.Any()) {
                    sBuilder.AppendLine(":");

                    foreach (var user in domain.DomainUsers) {
                        sBuilder.AppendLine($"\t\t{user.AccountName} [{user.AccountType.ToString()}]");
                    }
                    sBuilder.AppendLine();
                }
            }

            return new CommandResult(sBuilder.ToString(), domains);
        }

        private ICommandResult Handle_GetSingleDomain(params string[] args) {
            if (args is null || args.Length == 0) {
                return new CommandErrorResult("At least one domain must be supplied!");
            }

            var domains = m_dbConnector?.GetDomainsAsync(true).GetAwaiter().GetResult().Where(d => args.Contains(d.ToString()));
            if (domains is null || domains.Any()) {
                return new CommandErrorResult("No domains found matching any of the inputs!");
            }

            var sBuilder = new StringBuilder().AppendLine("Got domain(s):");

            foreach (var domain in domains) {
                sBuilder.Append($"\t{ domain.ToString() }");

                if (domain.DomainUsers.Any()) {
                    sBuilder.AppendLine(":");

                    foreach (var user in domain.DomainUsers) {
                        sBuilder.AppendLine($"\t\t{ user.AccountName } [{ user.AccountType.ToString() }]");
                    }
                    sBuilder.AppendLine();
                }
            }

            return new CommandResult(sBuilder.ToString(), domains);
        }

        private ICommandResult Handle_AddDomain(params string[] args) {
            if (args.Length == 0) {
                return new CommandErrorResult("Missing input parameters!");
            }

            Dictionary<string, string> failedDomains = new Dictionary<string, string>();
            List<Domain> addedDomains = new List<Domain>();

            foreach (var domain in args) {
                try {
                    var newDomain = m_dbConnector?.AddDomainAsync(domain).GetAwaiter().GetResult();
                    if (newDomain is null) {
                        throw new Exception($"Failed to add domain { domain }! It was null.");
                    }
                    addedDomains.Add(newDomain);
                } catch (Exception ex) {
                    failedDomains.Add(domain, ex.Message);
                }
            }

            ICommandResult? result;

            if (failedDomains.Count > 0) {
                result = new CommandErrorResult(
                    $"Failed to add one or more domains!\n" +
                    string.Join('\n', failedDomains.Select(x => $"{ x.Key }: Reason: { x.Value }"))
                );
            } else {
                result = new CommandResult($"Added { addedDomains.Count } domains!", addedDomains);
            }

            return result;
        }

        private ICommandResult Handle_RemoveDomain(params string[] args) {
            return new CommandErrorResult("This command is not yet implemented. Sorry");
        }

        private ICommandResult Handle_GetUsers(params string[] args) {
            return new CommandErrorResult("This command is not yet implemented. Sorry");
        }

        private ICommandResult Handle_GetUser(params string[] args) {
            return new CommandErrorResult("This command is not yet implemented. Sorry");
        }

        private ICommandResult Handle_AddUser(params string[] args) {
            return new CommandErrorResult("This command is not yet implemented. Sorry");
        }

        private ICommandResult Handle_RemoveUser(params string[] args) {
            return new CommandErrorResult("This command is not yet implemented. Sorry");
        }

    }
}

