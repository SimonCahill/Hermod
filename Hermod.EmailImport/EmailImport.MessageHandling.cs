using System;

namespace Hermod.EmailImport {

    using Core.Delegation;

    partial class EmailImporter {


        /// <summary>
        /// Event handler which is called when a message is received on a topic this plugin subscribed.
        /// </summary>
        /// <param name="sender">The <see cref="IPluginDelegator"/> instance which raised the event.</param>
        /// <param name="e">The message.</param>
        private void PluginDelegator_MessageReceived(object? sender, MessageReceivedEventArgs e) {
            switch (e.Topic) {
                default: break;
            }
        }

        private void HandleAddDomainMessageReceived(string topic, object? message) {

        }

    }
}

