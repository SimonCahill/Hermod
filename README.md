# Hermod - An open-source, GDPR-compliant e-mail archival system for Windows, Linux and macOS

Hermod, from the Norse god of communication, is an open-source, modular and (with first-party plugins) fully GDPR-compliant email archival system.
A complete re-write of a previous project and a work project I started many years ago (which they apparently still use!), Hermod is what I always envisioned it to become.

Hermod provides a basic framework to become whatever you need it to be.

# Cross platform

Hermod was and is (at the time of writing) being developed on a Mac and is constantly tested in a Linux VM/container to ensure full compatibilit across all major operating systems.

## Advantages
 - You don't have to buy expensive Windows licenses?
 - Run it on your existing infrastructure!
 - Cross platform is a nice buzzword 🙂

# Modularity

Hermod was designed from the ground up to be 100% modular.

This means that running without any support plugins, it is virtually useless - save from executing a few little commands here and there.
Designing Hermod this way means it is fully expandable for use however required!

However, with its first-party plugins, Hermod is mainly a fully GDPR-compliant email archival solution.

## Plugins

As mentioned before, Hermod relies on plugins for its functionality.

Plugins can be loaded and unloaded at runtime, meaning they can be updated on-the-fly, or new functionality can be added whenever needed.

Plugins must fulfill some basic requirements to be loaded:

1. They must be contained within a valid `Assembly` file (MSIL .dll, .exe)
2. They must contain **at least** one class that inherits from `IPlugin` or better `Plugin`.
    - Use of the `Plugin` abstract class is recommended because it implements most of the basic funtionality.

### Plugin features

Plugins can provide their own featureset as well as their own commands.

The concept of commands isn't any different than on a "regular" terminal; open an interactive session (done in Hermod by executing it with the `-i` flag) and enter the command with any valid argument.

# Event-driven

Hermod is designed to be event-driven and supports inter-plugin communication for quick and easy transfer of data between plugins.

This is how Hermod can achieve its level of modularity without any dependency on other plugins.

If a new file is loaded, a message will be published to an internal IPC topic with the file data.

The built-in IPC system is fully internal and allows plugins to pass objects (even reference objects!) around for high performance.

# RESTful
Hermod is designed to be REST-friendly and provides (at some point 😊) a RESTful API for remote management.

## Web-Based
A web-based management portal is also being planned.

# Commands

Hermod relies on commands for handling simple things, such as starting processing, loading and unloading plugins, and much more, depending on the plugins you install!

**Built-in commands**

| Command Name  | Brief Description                                                         |
|---------------|---------------------------------------------------------------------------|
| help          | Displays a help menu with all commands and their brief description        |
| clear         | Clears the terminal and re-prints the prompt                              |
| quit          | Gracefully quits the application. (None-interactive sessions use `CTRL+C`)|
| load-plugin   | Loads a plugin from a file                                                |
| unload-plugin | Unloads a plugin from the application namespace.                          |

**Planned commands**
| Command Name  | Brief Description                                                         |
|---------------|---------------------------------------------------------------------------|
| install-plugin| Installs a new plugin from the Plugin Repository (in the far future)      |
| remove-plugin | Removes a plugin and its resources from the system.                       |
| get-cfg       | Retrieves a single config for viewing                                     |
| set-cfg       | Allows a single configuration to be set via interactive session           |

# Email support

Hermod supports parsing emails in MIME and mbox format and uses the fantastic [MimeKit and Mailkit](http://www.mimekit.net) libraries to parse emails and retrieve their attachments.

## Further Format Support

If MIME doesn't suit your needs, you can develop (and publish) your own mail parser for Hermod!

# Email archival

Hermod provides an account-based, filesystem-based archival approach with automagic compression of old files with individual deletion dates!

This means that each email domain and account will have its own directory, and each user (or domain admin) will only be allowed to access their data.

# Searchability

The first-party plugins for Hermod use [Elastic](https://www.elastic.co) bands as the back-end and search engine; this means all emails are stored securely as documents - including attachments! - and are searchable **without** having to wait for and hard drives to spin up or files to be uncompressed!
