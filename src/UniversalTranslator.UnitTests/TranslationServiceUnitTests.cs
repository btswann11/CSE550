using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace UniversalTranslator.UnitTests;

public class TranslationServiceUnitTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly TranslationService _translationService;
    private readonly Uri _baseAddress = new Uri("https://api.cognitive.microsofttranslator.com");

    public TranslationServiceUnitTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        
        // Setup the Dispose method to prevent MockBehavior.Strict from failing
        _httpMessageHandlerMock.Protected()
            .Setup("Dispose", ItExpr.IsAny<bool>());

        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = _baseAddress
        };
        _translationService = new TranslationService(_httpClient);
    }

    //public void Dispose()
    //{
    //    _httpClient?.Dispose();
    //    _httpMessageHandlerMock?.Reset();
    //}

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TranslationService(null!));
    }

    [Fact]
    public async Task GetSupportedLanguagesAsync_WithSuccessfulResponse_ReturnsLanguageData()
    {
        // Arrange
        var expectedLanguages = """{"translation":{"en":{"name":"English","nativeName":"English"},"es":{"name":"Spanish","nativeName":"Español"}}}""";
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(expectedLanguages)
        };

        SetupHttpMessageHandler(
            HttpMethod.Get,
            new Uri(_baseAddress, "/languages?api-version=3.0"),
            httpResponse);

        // Act
        var result = await _translationService.GetSupportedLanguagesAsync();

        // Assert
        Assert.Equal(expectedLanguages, result);
        VerifyHttpMessageHandler(Times.Once());
    }

    [Fact]
    public async Task GetSupportedLanguagesAsync_WithUnsuccessfulResponse_ThrowsHttpRequestException()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            ReasonPhrase = "Internal Server Error"
        };

        SetupHttpMessageHandler(
            HttpMethod.Get,
            new Uri(_baseAddress, "/languages?api-version=3.0"),
            httpResponse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => 
            _translationService.GetSupportedLanguagesAsync());
        
        Assert.Contains("Failed to retrieve supported languages", exception.Message);
        Assert.Contains("Internal Server Error", exception.Message);
        VerifyHttpMessageHandler(Times.Once());
    }

    [Fact]
    public async Task TranslateAsync_WithValidInputs_ReturnsTranslatedText()
    {
        // Arrange
        var translationResponse = """[{"translations":[{"text":"Hola mundo","to":"es"}]}]""";
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(translationResponse)
        };

        SetupHttpMessageHandler(
            HttpMethod.Post,
            new Uri(_baseAddress, "/translate?api-version=3.0&from=en&to=es"),
            httpResponse);

        // Act
        var result = await _translationService.TranslateAsync("Hello world", "en", "es");

        // Assert
        Assert.Equal(translationResponse, result);
        VerifyHttpMessageHandler(Times.Once());
    }

    [Fact]
    public async Task TranslateAsync_WithUnsuccessfulResponse_ThrowsHttpRequestException()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);

        SetupHttpMessageHandler(
            HttpMethod.Post,
            new Uri(_baseAddress, "/translate?api-version=3.0&from=en&to=es"),
            httpResponse);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => 
            _translationService.TranslateAsync("Hello world", "en", "es"));
        
        VerifyHttpMessageHandler(Times.Once());
    }

    [Fact]
    public async Task TranslateAsync_WithNullText_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _translationService.TranslateAsync(null!, "en", "es"));
        
        // Verify no HTTP calls were made
        VerifyHttpMessageHandler(Times.Never());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public async Task TranslateAsync_WithEmptyOrWhitespaceText_ThrowsArgumentException(string text)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _translationService.TranslateAsync(text, "en", "es"));
        
        // Verify no HTTP calls were made
        VerifyHttpMessageHandler(Times.Never());
    }

    [Fact]
    public async Task TranslateAsync_WithNullFromLanguage_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _translationService.TranslateAsync("Hello world", null!, "es"));
        
        // Verify no HTTP calls were made
        VerifyHttpMessageHandler(Times.Never());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public async Task TranslateAsync_WithEmptyOrWhitespaceFromLanguage_ThrowsArgumentException(string fromLanguage)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _translationService.TranslateAsync("Hello world", fromLanguage, "es"));
        
        // Verify no HTTP calls were made
        VerifyHttpMessageHandler(Times.Never());
    }

    [Fact]
    public async Task TranslateAsync_WithNullToLanguage_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _translationService.TranslateAsync("Hello world", "en", null!));
        
        // Verify no HTTP calls were made
        VerifyHttpMessageHandler(Times.Never());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public async Task TranslateAsync_WithEmptyOrWhitespaceToLanguage_ThrowsArgumentException(string toLanguage)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _translationService.TranslateAsync("Hello world", "en", toLanguage));
        
        // Verify no HTTP calls were made
        VerifyHttpMessageHandler(Times.Never());
    }

    [Fact]
    public async Task TranslateAsync_SendsCorrectHttpRequest()
    {
        // Arrange
        var text = "Hello world";
        var fromLanguage = "en";
        var toLanguage = "es";
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""[{"translations":[{"text":"Hola mundo","to":"es"}]}]""")
        };

        HttpRequestMessage? capturedRequest = null;
        SetupHttpMessageHandlerWithCallback(
            httpResponse,
            (req, token) => capturedRequest = req);

        // Act
        await _translationService.TranslateAsync(text, fromLanguage, toLanguage);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal(new Uri(_baseAddress, "/translate?api-version=3.0&from=en&to=es"), capturedRequest.RequestUri);
        
        // Verify request body contains the text
        var requestContent = await capturedRequest.Content!.ReadAsStringAsync();
        Assert.Contains(text, requestContent);
        VerifyHttpMessageHandler(Times.Once());
    }

    [Fact]
    public async Task TranslateAsync_SendsCorrectRequestBody()
    {
        // Arrange
        var text = "Hello world";
        var fromLanguage = "en";
        var toLanguage = "es";
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""[{"translations":[{"text":"Hola mundo","to":"es"}]}]""")
        };

        string? capturedRequestBody = null;
        SetupHttpMessageHandlerWithCallback(
            httpResponse,
            async (req, token) => capturedRequestBody = await req.Content!.ReadAsStringAsync());

        // Act
        await _translationService.TranslateAsync(text, fromLanguage, toLanguage);

        // Assert
        Assert.NotNull(capturedRequestBody);
        
        // Deserialize and verify the request body structure
        var requestBodyJson = JsonDocument.Parse(capturedRequestBody);
        var requestArray = requestBodyJson.RootElement;
        var firstElement = requestArray[0];
        var hasElement = firstElement.TryGetProperty("text", out var textProperty);
        Assert.True(hasElement);
        Assert.True(requestArray.ValueKind == JsonValueKind.Array);
        Assert.Equal(1, requestArray.GetArrayLength());
        
        
        
        Assert.Equal(text, textProperty.GetString());
        VerifyHttpMessageHandler(Times.Once());
    }

    [Fact]
    public async Task TranslateAsync_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var text = "Hello! How are you? I'm fine. ??";
        var fromLanguage = "en";
        var toLanguage = "es";
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""[{"translations":[{"text":"¡Hola! ¿Cómo estás? Estoy bien. ??","to":"es"}]}]""")
        };

        SetupHttpMessageHandler(
            HttpMethod.Post,
            new Uri(_baseAddress, "/translate?api-version=3.0&from=en&to=es"),
            httpResponse);

        // Act
        var result = await _translationService.TranslateAsync(text, fromLanguage, toLanguage);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("¡Hola!", result);
        Assert.Contains("??", result);
        VerifyHttpMessageHandler(Times.Once());
    }

    [Fact]
    public async Task TranslateAsync_WithLongText_HandlesCorrectly()
    {
        // Arrange
        var text = new string('A', 1000); // 1000 character string
        var fromLanguage = "en";
        var toLanguage = "es";
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""[{"translations":[{"text":"Response","to":"es"}]}]""")
        };

        SetupHttpMessageHandler(
            HttpMethod.Post,
            new Uri(_baseAddress, "/translate?api-version=3.0&from=en&to=es"),
            httpResponse);

        // Act
        var result = await _translationService.TranslateAsync(text, fromLanguage, toLanguage);

        // Assert
        Assert.NotNull(result);
        VerifyHttpMessageHandler(Times.Once());
    }

    [Fact]
    public async Task TranslateAsync_WithDifferentLanguageCodes_BuildsCorrectUrl()
    {
        // Arrange
        var testCases = new[]
        {
            ("en", "es"),
            ("fr", "de"),
            ("zh-Hans", "ja"),
            ("pt-BR", "it")
        };

        foreach (var (from, to) in testCases)
        {
            // Create a new mock and HttpClient for each iteration to avoid setup conflicts
            var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            mockHandler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
            
            using var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = _baseAddress };
            var translationService = new TranslationService(httpClient);

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""[{"translations":[{"text":"test","to":"test"}]}]""")
            };

            HttpRequestMessage? capturedRequest = null;
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, token) => capturedRequest = req)
                .ReturnsAsync(httpResponse);

            // Act
            await translationService.TranslateAsync("test", from, to);

            // Assert
            Assert.NotNull(capturedRequest);
            Assert.Equal(new Uri(_baseAddress, $"/translate?api-version=3.0&from={from}&to={to}"), capturedRequest.RequestUri);
        }
    }

    [Fact]
    public void Dispose_HttpClient_DisposesCorrectly()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
        
        var httpClient = new HttpClient(mockHandler.Object);
        var translationService = new TranslationService(httpClient);

        // Act & Assert - Should not throw
        httpClient.Dispose();
    }

    [Fact]
    public async Task GetSupportedLanguagesAsync_SendsCorrectHttpRequest()
    {
        // Arrange
        var expectedLanguages = """{"translation":{"en":{"name":"English","nativeName":"English"},"es":{"name":"Spanish","nativeName":"Español"}}}""";
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(expectedLanguages)
        };

        HttpRequestMessage? capturedRequest = null;
        SetupHttpMessageHandlerWithCallback(
            httpResponse,
            (req, token) => capturedRequest = req);

        // Act
        await _translationService.GetSupportedLanguagesAsync();

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest.Method);
        Assert.Equal(new Uri(_baseAddress, "/languages?api-version=3.0"), capturedRequest.RequestUri);
        VerifyHttpMessageHandler(Times.Once());
    }

    [Fact]
    public async Task TranslateAsync_WithTimeout_ThrowsTaskCanceledException()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request was canceled"));

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            _translationService.TranslateAsync("Hello world", "en", "es"));
        
        VerifyHttpMessageHandler(Times.Once());
    }

    [Fact]
    public async Task GetSupportedLanguagesAsync_WithTimeout_ThrowsTaskCanceledException()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request was canceled"));

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            _translationService.GetSupportedLanguagesAsync());
        
        VerifyHttpMessageHandler(Times.Once());
    }

    [Fact]
    public async Task HttpClient_WithStrictMocking_PreventsUnexpectedCalls()
    {
        // Arrange - Don't setup any SendAsync calls
        
        // Act & Assert - Should throw because no setup exists for SendAsync
        await Assert.ThrowsAsync<MockException>(() =>
            _translationService.TranslateAsync("Hello world", "en", "es"));
    }

    private void SetupHttpMessageHandler(HttpMethod method, Uri uri, HttpResponseMessage response)
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == method && 
                    req.RequestUri == uri),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void SetupHttpMessageHandlerWithCallback(HttpResponseMessage response, Action<HttpRequestMessage, CancellationToken> callback)
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(callback)
            .ReturnsAsync(response);
    }

    private void VerifyHttpMessageHandler(Times times)
    {
        _httpMessageHandlerMock.Protected()
            .Verify<Task<HttpResponseMessage>>(
                "SendAsync",
                times,
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }
}