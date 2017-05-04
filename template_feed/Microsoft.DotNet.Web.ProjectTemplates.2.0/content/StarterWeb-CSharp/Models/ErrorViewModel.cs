using System;

namespace Company.WebApplication1.Models
{
    public class ErrorViewModel
    {
        public string CorrelationId { get; set; }

        public bool ShowCorrelationId => !string.IsNullOrEmpty(CorrelationId);
    }
}