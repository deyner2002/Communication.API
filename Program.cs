using APICommunication.DTOs;
using APIEmisorKafka;
using Confluent.Kafka;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);



// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();


var kafkaConfig = new ProducerConfig();
builder.Configuration.GetSection("Kafka").Bind(kafkaConfig);
builder.Services.Configure<ConnectionStrings>(builder.Configuration.GetSection("ConnectionStrings"));
builder.Services.Configure<MongoDBSettings>(builder.Configuration.GetSection("MongoDBSettings"));
builder.Services.AddSingleton<IProducer<string, string>>(new ProducerBuilder<string, string>(kafkaConfig).Build());
builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddSwaggerGen();
var _MyCors = "MyCors";
var HostFront = builder.Configuration.GetValue<string>("HostFront");

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: _MyCors, builder =>
    {
        builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(_MyCors);
app.UseAuthorization();

app.MapControllers();

app.Run();