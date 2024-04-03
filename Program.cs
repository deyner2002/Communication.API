using APICommunication.DTOs;
using Confluent.Kafka;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

IConfiguration config = new ConfigurationBuilder()
.AddAzureAppConfiguration(options =>
{
    options.Connect(builder.Configuration.GetValue<string>("AppConfigsAzure"))
           .ConfigureKeyVault(kv =>
           {
               kv.SetCredential(new DefaultAzureCredential());
           });
})
.Build();

builder.Services.Configure<ConnectionStrings>(config.GetSection("communication:sqlconnection"));

builder.Services.Configure<MongoDBSettings>(config.GetSection("mongo:configuration"));

var kafkaConfig = new ProducerConfig();
config.GetSection("kafka:configuration").Bind(kafkaConfig);

builder.Services.AddSingleton<IProducer<string, string>>(new ProducerBuilder<string, string>(kafkaConfig).Build());

builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddSwaggerGen();

var _MyCors = "MyCors";
var HostFront = config.GetValue<string>("HostFront");

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