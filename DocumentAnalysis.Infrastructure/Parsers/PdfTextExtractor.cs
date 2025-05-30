using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace DocumentAnalysis.Infrastructure.Parsers;

public class PdfTextExtractor
{
    public string ExtractAllText(byte[] fileBytes)
    {
        using var stream = new MemoryStream(fileBytes);
        using var document = PdfDocument.Open(stream);

        var sb = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            sb.AppendLine(page.Text);
        }

        return sb.ToString();
    }
    public static List<string> SplitText(string text, int chunkSize = 1000)
    {
        var chunks = new List<string>();

        for (int i = 0; i < text.Length; i += chunkSize)
        {
            int length = Math.Min(chunkSize, text.Length - i);
            chunks.Add(text.Substring(i, length));
        }

        return chunks;
    }
}




