using System;

namespace Hermod.Core.Attributes {

    /// <summary>
    /// Plugin attribute, contains metadata about a given plugin.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class PluginAttribute: Attribute {

        /// <summary>
        /// Constructs a new instance of this attribute.
        /// </summary>
        /// <param name="pluginName">The name of the plugin.</param>
        /// <param name="pluginVersion">The plugin version.</param>
        public PluginAttribute(string pluginName, string pluginVersion):
            this(pluginName, pluginVersion, string.Empty, string.Empty, string.Empty) { }

        /// <summary>
        /// Constructs a new instance of this attribute.
        /// </summary>
        /// <param name="pluginName">The name of the plugin.</param>
        /// <param name="pluginVersion">The plugin's version.</param>
        /// <param name="authorName">The name of the plugin's author.</param>
        /// <param name="authorEmail">The email address of the author.</param>
        /// <param name="pluginPage">The URL for the plugin's home page.</param>
        public PluginAttribute(string pluginName, string pluginVersion, string authorName, string authorEmail, string pluginPage) {
            PluginName = pluginName;
            PluginVersion = pluginVersion;
            AuthorEmail = authorEmail;
            AuthorName = authorName;
            PluginPage = pluginPage;
        }

        /// <summary>
        /// The name of the plugin.
        /// </summary>
        string PluginName { get; }

        /// <summary>
        /// The plugin's version.
        /// </summary>
        string PluginVersion { get; }

        /// <summary>
        /// The name of the plugin's author.
        /// </summary>
        string AuthorName { get; set; }

        /// <summary>
        /// The plugin author's email.
        /// </summary>
        string AuthorEmail { get; set; }

        /// <summary>
        /// The plugin's home page URL.
        /// </summary>
        string PluginPage { get; set; }
    }
}

