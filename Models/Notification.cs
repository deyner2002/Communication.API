using APIEmisorKafka.Enum;

namespace APIEmisorKafka.Models
{
    public class Notification
    {
        public string Id { get; set; }
        public bool IsRecurring { get; set; }
        public InfoRecurrence InfoRecurrence { get; set; }
        public List<Channel> Channels { get; set; }
        public List<Contact> Contacts { get; set; }
        public List<Template> Templates { get; set; }
    }
}
