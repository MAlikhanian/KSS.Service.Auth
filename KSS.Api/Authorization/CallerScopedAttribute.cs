namespace KSS.Api.Authorization
{
    /// <summary>
    /// Declares that an action operates only on the identity of the authenticated
    /// caller and accepts no target identifier. Satisfies the explicit-authorization
    /// requirement without a permission code.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class CallerScopedAttribute : Attribute
    {
    }
}
