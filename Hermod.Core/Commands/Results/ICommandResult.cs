using System;

namespace Hermod.Core.Commands.Results {

    /// <summary>
    /// A generic, object-based variation of <see cref="ICommandResult{T}"/>.
    /// </summary>
    public interface ICommandResult {

        /// <summary>
        /// An optional message.
        /// This message may be written to logs.
        /// </summary>
        string? Message { get; }

        /// <summary>
        /// The command's result.
        /// The result may be sent over the wire.
        /// </summary>
        object? Result { get; }

    }

    /// <summary>
    /// Defines a basic contract between a command and an executor to return a standardised result.
    /// </summary>
    public interface ICommandResult<T>: ICommandResult {

        /// <summary>
        /// The command's result.
        /// The result may be sent over the wire.
        /// </summary>
        new T Result { get; }

    }
}

