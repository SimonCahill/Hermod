using System;

namespace Hermod.Core.Commands {
    
    using Results;

    using getopt.net;

    /// <summary>
    /// Command handler delegate.
    /// </summary>
    /// <param name="command">The command being executed.</param>
    /// <param name="args">An array of arguments passed to the command.</param>
    /// <returns></returns>
    public delegate ICommandResult TerminalCommandHandler(TerminalCommand command, params string[] args);

    /// <summary>
    /// Represents a single command that can only be executed on the (interactive) terminal.
    ///
    /// TerminalCommand instances can be called via interactive terminal and command results will be shown there.
    /// Command results are not logged.
    /// </summary>
    public class TerminalCommand: ICommand {

        /// <summary>
        /// Creates a new instance of <see cref="TerminalCommand"/>.
        /// </summary>
        /// <param name="commandName">The (callable) name of the command.</param>
        /// <param name="shortDescription">A brief description of the command.</param>
        /// <param name="longDescription">A detailled description of the command.</param>
        /// <param name="handler">The command handler delegate. <see cref="TerminalCommandHandler"/></param>
        /// <param name="args">An optional list of <see cref="Option"/> representing possible arguments for the command.</param>
        public TerminalCommand(string commandName, string shortDescription, string? longDescription, TerminalCommandHandler handler, params Option[] args) {
            Name = commandName;
            ShortDescription = shortDescription;
            LongDescription = longDescription;
            CommandOptions = args;
            commandHandler = handler;
        }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public string ShortDescription { get; }

        /// <inheritdoc/>
        public string? LongDescription { get; }

        /// <inheritdoc/>
        public Option[] CommandOptions { get; }

        /// <summary>
        /// Instance of a command handler 
        /// </summary>
        TerminalCommandHandler? commandHandler;

        /// <summary>
        /// Executes the terminal command.
        /// </summary>
        /// <param name="args">A list of arguments to pass to the command.</param>
        /// <returns>The <see cref="ICommandResult"/> of the executed command.</returns>
        /// <exception cref="NullReferenceException">If <see cref="commandHandler"/> is not set to a reference of an instance.</exception>
        public ICommandResult Execute(params string[] args) {
            if (commandHandler is null) {
                throw new NullReferenceException("The command handler is not set to a reference of an instance!");
            }

            return commandHandler(this, args);
        }

        /// <summary>
        /// Asynchronously executes the terminal command.
        /// </summary>
        /// <param name="args">A list of arguments to pass to the command.</param>
        /// <returns>The <see cref="ICommandResult"/> of the executed command.</returns>
        /// <exception cref="NullReferenceException">If <see cref="commandHandler"/> is not set to a reference of an instance.</exception>
        public Task<ICommandResult> ExecuteAsync(params string[] args) => Task.FromResult(Execute(args));

        private GetOpt? m_argParser = null;

        /// <summary>
        /// Parses each argument individually.
        /// </summary>
        /// <param name="args">The argument list passed to the command.</param>
        /// <returns>The option value of the current command.</returns>
        /// <remarks >
        /// This method should use <see cref="GetOpt"/> in the background to provide full compatibility.
        /// Once the end of the argument list has been reached, <see cref="m_argParser"/> will be set to <code >null.</code>
        /// </remarks>
        public int ParseArgs(ref string[] args, out string? optArg) {
            if (m_argParser is null) {
                m_argParser = new GetOpt { AppArgs = args, Options = CommandOptions, AllExceptionsDisabled = true, AllowWindowsConventions = true };
            }

            var result = m_argParser.GetNextOpt(out optArg);

            if (result == -1) { m_argParser = null; }

            return result;
        }
    }
}

