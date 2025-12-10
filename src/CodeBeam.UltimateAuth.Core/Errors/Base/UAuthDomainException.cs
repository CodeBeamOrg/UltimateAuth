namespace CodeBeam.UltimateAuth.Core.Errors
{
    /// <summary>
    /// Represents an exception triggered by a violation of UltimateAuth's domain rules or invariants.
    /// These errors indicate that a business rule or authentication domain constraint has been broken (e.g., invalid session state transition,
    /// illegal revoke action, or inconsistent security version).
    /// </summary>
    public abstract class UAuthDomainException : UAuthException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UAuthDomainException"/> class with a message describing the violated domain rule.
        /// </summary>
        /// <param name="message">The descriptive message for the domain error.</param>
        protected UAuthDomainException(string message) : base(message) { }
    }
}
