using System;

namespace Hermod.Core.Accounts {

    /// <summary>
    /// This class represents a single user (email account) in a domain.
    ///
    /// Each <see cref="Domain"/> may contain N <see cref="DomainUser"/> objects.
    /// This object is tightly tied to the operation of Hermod.
    /// </summary>
    public sealed class DomainUser {

        public DomainUser(int id, string accountName, string encryptedPassword) {
            Id = id;
            AccountName = accountName;
            EncryptedPassword = encryptedPassword;
        }

        /// <summary>
        /// The domain user's ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The account name (i.e. the login name)
        /// </summary>
        public string AccountName { get; set; }

        /// <summary>
        /// The encrypted user password.
        /// Hermod does not and will never support using unencrypted passwords!
        /// </summary>
        public string EncryptedPassword { get; set; }

    }
}

