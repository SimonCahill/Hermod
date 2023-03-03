using System;

namespace Hermod.PluginFramework {

	using Config;
	using Core.Commands;
	using Core.Commands.Results;
	using Core.Delegation;

	using Serilog;

    /// <summary>
    /// An abstract class for plugins with all the main features already implemented.
    /// </summary>
    public abstract class Plugin: IPlugin {

		/// <summary>
		/// The <see cref="IPluginDelegator"/> instance for this class and plugin.
		/// </summary>
		/// <remarks >
		/// This is set in <see cref="OnLoad(IPluginDelegator)"/>
		/// </remarks>
		protected IPluginDelegator? PluginDelegator { get; private set; } = null;

		/// <summary>
		/// Specialised constructor; allows inheriting classes to set their values immediately.
		/// </summary>
		/// <param name="pluginName">The name of the plugin.</param>
		/// <param name="pluginVersion">The plugin's version.</param>
		/// <param name="commands">A list of commands (if any).</param>
		public Plugin(string pluginName, Version pluginVersion, params ICommand[] commands) {
			PluginVersion = pluginVersion;
			PluginName = pluginName;
			PluginCommands = commands.ToList();
		}

		/// <inheritdoc/>
		public Version PluginVersion { get; protected set; }

		/// <inheritdoc/>
		public string PluginName { get; protected set; }

		/// <inheritdoc/>
		public List<ICommand> PluginCommands { get; protected set; }

		/// <inheritdoc/>
		public virtual void OnLoad(IPluginDelegator pluginDelegator) {
			PluginDelegator = pluginDelegator;
			PluginDelegator.MessageReceived += PluginDelegator_MessageReceived;
		}

		/// <inheritdoc/>
		public abstract void OnStart();

		/// <inheritdoc/>
		public abstract void OnStop();

		/// <inheritdoc/>
		public virtual void OnConfigChanged(ConfigChangedEventArgs e) { }

		/// <inheritdoc/>
		public virtual void OnConfigLoaded() { }

		/// <summary>
		/// Method which is called by the abstract <see cref="Plugin"/> class when a subscribed message is received.
		/// </summary>
		/// <remarks >
		/// This method may be overriden by inheriting classes.
		/// </remarks>
		/// <param name="topic">The topic on which a message was published.</param>
		/// <param name="message">The received message.</param>
		protected virtual void OnMessageReceived(string topic, object? message) { }

		/// <summary>
		/// Event handler for the IPluginDelegator.MessageReceived event.
		/// </summary>
		/// <param name="sender">Is always the <see cref="IPluginDelegator"/> instance assigned to this object.</param>
		/// <param name="e">The event arguments.</param>
		private void PluginDelegator_MessageReceived(object? sender, MessageReceivedEventArgs e) {
			OnMessageReceived(e.Topic, e.Message);
		}

		/// <summary>
		/// Executes a command in Hermod and returns the result.
		/// </summary>
		/// <param name="args">The command and its arguments.</param>
		/// <returns>An instance of <see cref="ICommandResult"/>. Usually either <see cref="CommandResult"/> or <see cref="CommandErrorResult"/>.</returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="NullReferenceException"></exception>
		protected ICommandResult? ExecuteCommand(params string[] args) {
			if (args is null || args.Length == 0) { throw new ArgumentNullException(nameof(args), "Arguments must not be null or empty!"); }
			if (PluginDelegator is null) { throw new NullReferenceException($"The IPluginDelegator for { PluginName } has not yet been loaded! Did you override OnLoad()?"); }

			return PluginDelegator.ExecuteCommand(args);
		}

		/// <summary>
		/// Publishes a message to the internal broker.
		/// </summary>
		/// <param name="topic">The topic on which to publish the message.</param>
		/// <param name="message">The message object.</param>
		/// <exception cref="ArgumentNullException">If the topic was empty or null.</exception>
		/// <exception cref="NullReferenceException">If <see cref="PluginDelegator"/> was null.</exception>
		protected virtual void PublishMessage(string topic, object? message) {
			if (string.IsNullOrEmpty(topic?.Trim())) { throw new ArgumentNullException(nameof(topic), "Topic must not be null or empty!"); }

			if (PluginDelegator is null) { throw new NullReferenceException($"Plugin delegator was null! Did you override { nameof(OnLoad) }?"); }

			PluginDelegator.PublishMessage(topic, message);
		}
	}
}

