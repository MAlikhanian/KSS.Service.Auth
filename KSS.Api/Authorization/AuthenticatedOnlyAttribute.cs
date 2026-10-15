namespace KSS.Api.Authorization
{
    /// <summary>
    /// Declares that an action is available to any authenticated caller and is
    /// deliberately not gated by a permission code. Distinct from
    /// <see cref="CallerScopedAttribute"/>: the action may act on identifiers the
    /// caller supplies. The reason is required and records why no permission applies.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class AuthenticatedOnlyAttribute : Attribute
    {
        public string Reason { get; }

        public AuthenticatedOnlyAttribute(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("A reason is required.", nameof(reason));

            Reason = reason;
        }
    }
}
