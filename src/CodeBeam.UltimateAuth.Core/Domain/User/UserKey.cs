namespace CodeBeam.UltimateAuth.Core.Domain
{
    public readonly record struct UserKey
    {
        public string Value { get; }

        private UserKey(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a UserKey from a GUID (default and recommended).
        /// </summary>
        public static UserKey FromGuid(Guid value) => new(value.ToString("N"));

        /// <summary>
        /// Creates a UserKey from a canonical string.
        /// Caller is responsible for stability and uniqueness.
        /// </summary>
        public static UserKey FromString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("UserKey cannot be empty.", nameof(value));

            return new UserKey(value);
        }

        /// <summary>
        /// Generates a new GUID-based UserKey.
        /// </summary>
        public static UserKey New() => FromGuid(Guid.NewGuid());

        public override string ToString() => Value;

        public static implicit operator string(UserKey key) => key.Value;
    }

}
