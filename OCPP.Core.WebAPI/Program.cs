using Microsoft.EntityFrameworkCore;
using OCPP.Core.Database;
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

app.UseAuthorization();

app.MapControllers();

app.Run();