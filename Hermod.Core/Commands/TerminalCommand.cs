using System;

namespace Hermod.Core.Commands {
	
	using Results;

	using getopt.net;

    /// <summary>
    /// Represents a single command that can only be executed on the (interactive) terminal.
    ///
    /// TerminalCommand instances can be called via interactive terminal and command results will be shown there.
    /// Command results are not logged.
    /// </summary>
    public class TerminalCommand: ICommand {

        public TerminalCommand(string commandName, string shortDescription, string? longDescription, Func<string[], ICommandResult> func, params Option[] args) {
			Name = commandName;
			ShortDescription = shortDescription;
			LongDescription = longDescription;
			CommandOptions = args;
			executor = func;
		}

		public string Name { get; }

		public string ShortDescription { get; }

		public string? LongDescription { get; }

		public Option[] CommandOptions { get; }

		Func<string[], ICommandResult> executor;

		public ICommandResult Execute(params string[] args) => executor(args);

		public Task<ICommandResult> ExecuteAsync(params string[] args) {
			return new TaskFactory().StartNew(() => executor(args));
		}
	}
}

