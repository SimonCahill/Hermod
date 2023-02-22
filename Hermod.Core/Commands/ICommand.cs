using System;

namespace Hermod.Core.Commands {

    using Results;

    using getopt.net;

    public interface ICommand {

        /// <summary>
        /// The (callable) name for the command.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// A short description of the command and its function.
        /// </summary>
        string ShortDescription { get; }

        /// <summary>
        /// A detailled description of the command and its function.
        /// </summary>
        string? LongDescription { get; }

        /// <summary>
        /// A list of options applicable to this command.
        /// </summary>
        Option[] CommandOptions { get; }

        /// <summary>
        /// Executes the command and returns a genericised variation of the <see cref="ICommandResult{T}"/>, namely <see cref="ICommandResult"/>.
        /// </summary>
        ICommandResult Execute(params string[] args);

        /// <summary>
        /// Asynchronous variation of <see cref="Execute(string[])"/>.
        /// </summary>
        /// <param name="args">The arguments to be passed to the command.</param>
        /// <returns>An awaitable <see cref="ICommandResult"/>.</returns>
        Task<ICommandResult> ExecuteAsync(params string[] args);

    }

    /// <summary>
    /// Specialised variation of <see cref="ICommand"/> with generic typing.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICommand<T>: ICommand {

        /// <summary>
        /// Overrides the default implementation of <see cref="ICommand"/> with a generic type approach.
        /// </summary>
        /// <param name="args">The arguments to be passed to the command.</param>
        /// <returns>The result of the command after execution.</returns>
        new ICommand<T> Execute(params string[] args);

        /// <summary>
        /// Asynchronous variation of <see cref="Execute(string[])"/>.
        /// </summary>
        /// <param name="args">The arguments to be passed to the command.</param>
        /// <returns>An awaitable <see cref="ICommandResult"/>.</returns>
        new Task<ICommandResult<T>> ExecuteAsync(params string[] args);

    }
}

