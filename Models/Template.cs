﻿using APIEmisorKafka.Enum;

namespace APIEmisorKafka.Models
{
    public class Template
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public Channel? Channel { get; set; }
        public string? Sender { get; set; }
        public string? Body { get; set; }
        public bool IsHtml { get; set; }
        public string? Subject { get; set; }
    }
}
