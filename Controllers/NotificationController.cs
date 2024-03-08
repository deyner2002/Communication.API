using APICommunication.DTOs;
using APIEmisorKafka.Enum;
using APIEmisorKafka.Models;
using AutoMapper;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Text;
using System.IO;
using OfficeOpenXml;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Diagnostics.Contracts;

namespace APIEmisorKafka.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {

        private readonly IProducer<string, string> _kafkaProducer;
        private readonly MongoDBSettings _mongoDBSettings;
        public readonly ConnectionStrings _ConnectionStrings;
        private readonly IMapper _mapper;

        public NotificationController(IProducer<string, string> kafkaProducer, IOptions<MongoDBSettings> mongoDBSettings, IOptions<ConnectionStrings> ConnectionStrings, IMapper mapper)
        {
            _kafkaProducer = kafkaProducer;
            _mongoDBSettings = mongoDBSettings.Value;
            _ConnectionStrings = ConnectionStrings.Value;
            _mapper = mapper;
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
        public IActionResult SaveNotification([FromBody] NotificationRequest notificationRequest)
        {
            if (notificationRequest.ContactInfo.Type == APICommunication.Enum.TypeContactInfo.Excel)
                notificationRequest.ContactInfo.Contacts = GetContactsByExcel(notificationRequest.ContactInfo.ContactExcelBase64);

            var notification = _mapper.Map<NotificationRequest, Notification>(notificationRequest);

            foreach (var idTemplate in notificationRequest.TemplatesIds)
            {
                notification.Templates.Add(GetTemplate(int.Parse(idTemplate.ToString())));
            }

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

        private List<Contact> GetContactsByExcel(string base64)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            List<Contact> contacts = new List<Contact>();

            byte[] bytes = Convert.FromBase64String(base64);

            using (var memoryStream = new MemoryStream(bytes))
            {
                using (var excelPackage = new ExcelPackage(memoryStream))
                {
                    ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets[0];

                    int rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        string email = worksheet.Cells[row, 1].Value?.ToString();
                        string phone = worksheet.Cells[row, 2].Value?.ToString();

                        contacts.Add(new Contact { Mail = email, Phone = phone });
                    }
                }
            }
            return contacts;
        }

        [HttpPost]
        [Route("SaveTemplate")]
        public IActionResult SaveTemplate(IFormFile archivo, string Name, string Sender, int Channel, string Subject, string? attachments)
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

                    string query = string.Format("INSERT INTO [dbo].[Template] (Id, Name, Body, Sender, Channel, Subject, CreationDate, ModificationDate, CreationUser, Status, Attachments) " +
                        "VALUES (NEXT VALUE FOR SequenceTemplate, '{0}', '{1}', '{2}', {3}, '{4}', GETDATE(), GETDATE(), 'APICOMM', 'A', '{5}')",
                        Name, Body, Sender, Channel, Subject, attachments);

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
        [Route("UpdateTemplate")]
        public IActionResult UpdateTemplate(IFormFile archivo, int Id, string Sender, string Subject, string? attachments)
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
                        Body = streamReader.ReadToEndAsync().Result;
                    }
                }

                using (SqlConnection connection = new SqlConnection(_ConnectionStrings.WebConnection))
                {
                    connection.Open();

                    Body = Body.Replace("'", "*-*");

                    string query = string.Format("UPDATE Template SET BODY = '{1}', Attachments = '{2}', Sender = '{3}', Subject = '{4}' WHERE Id = {0}", Id,Body,attachments,Sender, Subject);

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Registro actualizado correctamente.");
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
        [Route("DeleteTemplate")]
        public IActionResult DeleteTemplate(int Id)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnectionStrings.WebConnection))
                {
                    connection.Open();

                    string query = string.Format("UPDATE Template SET Status = 'I' WHERE Id = {0}", Id);

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Registro eliminado correctamente.");
                    }
                }
                return Ok("The notification was removed");
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
                                NumberId = (int)reader["Id"],
                                Name = reader["Name"] is DBNull ? null : (string)reader["Name"],
                                Channel = reader["Channel"] is DBNull ? null : (Channel)reader["Channel"],
                                Sender = reader["Sender"] is DBNull ? null : (string)reader["Sender"],
                                Body = reader["Body"] is DBNull ? null : (string)reader["Body"],
                                Subject = reader["Subject"] is DBNull ? null : (string)reader["Subject"],
                                IsHtml = reader["IsHTML"] is DBNull ? false : (int)reader["IsHTML"] == 1 ? true : false,
                                Attachments = reader["Attachments"] is DBNull ? null : (string)reader["Attachments"],
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

        [HttpPost]
        [Route("GetTemplates")]
        public List<Template> GetTemplates()
        {
            List<Template> templates = new List<Template>();
            using (SqlConnection connection = new SqlConnection(_ConnectionStrings.WebConnection))
            {
                connection.Open();

                string query = "SELECT * FROM Template WHERE Status = 'A'";
                using (SqlCommand command = new SqlCommand(query, connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Template template = new Template
                            {
                                NumberId = (int)reader["Id"],
                                Name = reader["Name"] is DBNull ? null : (string)reader["Name"],
                                Channel = reader["Channel"] is DBNull ? null : (Channel)reader["Channel"],
                                Sender = reader["Sender"] is DBNull ? null : (string)reader["Sender"],
                                Subject = reader["Subject"] is DBNull ? null : (string)reader["Subject"],
                                IsHtml = reader["IsHTML"] is DBNull ? false : (int)reader["IsHTML"] == 1 ? true : false,
                            };
                            templates.Add(template);
                        }
                    }
            }
            return templates;
        }

    }
}
