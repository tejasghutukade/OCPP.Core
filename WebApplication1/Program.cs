using DB;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var configurationBuilder = new ConfigurationBuilder().AddJsonFile("appsettings.json",optional:false,reloadOnChange:true).Build();
builder.Services.AddDbContext<OcppContext>(options => options.UseMySql(configurationBuilder.GetConnectionString("MySql"), ServerVersion.Parse("8.0.28-mysql")));

var app = builder.Build();
//Add dbcontext to the container.

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();