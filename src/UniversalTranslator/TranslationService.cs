using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace UniversalTranslator;
public class TranslationService
{
    private record TranslationRequest(string Text);
    private readonly HttpClient _httpClient;

    public TranslationService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
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
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text to translate cannot be null or empty.", nameof(text));
        }
        if (string.IsNullOrWhiteSpace(fromLanguage))
        {
            throw new ArgumentException("Source language cannot be null or empty.", nameof(fromLanguage));
        }
        if (string.IsNullOrWhiteSpace(toLanguage))
        {
            throw new ArgumentException("Target language cannot be null or empty.", nameof(toLanguage));
        }

        var body = new[]
        {
            new TranslationRequest(text)
        };

        var response = await _httpClient.PostAsJsonAsync($"/translate?api-version=3.0&from={fromLanguage}&to=fr&to={toLanguage}", body);
        
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync();
    }
}
