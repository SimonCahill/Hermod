using System;

namespace Hermod.Core.Delegation {

    /// <summary>
    /// Event handler delegate for the MessageReceived event.
    /// </summary>
    /// <param name="sender">The object that fired the event.</param>
    /// <param name="e">The event arguments.</param>
    public delegate void MessageReceivedEventHandler(object? sender, MessageReceivedEventArgs e);

    /// <summary>
    /// EventArgs-derived class containing the event arguments for when a message was received on a given topic.
    /// </summary>
    public class MessageReceivedEventArgs: EventArgs {

        /// <summary>
        /// Constructs a new instance of this class.
        /// </summary>
        /// <param name="topic">The topic on which the message was sent.</param>
        /// <param name="message">The message that was sent.</param>
        public MessageReceivedEventArgs(string topic, object? message): base() {
            Topic = topic;
            Message = message;
        }

        /// <summary>
        /// The topic on which the message was sent.
        /// </summary>
        public string Topic { get; }

        /// <summary>
        /// The message that was sent.
        /// </summary>
        public object? Message { get; }

    }

    /// <summary>
    /// Provides a standardised method of inter-plugin, message-based communication.
    ///
    /// This interface allows all plugins to receive topics they have subscribed to.
    /// </summary>
    public interface IMessagePublished {

        /// <summary>
        /// An event that is fired by Hermod when a message was published by a plugin or Hermod.
        /// </summary>
        public event MessageReceivedEventHandler? MessageReceived;

    }
}

