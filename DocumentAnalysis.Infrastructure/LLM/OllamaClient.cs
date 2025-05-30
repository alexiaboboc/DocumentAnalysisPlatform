using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Text.Json;

namespace DocumentAnalysis.Infrastructure.LLM;

public class OllamaClient
{
    private readonly HttpClient _httpClient;

    public OllamaClient()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:11434/")
        };
    }

    public async Task<string> GenerateSummaryAsync(string inputText, string model)
    {
        var prompt = $"Rezuma urmatorul text in 5 propozitii:\n\n{inputText}";

        var requestBody = new
        {
            model,
            prompt,
            stream = false
        };

        var response = await _httpClient.PostAsJsonAsync("api/generate", requestBody);

        if (!response.IsSuccessStatusCode)
            return $"[Eroare Ollama]: {response.StatusCode}";

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);

        return json.RootElement.GetProperty("response").GetString() ?? "";
    }


    public async Task<ModelComparisonResult> GenerateComparisonAsync(string inputText, string model1 = "tinyllama", string model2 = "gemma:2b")
    {
        var summary1 = await GenerateSummaryAsync(inputText, model1);
        var summary2 = await GenerateSummaryAsync(inputText, model2);

        return new ModelComparisonResult
        {
            Model1Name = model1,
            Model1Summary = summary1,
            Model2Name = model2,
            Model2Summary = summary2
        };
    }
}


public class ModelComparisonResult
{
    public string Model1Name { get; set; } = "";
    public string Model1Summary { get; set; } = "";
    public string Model2Name { get; set; } = "";
    public string Model2Summary { get; set; } = "";
}
