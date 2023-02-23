using System;

namespace Hermod.Core.Accounts {

    /// <summary>
    /// Enumerates the different types of account types that are supported.
    /// </summary>
    public enum AccountType {

        /// <summary>
        /// The account uses the IMAP protocol to retrieve data.
        /// </summary>
        /// <remarks >
        /// Use of the IMAP protocol is inefficient, compared to POP3 because IMAP accounts
        /// keep their emails on the server, meaning Hermod will have to determine if the email
        /// has previously been indexed, whereas POP3 means the emails are deleted automatically.
        ///
        /// IMAP remains the default, however, as not many people wish to create separate inboxes.
        /// </remarks>
        Imap = 0,

        /// <summary>
        /// The account uses POP3 to retrieve data.
        /// </summary>
        /// <remarks >
        /// This is the recommended protocol for use with Hermod, as POP3 accounts delete the files from
        /// the server, meaning that Hermod does not have to check each email if it has previously been indexed.
        /// </remarks>
        Pop3 = 1

    }

}

