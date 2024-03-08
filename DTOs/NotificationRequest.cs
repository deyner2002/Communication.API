using APICommunication.Models;
using APIEmisorKafka.Enum;
using APIEmisorKafka.Models;

namespace APICommunication.DTOs
{
    public class NotificationRequest
    {
        public bool IsProgrammed { get; set; }
        public ProgrammingInfo ProgrammingInfo { get; set; }
        public ContactInfo ContactInfo { get; set; }
        public List<long> TemplatesIds { get; set; }
        public bool GetObject { get; set; }
        public string? GetObjectUrl { get; set; }
        public string? ObjectTemplate { get; set; }

    }
}
