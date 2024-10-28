namespace DsbNorge.A3Forms.Tests.resources;

public class MockHttpMessageHandler : HttpMessageHandler, IDisposable
{
    private HttpResponseMessage _httpResponseMessage;

    public void SetHttpResponse(HttpResponseMessage responseMessage)
    {
        _httpResponseMessage = responseMessage;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_httpResponseMessage);
    }

    public new void Dispose()
    {
        _httpResponseMessage?.Dispose();
    }
}