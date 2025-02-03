namespace DsbNorge.A3Forms.Tests.resources;

public class MockHttpMessageHandler : HttpMessageHandler, IDisposable
{
    private HttpResponseMessage? _httpResponseMessage;

    public void SetHttpResponse(HttpResponseMessage responseMessage)
    {
        _httpResponseMessage = responseMessage;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_httpResponseMessage!);
    }

    public new void Dispose()
    {
        _httpResponseMessage?.Dispose();
        GC.SuppressFinalize(this); // as per https://learn.microsoft.com/en-gb/dotnet/fundamentals/code-analysis/quality-rules/ca1816
    }
}