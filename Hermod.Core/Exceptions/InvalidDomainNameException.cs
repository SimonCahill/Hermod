using System;

namespace Hermod.Core.Exceptions {

    /// <summary>
    /// Exception class that is thrown when an invalid domain name is encountered.
    /// </summary>
    public class InvalidDomainNameException: Exception {

        public InvalidDomainNameException(string msg): base(msg) { }

        public InvalidDomainNameException(string domainName, string msg) : base(msg) {
            DomainName = domainName;
        }

        /// <summary>
        /// The offending domain.
        /// </summary>
        public string? DomainName { get; }
    }
}

