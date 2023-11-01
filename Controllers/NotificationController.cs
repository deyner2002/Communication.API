using APIEmisorKafka.Models;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace APIEmisorKafka.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NotificationController : ControllerBase
    {

        private readonly IProducer<string, string> _kafkaProducer;

        public NotificationController(IProducer<string, string> kafkaProducer)
        {
            _kafkaProducer = kafkaProducer;
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
                // Insertar en Mongo
            }
            
            return Ok("The notification was saved");
        }
    }
}
