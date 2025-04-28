using System.Net;

namespace DsbNorge.A3Forms.Tests.resources;

public class MockHttpMessageHandler : HttpMessageHandler, IDisposable
{
    private HttpResponseMessage? _httpResponseMessage;
    private Action<HttpRequestMessage>? _requestCaptureCallback;


    public void SetHttpResponse(HttpResponseMessage responseMessage)
    {
        _httpResponseMessage = responseMessage;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requestCaptureCallback?.Invoke(request);
        
        var responseToSend = _httpResponseMessage ?? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") }; 
                
        return Task.FromResult(responseToSend);
    }

    public new void Dispose()
    {
        _httpResponseMessage?.Dispose();
        GC.SuppressFinalize(this); // as per https://learn.microsoft.com/en-gb/dotnet/fundamentals/code-analysis/quality-rules/ca1816
    }
    
    public void CaptureRequest(Action<HttpRequestMessage> callback)
    {
        _requestCaptureCallback = callback;
    }
}