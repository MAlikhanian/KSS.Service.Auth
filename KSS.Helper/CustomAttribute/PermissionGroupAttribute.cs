namespace KSS.Helper.CustomAttribute
{
    /// <summary>
    /// Declares the permission group for a controller. Used by BaseController to
    /// automatically enforce CRUD permissions based on HTTP method.
    ///
    /// Example: [PermissionGroup("Email")] will enforce:
    ///   - GET / POST Find/Single/ToList  → "Email.Read"
    ///   - POST Add/AddDto/AddRange       → "Email.Create"
    ///   - PUT Update/UpdateDto/UpdateRange → "Email.Update"
    ///   - DELETE Remove/RemoveRange       → "Email.Delete"
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class PermissionGroupAttribute : Attribute
    {
        public string Group { get; }

        public PermissionGroupAttribute(string group)
        {
            Group = group;
        }
    }
}
