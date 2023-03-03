using System;

namespace Hermod {

    using Core.Commands;
    using Core.Commands.Results;
    using PluginFramework;

    using System.Text;

    public partial class Hermod {

        private bool TryGetCommand(string cmdName, out ICommand? outCommand) {
            bool Predicate(ICommand x) => x.Name.Equals(cmdName, StringComparison.CurrentCulture);

            // built-ins have priority
            outCommand = Commands.FirstOrDefault(Predicate);

            if (outCommand != null) { return true; }

            foreach (var plugin in PluginRegistry.Instance.Plugins) {
                outCommand = plugin.PluginCommands.FirstOrDefault(Predicate);

                if (outCommand != null) {
                    return true;
                }
            }

            return false;
        }

        List<ICommand> Commands => m_commands ??= new List<ICommand> {
            new TerminalCommand(
                "clear", "Clears the terminal of any text",
                "Clears the internal buffers of the terminal.",
                x => { Console.Clear(); return CommandResult.Empty; }
            ),
            new TerminalCommand(
                "quit", "Gracefully terminates Hermod.",
                "Unloads all plugins, shuts down internal services,\n" +
                "and gracfully shuts Hermod down. Useful for maintenance.",
                x => { m_keepAlive = false; return CommandResult.Empty; }
            ),
            new TerminalCommand(
                "help", "Displays a help text",
                "Prints general help about all commands found\n" +
                "in Hermod and any loaded plugins.\n" +
                "Type help <command> for detailled information.",
                HandleDisplayHelp
            ),
            new TerminalCommand(
                "load-plugin", "Loads one or more plugins",
                "Loads one or more plugins from disk and places\n" +
                "them in the application's namespace.\n" +
                "Usage:\n" +
                "\tload-plugin <plugin-file [<plugin-file>]>",
                HandleLoadPlugin
            ),
            new TerminalCommand(
                "unload-plugin", "Unloads one or more plugins from Hermod",
                "Unloads one or more plugins from Hermod's namespace.\n" +
                "Plugins are gracefully stopped before being unloaded.\n" +
                "Usage:\n" +
                "\tunload-plugin <plugin-name> # unload a single plugin\n" +
                "\tunload-plugin <plugin-name [<plugin-name>]> # unload multiple plugins\n" +
                "\tunload-plugin --all, -a # unload all plugins",
                HandleUnloadPlugin,
                new getopt.net.Option { Name = "all", ArgumentType = getopt.net.ArgumentType.None, Value = 'a' }
            )
        };

        private ICommandResult HandleDisplayHelp(params string[] args) {
            if (args.Length > 0) { return HandleDisplayCommandHelp(args[1]); }

            var sBuilder = new StringBuilder();

            void DumpCommandShortHelp(ICommand command) {
                sBuilder.AppendLine($"{ command.Name,-30 }{ command.ShortDescription,-80 }");
            }

            sBuilder.AppendLine("Built-ins:");

            foreach (var command in Commands) {
                DumpCommandShortHelp(command);
            }
            sBuilder.AppendLine();

            sBuilder.AppendLine("Plugin provided:");
            foreach (var plugin in PluginRegistry.Instance.Plugins) {
                sBuilder.AppendLine($"{ plugin.PluginName }:");
                foreach (var command in plugin.PluginCommands) {
                    DumpCommandShortHelp(command);
                }
                sBuilder.AppendLine();
            }

            return new CommandResult(sBuilder.ToString(), null);
        }

        private ICommandResult HandleDisplayCommandHelp(string arg) {
            var command =
                PluginRegistry.Instance.GetAllCommands()
                                       .FirstOrDefault(c => c.Name.Equals(arg, StringComparison.InvariantCulture));
            if (command is null) {
                return new CommandErrorResult($"Command \"{ arg }\" doesn't exist!");
            }

            return new CommandResult(command.LongDescription, null);
        }

        private ICommandResult HandleLoadPlugin(params string[] args) {
            if (args.Length == 0) {
                return new CommandErrorResult("Missing input parameters!", new ArgumentNullException(nameof(args), "Input args must not be null or empty!"));
            }

            foreach (var arg in args) {
                if (string.IsNullOrEmpty(arg)) {
                    m_appLogger.Warning($"Encountered empty argument in { nameof(HandleLoadPlugin) }. Ignoring...");
                    continue;
                }

                try {
                    var fInfo = new FileInfo(arg);
                    PluginRegistry.Instance.LoadPlugin(fInfo);
                    PluginRegistry.Instance.LastRegisteredPlugin?.OnStart();
                } catch (Exception ex) {
                    return new CommandErrorResult("Failed to load one or more plugins!", ex);
                }
            }

            return new CommandResult($"Successfully loaded plugin(s).", null);
        }

        private ICommandResult HandleUnloadPlugin(params string[] args) {
            return new CommandErrorResult("This command has not yet been implemented!");
        }

        private ICommandResult HandleGetPlugins(params string[] args) {
            var sBuilder = new StringBuilder();
            PluginRegistry.Instance.Plugins
                .Select(x => $"{ x.PluginName } v{ x.PluginVersion }")
                .ToList()
                .ForEach(x => sBuilder.AppendLine(x));

            return new CommandResult(sBuilder.ToString(), null);
        }

    }
}

