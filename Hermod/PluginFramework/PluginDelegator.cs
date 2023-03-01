using System;

namespace Hermod.PluginFramework {

    using Config;
    using Core.Commands.Results;
    using Core.Delegation;
    using Serilog;

    /// <summary>
    /// Allows delegating topics and command execution requests from plugins through Hermod to other plugins.
    /// </summary>
    public class PluginDelegator: IPluginDelegator {

        internal IPlugin Plugin { get; }
        internal ILogger? Logger { get; }

        /// <summary>
        /// Instantiates a new instance of this class.
        /// </summary>
        /// <param name="plugin">The plugin this delegator handles.</param>
        public PluginDelegator(IPlugin plugin, ILogger? logger = null) {
            Plugin = plugin;
            Logger = logger;
        }

        #region IMessageReceived
        /// <inheritdoc/>
        public event MessageReceivedEventHandler? MessageReceived;

        internal void OnMessageReceived(MessageReceivedEventArgs e) => MessageReceived?.Invoke(this, e);
        #endregion

        /// <inheritdoc/>
        public void SubscribeTopic(string topicName) => PluginRegistry.Instance.AddSubscription(Plugin, topicName);

        /// <inheritdoc/>
        public void SubscribeTopics(params string[] topics) {
            foreach (var topic in topics) {
                try { PluginRegistry.Instance.AddSubscription(Plugin, topic); } catch (Exception ex) {
                    Error($"Subscription failed: {ex.Message}");
                    Debug($"Stacktrace: {ex.StackTrace}");
                }
            }
        }

        /// <inheritdoc/>
        public void UnsubscribeTopic(string topicName) => PluginRegistry.Instance.RemoveSubscription(Plugin, topicName);

        /// <inheritdoc/>
        public void PublishMessage(string topic, object? message) => PluginRegistry.Instance.OnMessagePublished(topic, message);

        /// <inheritdoc/>
        public ICommandResult ExecuteCommand(params string[] command) => PluginRegistry.Instance.ExecuteCommand(command);

        /// <inheritdoc/>
		public void Information(string? msg) => Logger?.Information($"[{ Plugin.PluginName }] {msg}");

        /// <inheritdoc/>
		public void Debug(string? msg) => Logger?.Debug($"[{ Plugin.PluginName }] {msg}");

        /// <inheritdoc/>
		public void Error(string? msg) => Logger?.Error($"[{ Plugin.PluginName }] {msg}");

        /// <inheritdoc/>
		public void Warning(string? msg) => Logger?.Warning($"[{ Plugin.PluginName }] {msg}");

        /// <inheritdoc/>
		public void Trace(string? msg) => Logger?.Verbose($"[{ Plugin.PluginName }] {msg}");

        /// <inheritdoc/>
        public T GetApplicationConfig<T>(string config) => ConfigManager.Instance.GetConfig<T>(config);

        /// <inheritdoc/>
        public bool TryGetApplicationConfig<T>(string config, out T? value) {
            try {
                value = GetApplicationConfig<T>(config);
            } catch {
                value = default;
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public void SetApplicationConfig<T>(string config, T value) {
            ConfigManager.Instance.SetConfig(config, value);
            ConfigManager.Instance.SaveConfig();
        }

        /// <inheritdoc/>
        public bool TrySetApplicationConfig<T>(string config, T value) {
            try { SetApplicationConfig(config, value); } catch { return false; }

            return true;
        }
    }
}

