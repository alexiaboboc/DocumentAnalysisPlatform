using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentAnalysis.Domain.Entities
{
    public class Document
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty; 
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string? ExtractedText { get; set; }
    }
}
