using APIEmisorKafka.Enum;

namespace APIEmisorKafka.Models
{
    public class InfoRecurrence
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool Active { get; set; }
        public DateTime ActivationTime { get; set; }
        public Recurrence Recurrence { get; set; }
    }
}
