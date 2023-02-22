using System;

namespace Hermod.Core.Exceptions {

    /// <summary>
    /// Exception class that is thrown when a topic for IPC is malformed.
    /// </summary>
    public class MalformedTopicException: Exception {

        /// <summary>
        /// Constructs a new instance of this class.
        /// </summary>
        /// <param name="topic">The topic that is malformed.</param>
        /// <param name="message">An accompanying message.</param>
        public MalformedTopicException(string topic, string? message): base(message) {
            Topic = topic;
        }

        /// <summary>
        /// The malformed topic.
        /// </summary>
        public string Topic { get; }
    }
}

