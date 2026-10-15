namespace KSS.Service.IService
{
    /// <summary>
    /// Resolves an incoming hostname to the Company that host belongs to.
    ///
    /// Used by self-registration: a user who signs up on a tenant's hostname is
    /// assigned to that tenant's company. The mapping is read from Auth's own
    /// SERVER-SIDE configuration (TENANT_HOSTS), never from the request body —
    /// Register is [AllowAnonymous], so a caller-supplied company id would let
    /// anyone register into any tenant.
    ///
    /// Note this is deliberately NOT the Shell's TENANTS value. That one is
    /// served to browsers for logos and banners and is therefore not a
    /// trustworthy authority; this one is not client-reachable.
    /// </summary>
    public interface ITenantHostResolver
    {
        /// <summary>
        /// The company bound to <paramref name="host"/>, or null when the host is
        /// unknown, blank or the configuration is missing/malformed. Callers must
        /// treat null as "no tenant" and create no membership — never as a reason
        /// to fall back to some default company.
        /// </summary>
        Guid? ResolveCompanyId(string? host);
    }
}
