using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace UniversalTranslator;
public class TranslationService : ITranslationService
{
    private record TranslationRequest(string Text);
    private readonly HttpClient _httpClient;

    public TranslationService(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));

        _httpClient = httpClient;
    }

    public async Task<string> GetSupportedLanguagesAsync()
    {
        var response = await _httpClient.GetAsync("/languages?api-version=3.0");

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to retrieve supported languages: {response.ReasonPhrase}");
        }
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> TranslateAsync(string text, string fromLanguage, string toLanguage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text, nameof(text));
        ArgumentException.ThrowIfNullOrWhiteSpace(fromLanguage, nameof(fromLanguage));
        ArgumentException.ThrowIfNullOrWhiteSpace(toLanguage, nameof(toLanguage));

        var body = new[]
        {
            new TranslationRequest(text)
        };

        var response = await _httpClient.PostAsJsonAsync($"/translate?api-version=3.0&from={fromLanguage}&to={toLanguage}", body);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}
