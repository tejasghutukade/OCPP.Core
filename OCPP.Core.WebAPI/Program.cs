using System.Security.Claims;
using Auth0.ManagementApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using OCPP.Core.Database;
using OCPP.Core.WebAPI.Auth;
using OCPP.Core.WebAPI.Helpers;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var configurationBuilder = new ConfigurationBuilder().AddJsonFile("appsettings.json",optional:false,reloadOnChange:true).Build();



var logger = new LoggerConfiguration().ReadFrom.Configuration(configurationBuilder)
    .Enrich.FromLogContext().CreateLogger();
logger.Information("Application started123");
builder.Logging.AddSerilog(logger);


// Add services to the container.
builder.Host.UseSerilog((ctx, lc) =>
{
    ctx.HostingEnvironment.ApplicationName = "AATWebAPI";
    lc.ReadFrom.Configuration(configurationBuilder);
});


// Add Authentication to the container.
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = "JWT_OR_COOKIE";
        options.DefaultChallengeScheme = "JWT_OR_COOKIE";
        options.DefaultAuthenticateScheme = "JWT_OR_COOKIE";
    })
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
    })
    .AddJwtBearer(options =>
    {
        options.Authority = configurationBuilder.GetSection("Auth0:Domain").Value;
        logger.Debug(" Auth Domain : {AuthDomain} " ,configurationBuilder.GetSection("Auth0:Domain").Value);
        options.Audience = configurationBuilder.GetSection("Auth0:ApiIdentifier").Value;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = ClaimTypes.NameIdentifier,
        };
        options.RequireHttpsMetadata = false;
    })
    .AddPolicyScheme("JWT_OR_COOKIE","JWT_OR_COOKIE",options=>{
        options.ForwardDefaultSelector = context =>
        {
            // filter by auth type
            string authorization = context.Request.Headers[HeaderNames.Authorization];
            if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
                return "Bearer";

            // otherwise always check for cookie auth
            return "Cookies";
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("read:messages", policy => policy.Requirements.Add(new HasScopeRequirement("read:messages", configurationBuilder.GetSection("Auth0:Domain").Value)));
});

// register the scope authorization handler
builder.Services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();

ManagementApiClient auth0ManagementApiClient = new ManagementApiClient(configurationBuilder.GetSection("Auth0:AccessToken").Value, configurationBuilder.GetSection("Auth0:Domain").Value);
builder.Services.AddSingleton<ManagementApiClient,ManagementApiClient>(m=> auth0ManagementApiClient);


// Add services to the container.
builder.Services.AddDbContext<OcppCoreContext>(options => options.UseMySql(configurationBuilder.GetConnectionString("MySql"), ServerVersion.Parse("8.0.28-mysql")));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(typeof(AutoMapperProfiles).Assembly);
var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();