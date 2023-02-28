using System;

namespace Hermod.Core.Exceptions {

    /// <summary>
    /// Exception class that is thrown when an attempt is made to add a domain which already exists.
    /// </summary>
    public class DomainAlreadyExistsException: Exception {

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="msg">A brief description of the error.</param>
        public DomainAlreadyExistsException(string msg): base(msg) { }
    }
}

