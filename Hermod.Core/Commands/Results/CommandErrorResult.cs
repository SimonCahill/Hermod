using System;

namespace Hermod.Core.Commands.Results {

	/// <summary>
	/// A <see cref="ICommandResult"/> which implicates the command encountered an error.
	/// </summary>
	public class CommandErrorResult: CommandResult {

		/// <summary>
		/// Instantiates a new instance of this class.
		/// </summary>
		/// <param name="errMsg">The error message.</param>
		/// <param name="exception">(Optional) Exception that occurred.</param>
		public CommandErrorResult(string errMsg, Exception? exception = null): base(errMsg, exception) { }

		public static CommandErrorResult GetNotImplementedResult(string cmdName) => new CommandErrorResult($"The command { cmdName } has not yet been implemented.");
	}
}

