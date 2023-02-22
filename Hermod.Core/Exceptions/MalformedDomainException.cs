using System;

namespace Hermod.Core.Exceptions {

    /// <summary>
    /// Exception class that is thrown when a malformed FQDN (fully-qualified domain name) was passed somewhere.
    /// </summary>
    public class MalformedDomainException: Exception {

        /// <summary>
        /// Constructs a new instance of this class.
        /// </summary>
        /// <param name="fqdn">The offending string that should've been an FQDN.</param>
        public MalformedDomainException(string fqdn): base("Encountered malformed FQDN!") {
            Fqdn = fqdn;
        }

        /// <summary>
        /// The offending string.
        /// </summary>
        public string Fqdn { get; }
    }
}

