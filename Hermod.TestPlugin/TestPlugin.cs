using System;

namespace Hermod.TestPlugin {

	using Config;
	using Core.Commands;
	using Core.Commands.Results;
	using Hermod.Core.Delegation;
	using PluginFramework;

	using Serilog;

	/// <summary>
	/// Defines a test plugin; which may also be used as a reference for implementing your own plugins.
	/// </summary>
	public class TestPlugin: Plugin {

		private IPluginDelegator? m_delegator = null;

        public TestPlugin(): base(nameof(TestPlugin), new Version(0, 1, 0, 0)) {
			PluginCommands.Add(new TerminalCommand("test", "This is a test", "This is still just a test :)", HandleTestCommand));
			PluginCommands.Add(new TerminalCommand("error-test", "This will test error behaviours", "This test will return an error to test how Hermod handles it.", HandleTestErrorCommand));
		}

		private ICommandResult HandleTestCommand(TerminalCommand command, params string[] args) {
			return new CommandResult("This was a test! Thank you for participating!", null);
		}

		private ICommandResult HandleTestErrorCommand(TerminalCommand command, params string[] args) {
			return new CommandErrorResult("This was yet another test :)");
		}

		public override void OnLoad(IPluginDelegator pluginDelegator) {
			pluginDelegator.Information($"{ PluginName } has been loaded!");
			m_delegator = pluginDelegator;
		}

		public override void OnStart() {
			m_delegator?.Information($"{ PluginName } has started!");
		}

		public override void OnStop() {
			m_delegator?.Information($"{ PluginName } has stopped!");
		}

		public override void OnConfigChanged(ConfigChangedEventArgs e) {
			// can be safely ignored if you don't need it		
		}

		public override void OnConfigLoaded() {
			// can also safely be ignored.
		}
	}
}

