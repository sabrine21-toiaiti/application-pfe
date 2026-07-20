using System.Text.Json.Serialization;

namespace SebnWeb.Services;

public class AnomalieDetecteeDto
{
    [JsonPropertyName("type_anomalie")] public string TypeAnomalie { get; set; } = "";
    [JsonPropertyName("classe")] public string Classe { get; set; } = "";
    [JsonPropertyName("confiance")] public double Confiance { get; set; }
}

public class ResultatDetectionDto
{
    [JsonPropertyName("image_base64")] public string ImageBase64 { get; set; } = "";
    [JsonPropertyName("anomalie")] public AnomalieDetecteeDto? Anomalie { get; set; }
}

/// <summary>
/// Client HTTP vers le microservice IA Python (couche Traitement & Logique).
/// </summary>
public class DetectionApiClient
{
    private readonly HttpClient _http;

    public DetectionApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<bool> EstDisponibleAsync()
    {
        try
        {
            var rep = await _http.GetAsync("/health");
            return rep.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ResultatDetectionDto?> DetecterAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<ResultatDetectionDto>("/detect");
        }
        catch
        {
            return null;
        }
    }
}
