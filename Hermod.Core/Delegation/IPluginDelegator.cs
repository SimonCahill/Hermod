using System;

namespace Hermod.Core.Delegation {

    using Commands.Results;

    /// <summary>
    /// Basic contract between Hermod and any laoded plugins which allows jobs to be delegated to other plugins.
    ///
    /// Each plugin will receive an instance of IPluginDelegator (provided by Hermod),
    /// which can then be used to communicate with all other plugins, if need be.
    ///
    /// Each IPluginDelegator will provide a basic means of IPC (inter-plugin communication) and thus
    /// also a means of communicating with Hermod.
    ///
    /// # Publishing a message
    /// When publishing a message for IPC, the topic string must conform to the following pattern:
    /// Failure to adhere to this pattern will result in an exception!
    ///  - Must not be empty or contain only whitespace
    ///  - Must contain only alphanumeric characters or valid wildcards
    ///  - Must begin with a slash (/)
    ///  - Topic levels must be separated by a slah (/)
    ///  - Wildcards must be prefixed by a slash
    ///  - Topics must **not** only consist of only a slash (/)
    ///
    /// Any object type may be published to any topic. Uncaught exceptions will be handled by Hermod.
    /// The publisher will not receive feedback about the published messages.
    /// Published messages will **NOT** be published over any networks!
    /// </summary>
    public interface IPluginDelegator: IMessagePublished {

        /// <summary>
        /// Allows a plugin to subscribe to an individual topic.
        /// </summary>
        /// <param name="topicName">The topic to subscribe to</param>
        /// <exception cref="Exceptions.MalformedTopicException" >If the topic does not meet the topic string requirements.</exception>
        void SubscribeTopic(string topicName);

        /// <summary>
        /// Allows a plugin to subscribe to multiple individual topics.
        /// </summary>
        /// <remarks >
        /// This method will fail silently and ignore any non-conforming topics.
        /// </remarks>
        /// <param name="topics">The topics to subscribe to.</param>
        void SubscribeTopics(params string[] topics);

        /// <summary>
        /// Allows a plugin to unsubscribe from a single topic.
        /// </summary>
        /// <param name="topicName"></param>
        void UnsubscribeTopic(string topicName);

        /// <summary>
        /// Allows a plugin to publish a message on a given topic.
        /// </summary>
        /// <param name="topic">The topic to publish to.</param>
        /// <param name="message">The message to publish to any subscribed plugins.</param>
        void PublishMessage(string topic, object? message);

        /// <summary>
        /// Executes a single command.
        /// </summary>
        /// <remarks >
        /// If command execution has been disabled for this plugin, then a <see cref="Exceptions.CommandExecutionException"/> will be raised.
        /// </remarks>
        /// <param name="command">The command to be executed.</param>
        /// <returns>The result of the command.</returns>
        /// <exception cref="Exceptions.CommandExecutionException" >If an error occurs during command execution.</exception>
        ICommandResult ExecuteCommand(params string[] command);

        /// <summary>
        /// Logs an information message to the logger.
        /// </summary>
        /// <remarks >
        /// This will prefix the message with [name of plugin].
        /// </remarks>
        /// <param name="msg">The message to log.</param>
        void Information(string? msg);

        /// <summary>
        /// Logs a debug message to the logger.
        /// </summary>
        /// <remarks >
        /// This will prefix the message with [name of plugin].
        /// </remarks>
        /// <param name="msg">The message to log.</param>
        void Debug(string? msg);

        /// <summary>
        /// Logs an error message to the logger.
        /// </summary>
        /// <remarks >
        /// This will prefix the message with [name of plugin].
        /// </remarks>
        /// <param name="msg">The message to log.</param>
        void Error(string? msg);

        /// <summary>
        /// Logs a warning message to the logger.
        /// </summary>
        /// <remarks >
        /// This will prefix the message with [name of plugin].
        /// </remarks>
        /// <param name="msg">The message to log.</param>
        void Warning(string? msg);

        /// <summary>
        /// Logs a trace message to the logger.
        /// </summary>
        /// <remarks >
        /// This will prefix the message with [name of plugin].
        /// </remarks>
        /// <param name="msg">The message to log.</param>
        void Trace(string? msg);

    }


}

