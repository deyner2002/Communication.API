using APIEmisorKafka.Enum;

namespace APIEmisorKafka.Models
{
    public class Notification
    {
        public bool IsProgrammed { get; set; }
        public ProgrammingInfo ProgrammingInfo { get; set; }
        public List<Channel> Channels { get; set; }
        public List<Contact> Contacts { get; set; }
        public List<Template> Templates { get; set; }
        public string Object { get; set; }
    }
}
