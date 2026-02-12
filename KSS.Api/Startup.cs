using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using AutoMapper;
using KSS.Api.ServiceExtention;
using KSS.Api.MappingProfile;
using KSS.Api.Asset;
using KSS.Helper.Model;

namespace KSS.Api
{
    public class Startup
    {
        private static bool negotiate;
        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            var authenticationType = Configuration["Authentication:Type"];

            negotiate = string.Equals(authenticationType, "Negotiate", StringComparison.OrdinalIgnoreCase);

            // Get JWT secret from configuration (appsettings.json or environment variables)
            // Environment variables take precedence: JWT_SECRET or TokenSetting__JwtSecret
            string? jwtSecret = Configuration["JWT_SECRET"] 
                ?? Configuration["TokenSetting:JwtSecret"]
                ?? Configuration["TokenSetting__JwtSecret"];

            if (negotiate)
                serviceCollection.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
            else
            {
                if (string.IsNullOrEmpty(jwtSecret))
                {
                    throw new Exception("JWT Secret is required. Provide it via TokenSetting:JwtSecret in config, or JWT_SECRET or TokenSetting__JwtSecret environment variable.");
                }
                
                var key = Encoding.ASCII.GetBytes(jwtSecret);

                serviceCollection.AddAuthentication(x =>
                {
                    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(x =>
                {
                    x.RequireHttpsMetadata = false;
                    x.SaveToken = true;
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                });
            }

            serviceCollection.AddWindowsService();
            serviceCollection.AddHostedService<ApiService>();

            serviceCollection.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                });

            // Configure the HTTP JSON options that OpenAPI schema generation reads
            serviceCollection.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                options.SerializerOptions.MaxDepth = 256;
            });

            serviceCollection.AddBaseServiceExtention(Configuration);

            serviceCollection.AddOpenApi("v1", options =>
            {
                // Exclude endpoints that accept entity types with circular navigation properties
                options.ShouldInclude = (description) =>
                {
                    foreach (var param in description.ParameterDescriptions)
                    {
                        if (ContainsEntityType(param.Type))
                            return false;
                    }
                    return true;

                    static bool ContainsEntityType(Type? type)
                    {
                        if (type == null) return false;
                        if (type.Namespace == "KSS.Entity") return true;
                        if (type.IsGenericType)
                        {
                            foreach (var arg in type.GenericTypeArguments)
                            {
                                if (ContainsEntityType(arg)) return true;
                            }
                        }
                        return false;
                    }
                };

                options.AddDocumentTransformer((document, context, ct) =>
                {
                    document.Info = new OpenApiInfo
                    {
                        Title = "KSS.Service.Auth",
                        Version = "v1"
                    };
                    return Task.CompletedTask;
                });

                if (!negotiate)
                {
                    options.AddDocumentTransformer((document, context, ct) =>
                    {
                        document.Components ??= new OpenApiComponents();
                        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                        {
                            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                            Name = "Authorization",
                            In = ParameterLocation.Header,
                            Type = SecuritySchemeType.Http,
                            BearerFormat = "JWT",
                            Scheme = "Bearer"
                        };
                        return Task.CompletedTask;
                    });

                    options.AddOperationTransformer((operation, context, ct) =>
                    {
                        operation.Security ??= new List<OpenApiSecurityRequirement>();
                        operation.Security.Add(new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecuritySchemeReference("Bearer")] = new List<string>()
                        });
                        return Task.CompletedTask;
                    });
                }
            });

            serviceCollection.AddAutoMapper(mc => mc.AddProfile(new BaseMappingProfile()));

            serviceCollection.Configure<KestrelServerOptions>(configureOptions =>
            {
                // Get certificate settings from configuration (appsettings.json or environment variables)
                string? pfxFilePath = Configuration["CERTIFICATE_PFX_FILE_PATH"]
                    ?? Configuration["Certificate:pfxFilePath"]
                    ?? Configuration["Certificate__pfxFilePath"];
                string? pfxPassword = Configuration["CERTIFICATE_PFX_PASSWORD"]
                    ?? Configuration["Certificate:pfxPassword"]
                    ?? Configuration["Certificate__pfxPassword"];
                
                // Get port from configuration, default to 8080
                var portValue = Configuration["PORT"]
                    ?? Configuration["Port"]
                    ?? "8080";
                
                if (int.TryParse(portValue, out int port))
                {
                    if (!string.IsNullOrEmpty(pfxFilePath) && !string.IsNullOrEmpty(pfxPassword) && File.Exists(pfxFilePath))
                    {
                        configureOptions.Listen(IPAddress.Any, port, configure => { configure.UseHttps(pfxFilePath, pfxPassword); });
                    }
                    else
                    {
                        // Listen on HTTP only if no certificate is provided
                        configureOptions.Listen(IPAddress.Any, port);
                    }
                }
            });

            // Configure strongly-typed settings from configuration
            serviceCollection.Configure<FileSetting>(Configuration.GetSection("FileSetting"));
            serviceCollection.Configure<TokenSetting>(Configuration.GetSection("TokenSetting"));
        }

        public void Configure(IApplicationBuilder applicationBuilder, IWebHostEnvironment webHostEnvironment)
        {
            if (webHostEnvironment.IsDevelopment())
            {
                applicationBuilder.UseDeveloperExceptionPage();

            }

            // CORS must be configured before other middleware
            applicationBuilder.UseCors(configurePolicy =>
            {
                // Get allowed origins from configuration (appsettings.json or environment variables)
                var allowedOrigins = Configuration.GetSection("AllowedOrigins").Get<string[]>();
                
                // Also check ALLOWED_ORIGINS environment variable (comma-separated)
                if (allowedOrigins == null || allowedOrigins.Length == 0)
                {
                    var envAllowedOrigins = Configuration["ALLOWED_ORIGINS"];
                    if (!string.IsNullOrEmpty(envAllowedOrigins))
                    {
                        allowedOrigins = envAllowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(o => o.Trim())
                            .ToArray();
                    }
                }

                if (allowedOrigins != null && allowedOrigins.Length > 0)
                {
                    configurePolicy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
                else
                {
                    // Allow all origins in development if not configured
                    if (webHostEnvironment.IsDevelopment())
                    {
                        configurePolicy.AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    }
                }
            });

            // Only use HTTPS redirection in production
            if (!webHostEnvironment.IsDevelopment())
            {
                applicationBuilder.UseHttpsRedirection();
            }

            applicationBuilder.UseRouting();

            applicationBuilder.UseAuthentication();
            applicationBuilder.UseAuthorization();

            if (negotiate)
                applicationBuilder.UseMiddleware<NegMiddleware>();
            else
                applicationBuilder.UseMiddleware<JwtMiddleware>();

            applicationBuilder.UseEndpoints(configure =>
            {
                configure.MapOpenApi().AllowAnonymous();
                configure.MapScalarApiReference(options =>
                {
                    options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
                }).AllowAnonymous();
                configure.MapControllerRoute(name: "default", pattern: "Api/{controller}/{action}/{id?}");
                configure.MapControllers();
            });
        }
    }
}
