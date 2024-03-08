using APICommunication.Enum;
using APIEmisorKafka.Models;

namespace APICommunication.Models
{
    public class ContactInfo
    {
        public TypeContactInfo Type { get; set; }
        public string? ContactExcelBase64 { get; set; }
        public List<Contact>? Contacts { get; set; }
    }
}
