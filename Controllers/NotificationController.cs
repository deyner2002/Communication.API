using APIEmisorKafka.Models;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace APIEmisorKafka.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NotificationController : ControllerBase
    {

        private readonly IProducer<string, string> _kafkaProducer;
        private readonly MongoDBSettings _mongoDBSettings;

        public NotificationController(IProducer<string, string> kafkaProducer, IOptions<MongoDBSettings> mongoDBSettings)
        {
            _kafkaProducer = kafkaProducer;
            _mongoDBSettings = mongoDBSettings.Value;
        }

        [HttpPost]
        [Route("EnviarMensaje")]
        public IActionResult EnviarMensaje([FromBody] string mensaje)
        {
            var message = new Message<string, string>
            {
                Key = null,
                Value = mensaje
            };

            _kafkaProducer.ProduceAsync("test-kafka", message);

            return Ok("Mensaje enviado a Kafka");
        }

        [HttpPost]
        [Route("SaveNotification")]
        public IActionResult SaveNotification([FromBody] Notification notification)
        {
            if (!notification.IsRecurring)
            {
                var message = new Message<string, string>
                {
                    Key = notification.Id,
                    Value = JsonConvert.SerializeObject(notification)
                };
                _kafkaProducer.ProduceAsync("test-kafka", message);
            }
            else
            {
                MongoClient client = new MongoClient(_mongoDBSettings.ConnectionString);

                IMongoDatabase database = client.GetDatabase(_mongoDBSettings.DatabaseName);
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>(_mongoDBSettings.CollectionName);

                BsonDocument document = BsonDocument.Parse(JsonConvert.SerializeObject(notification));
                collection.InsertOne(document);
            }
            return Ok("The notification was saved");
        }
    }
}
