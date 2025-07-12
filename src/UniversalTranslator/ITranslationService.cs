
using System.Diagnostics.CodeAnalysis;

namespace UniversalTranslator;

public interface ITranslationService
{
    Task<string> GetSupportedLanguagesAsync();
    Task<string> TranslateAsync(string text, string fromLanguage, string toLanguage);
}