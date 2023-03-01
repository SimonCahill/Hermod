using System;

namespace Hermod.EmailImport {

    using Core.Accounts;
    using Core.Commands.Results;

    using System.Text;

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
                }
               sBuilder.AppendLine();
            }

            return new CommandResult(sBuilder.ToString(), domains);
        }

        private ICommandResult Handle_GetSingleDomain(params string[] args) {
            if (args is null || args.Length == 0) {
                return new CommandErrorResult("At least one domain must be supplied!");
            }

            var domains = m_dbConnector?.GetDomainsAsync(true).GetAwaiter().GetResult().Where(d => args.Contains(d.ToString()));
            if (domains is null || !domains.Any()) {
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
            if (args is null || args.Length == 0) {
                return new CommandErrorResult("Unexpected end of domains!", new ArgumentNullException(nameof(args), "Domains must not be empty!"));
            }

            var domainsRemoved = 0;
            var domainsNotRemoved = new Dictionary<string, string>();

            foreach (var domain in args) {
                if (ExecuteCommand("get-users", domain) is not CommandErrorResult) {
                    domainsNotRemoved.Add(domain, $"{ domain } still has users! Please remove users");
                    continue;
                }

                // this will need refactoring.
                try {
                    var domainResult = ExecuteCommand("get-domain", domain);
                    if (domainResult is not null && domainResult is CommandErrorResult e) {
                        domainsNotRemoved.Add(domain, e.Message ?? "Unknown error");
                        continue;
                    } else if (domainResult is not null && domainResult is CommandResult r) {
                        if (r.Result is not Domain) {
                            domainsNotRemoved.Add(domain, r.Message ?? "Unknown error");
                            continue;
                        } else if (r.Result is Domain d) {
                            if (m_dbConnector?.RemoveDomainAsync(d).GetAwaiter().GetResult() == true) {
                                domainsRemoved++;
                            } else {
                                domainsNotRemoved.Add(domain, "Unknown error");
                            }
                        }
                    }
                } catch (Exception ex) {
                    domainsNotRemoved.Add(domain, ex.Message);
                }
            }

            if (domainsNotRemoved.Count > 0) {
                var sBuilder = new StringBuilder().AppendLine($"Removed {domainsRemoved}/{args.Length} domains!");

                foreach (var domain in domainsNotRemoved) {
                    sBuilder.Append($"\tDomain not removed: { domain.Key }. Reason: { domain.Value }.");
                }
                return new CommandErrorResult(sBuilder.ToString());
            }

            return new CommandResult($"Removed { domainsRemoved } domains.", null);
        }

        private ICommandResult Handle_GetUsers(params string[] args) {
            if (args is null || args.Length == 0) {
                return new CommandErrorResult(
                    "At least one domain must be supplied!",
                    new ArgumentNullException(nameof(args), "Command arguments must not be null or empty")
                );
            }

            var domain = ExecuteCommand("get-domain", args.First());
            if (domain is null || domain is CommandResult result && (domain.Result is null || (domain.Result as IEnumerable<Domain>).Count() == 0)) {
                return new CommandErrorResult($"Unknown error retrieving domain { args.First() }");
            } else if (domain is CommandErrorResult e) {
                return e;
            }

            var users = (domain.Result as IEnumerable<Domain>).First().DomainUsers;
            var sBuilder = new StringBuilder()
                .Append($"Got domain { args.First() } with { users.Count() } users");

            if (users.Count > 0) {
                sBuilder.AppendLine(":");

                foreach (var user in users) {
                    sBuilder.AppendLine($"\t{ user.AccountName } [{ user.AccountType }]");
                }
            }

            return new CommandResult(sBuilder.ToString(), users);
        }

        private ICommandResult Handle_GetUser(params string[] args) {
            return new CommandErrorResult("This command is not yet implemented. Sorry");
        }

        private ICommandResult Handle_AddUser(params string[] args) {
            PluginDelegator?.Warning("An attempt is being made to add a new user to a domain!");

            if (args is null || args.Length != 4) {
                return new CommandErrorResult("Insufficient arguments passed! <domain> <username> <password> <account type> required! For more info, type help add-domain");
            }

            var domainResult = ExecuteCommand("get-domain", args[0]);

            var domain = default(Domain);

            if (domainResult is null) {
                return new CommandErrorResult($"Unknown error while executing command \"get-domain {args[0]}\"");
            } else if (domainResult?.Result is CommandErrorResult e) {
                return e;
            } else if (domainResult?.Result is null) {
                return new CommandErrorResult($"Failed to retrieve domain { args[0] }! Does it exist?");
            }

            if (domainResult?.Result is IEnumerable<Domain> list) {
                domain = list.FirstOrDefault();
            } else if (domainResult?.Result is Domain d) {
                domain = d;
            }

            if (domain is null) {
                return new CommandErrorResult($"Unknown error while retrieving domain { args[0] }!");
            }

            try {
                return new CommandResult(
                    $"Added { args[1] } to { domain.DomainName }",
                    m_dbConnector?.AddUserToDomainAsync(
                        domain, args[1], args[2], Enum.Parse<AccountType>(args[3], true)
                    )
                );
            } catch (Exception ex) {
                return new CommandErrorResult($"Failed to add user { args[1] } to domain { domain.DomainName }!", ex);
            } finally {
                args = null;
            }
        }

        private ICommandResult Handle_RemoveUser(params string[] args) {
            return new CommandErrorResult("This command is not yet implemented. Sorry");
        }

    }
}

