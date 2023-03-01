using System;

namespace Hermod.PluginFramework {

    using Core.Commands.Results;
    using Core.Delegation;
    using Core.Exceptions;
    using System.Text.RegularExpressions;

    partial class PluginRegistry {

        /// <summary>
        /// The topic subscription list.
        /// </summary>
        internal Dictionary<string, List<IPlugin>> TopicSubscriptions { get; } = new Dictionary<string, List<IPlugin>>();

        /// <summary>
        /// Adds a plugin to a topic subscription list.
        /// </summary>
        /// <param name="plugin">The plugin to add.</param>
        /// <param name="topic">The topic to subscribe to.</param>
        /// <exception cref="MalformedTopicException"></exception>
        internal void AddSubscription(IPlugin plugin, string topic) {
            if (!TopicIsValid(ref topic)) {
                throw new MalformedTopicException(topic, "The topic is invalid!");
            }

            if (!TopicSubscriptions.ContainsKey(topic)) {
                TopicSubscriptions.Add(topic, new List<IPlugin>());
            }

            var subList = TopicSubscriptions[topic];
            if (subList.Contains(plugin)) { return; }

            subList.Add(plugin);
        }

        /// <summary>
        /// Removes a plugin from a topic subscription list.
        /// </summary>
        /// <param name="plugin">The plugin to remove from the list.</param>
        /// <param name="topic">The topic to unsubscribe the plugin from.</param>
        /// <exception cref="MalformedTopicException">If the passed topic is invalid or malformed.</exception>
        internal void RemoveSubscription(IPlugin plugin, string topic) {
            if (!TopicIsValid(ref topic)) {
                throw new MalformedTopicException(topic, "The topic is invalid!");
            }

            if (!TopicSubscriptions.ContainsKey(topic)) { return; }

            var subList = TopicSubscriptions[topic];
            if (!subList.Contains(plugin)) { return; }

            subList.Remove(plugin);
        }

        [GeneratedRegex("^[A-z0-9-_Ä-ü]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public static partial Regex TopicRegex();

        /// <summary>
        /// Gets a value indicating whether or not a given topic is valid.
        /// </summary>
        /// <param name="topic">The topic to check for validity.</param>
        /// <returns><code >true</code> if the topic is valid. <code >false</code> otherwise.</returns>
        bool TopicIsValid(ref string topic) {
            if (
                string.IsNullOrEmpty(topic)         ||
                string.IsNullOrWhiteSpace(topic)    ||
                topic[0] != '/'                     ||
                topic.EndsWith('/')
            ) {
                return false;
            }

            var splitTopic = topic.Split('/').Where(x => x != "/").Where(x => !TopicRegex().IsMatch(x));

            return splitTopic.Count() > 0;
        }

        internal void OnMessagePublished(string topic, object? message) {
            if (!TopicIsValid(ref topic)) { throw new MalformedTopicException(topic, "The given topic is invalid!"); }

            if (!TopicSubscriptions.ContainsKey(topic)) {
                TopicSubscriptions.Add(topic, new List<IPlugin>());
                return; // this may change in the future.
            }

            var eventArgs = new MessageReceivedEventArgs(topic, message);
            foreach (var subscriber in TopicSubscriptions[topic]) {
                PluginDelegators.FirstOrDefault(p => p.Plugin == subscriber)?.OnMessageReceived(eventArgs);
            }
        }

        internal ICommandResult ExecuteCommand(params string[] commands) {
            var cmd = GetAllCommands().FirstOrDefault(c => c.Name == commands[0]);

            if (cmd is null) {
                return new CommandErrorResult($"The command { commands[0] } does not exist in Hermod's namespace! Are the correct plugins loaded?");
            }

            return cmd.Execute(commands);
        }

    }
}

