using System;

namespace Hermod.Core.Accounts {

    /// <summary>
    /// This class represents a single user (email account) in a domain.
    ///
    /// Each <see cref="Domain"/> may contain N <see cref="DomainUser"/> objects.
    /// This object is tightly tied to the operation of Hermod.
    /// </summary>
    public sealed class DomainUser {

        /// <summary>
        /// Creates a new instance of this object.
        /// </summary>
        /// <param name="id">The user's ID. Leave to -1 if a new account is being created.</param>
        /// <param name="accountName">The account name.</param>
        /// <param name="encryptedPassword">The encrypted account password.</param>
        /// <param name="passwordSalt">The password salt.</param>
        /// <param name="accType">The account type.</param>
        public DomainUser(int id, string accountName, string encryptedPassword, string passwordSalt, AccountType accType = AccountType.Imap) {
            Id = id;
            AccountName = accountName;
            EncryptedPassword = encryptedPassword;
            AccountType = accType;
            PasswordSalt = passwordSalt;
        }

        /// <summary>
        /// The domain user's ID.
        /// </summary>
        /// <remarks >
        /// If this is set to -1, an ID will automagically be generated.
        /// </remarks>
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

        /// <summary>
        /// The <see cref="AccountType"/> this account is configured for use with.
        /// </summary>
        public AccountType AccountType { get; set; }

        /// <summary>
        /// Gets or sets the salt to use for password encryption.
        /// </summary>
        public string PasswordSalt { get; set; }

    }
}

