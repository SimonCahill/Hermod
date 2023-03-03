using System;

namespace Hermod.Core.Accounts {

    using System.Security.Cryptography;

    /// <summary>
    /// This class represents a single user (email account) in a domain.
    ///
    /// Each <see cref="Domain"/> may contain N <see cref="DomainUser"/> objects.
    /// This object is tightly tied to the operation of Hermod.
    /// </summary>
    public sealed class DomainUser {

        /// <summary>
        /// The required salt size.
        /// </summary>
        public const int SaltSize = 4096;

        /// <summary>
        /// Creates a new instance of this object.
        /// </summary>
        /// <param name="id">The user's ID. Leave to -1 if a new account is being created.</param>
        /// <param name="accountName">The account name.</param>
        /// <param name="encryptedPassword">The encrypted account password.</param>
        /// <param name="passwordSalt">The password salt.</param>
        /// <param name="accType">The account type.</param>
        public DomainUser(int id, string accountName, byte[] encryptedPassword, byte[] passwordSalt, AccountType accType = AccountType.Imap) {
            if (passwordSalt is null || passwordSalt.Length != SaltSize || passwordSalt.All(b => b == 0) || passwordSalt.All(b => b == passwordSalt[0])) {
                throw new ArgumentException($"Password salt must be { SaltSize }b and must contain random bytes!", nameof(passwordSalt));
            }

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
        public byte[] EncryptedPassword { get; set; }

        /// <summary>
        /// The <see cref="AccountType"/> this account is configured for use with.
        /// </summary>
        public AccountType AccountType { get; set; }

        /// <summary>
        /// Gets or sets the salt to use for password encryption.
        /// </summary>
        public byte[] PasswordSalt { get; set; }

        /// <summary>
        /// Gets or sets the last time emails were imported from this account.
        /// </summary>
        public DateTime LastEmailRetrieval { get; set; }

        /// <summary>
        /// Generates <see cref="SaltSize"/> cryptographically random bytes and stores them in <paramref name="container"/>.
        /// </summary>
        /// <param name="container">A reference to the container to store the bytes in.</param>
        public static void GenerateEntropy(ref byte[] container) {

            using var rng = RandomNumberGenerator.Create();

            if (container is null) {
                container = new byte[SaltSize];
            } else {
                Array.Resize(ref container, SaltSize);
            }

            rng.GetBytes(container, 0, SaltSize);
        }

    }
}

