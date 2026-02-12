//using System.Net;
//using Microsoft.AspNetCore.Authentication.Negotiate;
//using KSS.Data;
//using KSS.Api.Asset;
//using KSS.Api.ServiceExtention;
//using AutoMapper;
//using Microsoft.Extensions.DependencyInjection;
//using KSS.Api.MappingProfile;

//var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();

//builder.Services.AddWindowsService();
//builder.Services.AddHostedService<ApiService>();

//builder.Services.AddControllers();

//builder.Services.AddBaseServiceExtention(builder.Configuration);
//builder.Services.Add_000_ERP_ServiceExtention(builder.Configuration);
//builder.Services.Add_017_CMS_ServiceExtention(builder.Configuration);
////builder.Services.Add_018_NMS_ServiceExtention(builder.Configuration); 

//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//var mappingConfig = new MapperConfiguration(mc =>
//{
//    mc.AddProfile(new BaseMappingProfile());
//});

//IMapper mapper = mappingConfig.CreateMapper();
//builder.Services.AddSingleton(mapper);

//builder.WebHost.ConfigureKestrel(serverOptions =>
//{
//    string pfxFilePath, pfxPassword;

//    pfxFilePath = @"C:\Application\Certificate\API_TIBOffice_IR_202307_202407.pfx"; pfxPassword = "ABcd@12345";

//    serverOptions.Listen(IPAddress.Any, 61652, listenOptions => { listenOptions.UseHttps(pfxFilePath, pfxPassword); });
//});

//var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseCors(builder =>
//{
//    builder.WithOrigins("http://172.21.251.221:8080", "http://localhost:4200").AllowAnyHeader().AllowAnyMethod().AllowCredentials();
//    //builder.WithOrigins("http://*", "http://*").AllowAnyHeader().AllowAnyMethod().AllowCredentials(); //Not Working
//});

//app.UseHttpsRedirection();

//app.UseRouting();

//app.UseAuthentication();
//app.UseAuthorization();

//app.UseMiddleware<Middleware>();

//app.MapControllers();

//using var scope = app.Services.CreateScope();
//var services = scope.ServiceProvider;
//var context = services.GetRequiredService<MainDbContext>();

//app.Run();