using System;

namespace Hermod.Core.Commands.Results {

	/// <summary>
	/// The most basic of CommandResult implementations.
	/// </summary>
	public class CommandResult: ICommandResult {

		private static CommandResult? _empty;

		/// <summary>
		/// Provides an empty CommandResult for commands which do not require a result.
		/// </summary>
		public static CommandResult Empty => _empty ??= new CommandResult(null, null);

		public CommandResult(string? message, object? result) {
			Message = message;
			Result = result;
		}

		/// <inheritdoc/>
		public string? Message { get; }

		/// <inheritdoc/>
		public object? Result { get; }
	}
}

