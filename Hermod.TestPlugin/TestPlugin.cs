using System;

namespace Hermod.TestPlugin {

	using Config;
	using Core.Commands;
	using Core.Commands.Results;
    using PluginFramework;

	using Serilog;

	public class TestPlugin: Plugin {

		private ILogger? m_logger = null;

        public TestPlugin(): base(nameof(TestPlugin), new Version(0, 1, 0, 0)) {
			PluginCommands.Add(new TerminalCommand("test", "This is a test", "This is still just a test :)", HandleTestCommand));
			PluginCommands.Add(new TerminalCommand("error-test", "This will test error behaviours", "This test will return an error to test how Hermod handles it.", HandleTestErrorCommand));
		}

		private ICommandResult HandleTestCommand(params string[] args) {
			return new CommandResult("This was a test! Thank you for participating!", null);
		}

		private ICommandResult HandleTestErrorCommand(params string[] args) {
			return new CommandErrorResult("This was yet another test :)");
		}

		public override void OnLoad(ILogger logger) {
			logger.Information($"{ PluginName } has been loaded!");
			m_logger = logger;
		}

		public override void OnStart() {
			m_logger?.Information($"{ PluginName } has started!");
		}

		public override void OnStop() {
			m_logger?.Information($"{ PluginName } has stopped!");
		}

		public override void OnConfigChanged(ConfigChangedEventArgs e) {
			// can be safely ignored if you don't need it		
		}

		public override void OnConfigLoaded() {
			// can also safely be ignored.
		}
	}
}

