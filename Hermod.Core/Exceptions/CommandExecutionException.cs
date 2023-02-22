using System;

namespace Hermod.Core.Exceptions {

    /// <summary>
    /// Exception class that may be thrown when an error occurs during command execution.
    /// </summary>
    public class CommandExecutionException: Exception {

        /// <summary>
        /// Constructs a new instance of this class.
        /// </summary>
        /// <param name="command">The command that was executed.</param>
        /// <param name="msg">The accompanying error message.</param>
        public CommandExecutionException(string command, string? msg): base(msg) {
            Command = command;
        }

        /// <summary>
        /// The command that was executed.
        /// </summary>
        public string Command { get; }
    }
}

