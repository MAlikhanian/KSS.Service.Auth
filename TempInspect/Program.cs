using System.Reflection;

var asm = typeof(Microsoft.OpenApi.OpenApiDocument).Assembly;

// Check OpenApiSecuritySchemeReference constructors
var secRef = asm.GetType("Microsoft.OpenApi.OpenApiSecuritySchemeReference");
Console.WriteLine("OpenApiSecuritySchemeReference constructors:");
foreach (var c in secRef?.GetConstructors() ?? [])
{
    var parms = string.Join(", ", c.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
    Console.WriteLine($"  ({parms})");
}

Console.WriteLine();

// Check OpenApiOperation.Security type
var op = asm.GetType("Microsoft.OpenApi.OpenApiOperation");
var sec = op?.GetProperty("Security");
Console.WriteLine($"OpenApiOperation.Security type: {sec?.PropertyType}");

Console.WriteLine();

// Check if OpenApiComponents can be created
var comp = new Microsoft.OpenApi.OpenApiComponents();
comp.SecuritySchemes["Bearer"] = new Microsoft.OpenApi.OpenApiSecurityScheme
{
    Description = "test",
    Name = "Authorization",
    In = Microsoft.OpenApi.ParameterLocation.Header,
    Type = Microsoft.OpenApi.SecuritySchemeType.Http,
    BearerFormat = "JWT",
    Scheme = "Bearer"
};
Console.WriteLine("Security scheme added to components successfully");

// Check SecurityRequirement with reference
var doc = new Microsoft.OpenApi.OpenApiDocument();
var schemeRef = new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", doc);
var req = new Microsoft.OpenApi.OpenApiSecurityRequirement
{
    [schemeRef] = new List<string>()
};
Console.WriteLine("Security requirement created successfully");
