using System.Net;
using DsbA3Forms.Clients;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace DsbA3Forms.Tests
{
    [TestFixture]
    public class GeoNorgeClientTests : IDisposable
    {
        private HttpClient _httpClient;
        private Mock<ILogger<IGeoNorgeClient>> _loggerMock;
        private Mock<IMemoryCache> _memoryCacheMock;
        private GeoNorgeClient _geoNorgeClient;
        private TestHttpMessageHandler _testHttpMessageHandler;

        [SetUp]
        public void Setup()
        {
            _testHttpMessageHandler = new TestHttpMessageHandler();
            _httpClient = new HttpClient(_testHttpMessageHandler)
            {
                BaseAddress = new Uri("https://ws.geonorge.no/")
            };
            _loggerMock = new Mock<ILogger<IGeoNorgeClient>>();
            _memoryCacheMock = new Mock<IMemoryCache>();

            _geoNorgeClient = new GeoNorgeClient(_httpClient, _loggerMock.Object, _memoryCacheMock.Object);
        }


        [Test]
        public async Task GetAddresses_should_return_emptyList_when_search_is_empty()
        {
            var result = await _geoNorgeClient.GetAddresses("", 5);

            Assert.IsNotNull(result);
            Assert.IsEmpty(result);
        }

        [Test]
        public async Task GetAddresses_should_return_emptyList_when_call_fails()
        {
            var searchString = "Oslo";

            _testHttpMessageHandler.SetHttpResponse(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest
            });

            var result = await _geoNorgeClient.GetAddresses(searchString, 5);

            Assert.IsNotNull(result);
            Assert.IsEmpty(result);
        }


        [TearDown]
        public void TearDown()
        {
            _httpClient.Dispose();
        }

        public void Dispose()
        {
            TearDown();
        }
    }

    public class TestHttpMessageHandler : HttpMessageHandler
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
    }
}
