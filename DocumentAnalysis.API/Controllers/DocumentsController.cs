using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DocumentAnalysis.Infrastructure.Parsers;
using UglyToad.PdfPig;
using DocumentAnalysis.Infrastructure.LLM;
using DocumentFormat.OpenXml.Packaging;

namespace DocumentAnalysis.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController : ControllerBase
    {
        [HttpPost("upload-comparison")]
        public async Task<IActionResult> UploadDocumentComparison(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded." });

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            string fullText = "";
            var fileName = file.FileName.ToLowerInvariant();

            try
            {
                if (fileName.EndsWith(".pdf"))
                {
                    var extractor = new PdfTextExtractor();
                    fullText = extractor.ExtractAllText(fileBytes);
                }
                else if (fileName.EndsWith(".txt"))
                {
                    using var reader = new StreamReader(new MemoryStream(fileBytes));
                    fullText = await reader.ReadToEndAsync();
                }
                else if (fileName.EndsWith(".docx"))
                {
                    using var memory = new MemoryStream(fileBytes);
                    using var wordDoc = WordprocessingDocument.Open(memory, false);
                    var body = wordDoc.MainDocumentPart?.Document?.Body;
                    fullText = body?.InnerText ?? "";
                }
                else if (file.ContentType.StartsWith("image"))
                {
                    var tessdataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
                    var langPath = Path.Combine(tessdataPath, "eng.traineddata");

                    if (!System.IO.File.Exists(langPath))
                        return StatusCode(500, new { error = "eng.traineddata is missing in tessdata folder!" });

                    using var engine = new Tesseract.TesseractEngine(tessdataPath, "eng", Tesseract.EngineMode.Default);

                    try
                    {
                        using var img = Tesseract.Pix.LoadFromMemory(fileBytes);
                        using var page = engine.Process(img);
                        fullText = page.GetText();
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, new { error = $"Eroare la extragerea textului din imagine: {ex.Message}" });
                    }
                }
                else
                {
                    return BadRequest(new { error = "Unsupported file type." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Eroare la extragerea textului: {ex.Message}" });
            }

            if (string.IsNullOrWhiteSpace(fullText))
                return BadRequest(new { error = "Text could not be extracted from the document." });

            var chunks = PdfTextExtractor.SplitText(fullText, 1000);
            var ollama = new OllamaClient();

            var stopwatch1 = new System.Diagnostics.Stopwatch();
            var stopwatch2 = new System.Diagnostics.Stopwatch();

            var partialSummariesModel1 = new List<string>();
            var partialSummariesModel2 = new List<string>();

            stopwatch1.Start();
            foreach (var chunk in chunks)
            {
                var summary = await ollama.GenerateSummaryAsync(chunk, "tinyllama");
                partialSummariesModel1.Add(summary.Trim());
            }
            stopwatch1.Stop();

            stopwatch2.Start();
            foreach (var chunk in chunks)
            {
                var summary = await ollama.GenerateSummaryAsync(chunk, "gemma:2b");
                partialSummariesModel2.Add(summary.Trim());
            }
            stopwatch2.Stop();

            var finalSummaryModel1 = string.Join("\n\n", partialSummariesModel1);
            var finalSummaryModel2 = string.Join("\n\n", partialSummariesModel2);

            int textLength = fullText.Length;

            var model1Stats = new
            {
                Length = finalSummaryModel1.Length,
                Words = finalSummaryModel1.Split(' ').Length,
                Sentences = finalSummaryModel1.Split('.').Length,
                Ratio = Math.Round((double)finalSummaryModel1.Length / textLength * 100, 1),
                TimeSeconds = Math.Round(stopwatch1.Elapsed.TotalSeconds, 2)
            };

            var model2Stats = new
            {
                Length = finalSummaryModel2.Length,
                Words = finalSummaryModel2.Split(' ').Length,
                Sentences = finalSummaryModel2.Split('.').Length,
                Ratio = Math.Round((double)finalSummaryModel2.Length / textLength * 100, 1),
                TimeSeconds = Math.Round(stopwatch2.Elapsed.TotalSeconds, 2)
            };

            return Ok(new
            {
                FileName = file.FileName,
                ExtractedText = fullText,
                Model1Name = "tinyllama",
                SummaryModel1 = finalSummaryModel1,
                Model1Stats = model1Stats,
                Model2Name = "gemma:2b",
                SummaryModel2 = finalSummaryModel2,
                Model2Stats = model2Stats
            });
        }
    }
}