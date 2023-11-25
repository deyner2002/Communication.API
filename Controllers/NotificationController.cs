using APICommunication.DTOs;
using APIEmisorKafka.Enum;
using APIEmisorKafka.Models;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace APIEmisorKafka.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NotificationController : ControllerBase
    {

        private readonly IProducer<string, string> _kafkaProducer;
        private readonly MongoDBSettings _mongoDBSettings;
        public readonly ConnectionStrings _ConnectionStrings;

        public NotificationController(IProducer<string, string> kafkaProducer, IOptions<MongoDBSettings> mongoDBSettings, IOptions<ConnectionStrings> ConnectionStrings)
        {
            _kafkaProducer = kafkaProducer;
            _mongoDBSettings = mongoDBSettings.Value;
            _ConnectionStrings = ConnectionStrings.Value;
        }

        [HttpPost]
        [Route("SentMessage")]
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
            if (!notification.IsProgrammed)
            {
                var message = new Message<string, string>
                {
                    Key = null,
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

        [HttpPost]
        [Route("SaveTemplate")]
        public IActionResult SaveTemplate(IFormFile archivo, int Id, string Name, string Sender, int Channel,string Subject)
        {
            string Body = string.Empty;
            if (archivo == null || archivo.Length == 0)
            {
                return BadRequest("El archivo no es válido.");
            }

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    archivo.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    using (var streamReader = new StreamReader(memoryStream, Encoding.UTF8))
                    {
                        Body =  streamReader.ReadToEndAsync().Result;
                    }
                }

                using (SqlConnection connection = new SqlConnection(_ConnectionStrings.WebConnection))
                {
                    connection.Open();

                    Body = Body.Replace("'", "*-*");

                    string query = string.Format("INSERT INTO [dbo].[Template] (Id, Name, Body, Sender, Channel, Subject, CreationDate, ModificationDate, CreationUser, Status) " +
                        "VALUES ({0}, '{1}', '{2}', '{3}', {4}, '{5}', GETDATE(), GETDATE(), 'APICOMM', 'A' )",
                        Id, Name, Body, Sender, Channel, Subject);

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Registro agregado correctamente.");
                    }
                }

                return Ok("The notification was saved");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al procesar el archivo: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("GetTemplate")]
        public Template GetTemplate(int Id)
        {
            using (SqlConnection connection = new SqlConnection(_ConnectionStrings.WebConnection))
            {
                connection.Open();

                string query = "SELECT * FROM Template WHERE Id = @Id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", Id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Template template = new Template
                            {
                                Id = (int)reader["Id"],
                                Name = reader["Name"] is DBNull ? null : (string)reader["Name"],
                                Channel = reader["Channel"] is DBNull ? null : (Channel)reader["Channel"],
                                Sender = reader["Sender"] is DBNull ? null : (string)reader["Sender"],
                                Body = reader["Body"] is DBNull ? null : (string)reader["Body"],
                                Subject = reader["Subject"] is DBNull ? null : (string)reader["Subject"]
                            };

                            template.Body = template.Body.Replace("*-*", "'");
                            return template;
                        }
                        else
                        {
                            return null; // No se encontró un registro con el ID proporcionado
                        }
                    }
                }
            }
        }
    }
}
