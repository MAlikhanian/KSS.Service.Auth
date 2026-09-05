using System.Text.Json;
using KSS.Service.IService;
using Microsoft.Extensions.Configuration;

namespace KSS.Service.Service
{
    /// <summary>
    /// Reads TENANT_HOSTS — a JSON object of hostname to Company id, e.g.
    ///
    ///   {"erp.seba.ir":"019f317a-12ba-764f-b19b-e064e36b4be8", ...}
    ///
    /// Same deployment shape as the Shell's ZONES/TENANTS: a ConfigMap key, so
    /// adding a tenant is a config change and rebuilds nothing. Parsed once per
    /// instance; the value is read at container start like every other key.
    ///
    /// Every failure mode yields "no tenant" rather than a guess: a missing key,
    /// invalid JSON, a non-object, a malformed guid or an empty guid all resolve
    /// to null so registration simply creates no company membership.
    /// </summary>
    public class TenantHostResolver : ITenantHostResolver
    {
        private readonly Dictionary<string, Guid> _map;

        public TenantHostResolver(IConfiguration configuration)
        {
            _map = Parse(configuration["TENANT_HOSTS"] ?? configuration["TenantHosts"]);
        }

        internal static Dictionary<string, Guid> Parse(string? raw)
        {
            var map = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(raw)) return map;

            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.ValueKind != JsonValueKind.Object) return map;

                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind != JsonValueKind.String) continue;
                    if (!Guid.TryParse(prop.Value.GetString(), out var companyId)) continue;
                    if (companyId == Guid.Empty) continue;

                    var host = Normalize(prop.Name);
                    if (host.Length == 0) continue;
                    map[host] = companyId;
                }
            }
            catch (JsonException)
            {
                // Malformed config must not take registration down; no tenant is
                // the safe reading. Returns whatever parsed before the failure —
                // in practice an empty map, since JsonDocument.Parse is all-or-nothing.
                return new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            }

            return map;
        }

        /// <summary>Lowercased, port removed, IPv6 brackets removed, trailing root dot removed.</summary>
        internal static string Normalize(string? host)
        {
            if (string.IsNullOrWhiteSpace(host)) return string.Empty;
            var h = host.Trim().ToLowerInvariant();

            if (h.StartsWith('['))
            {
                var close = h.IndexOf(']');
                if (close != -1) return h[1..close];
            }

            // A forwarded host header can carry a chain; take the first entry.
            var comma = h.IndexOf(',');
            if (comma != -1) h = h[..comma].Trim();

            var colon = h.LastIndexOf(':');
            if (colon != -1) h = h[..colon];

            if (h.EndsWith('.')) h = h[..^1];
            return h;
        }

        public Guid? ResolveCompanyId(string? host)
        {
            var key = Normalize(host);
            if (key.Length == 0) return null;
            return _map.TryGetValue(key, out var id) ? id : null;
        }
    }
}
