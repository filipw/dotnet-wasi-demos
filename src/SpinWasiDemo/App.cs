using System.Net;
using System.Text.Json;
using Fermyon.Spin.Sdk;
using Net.Codecrete.QrCodeGenerator;

namespace SpinWasiDemo;

public static class App
{
    private static readonly Dictionary<string, Func<HttpRequest, HttpResponse>> _routes = new()
    {
        { "/hello", Hello },
        { "/qr", Qr }
    };

    [HttpHandler]
    public static HttpResponse Router(HttpRequest request)
    {
        var requestUrl = request.Url.ToLowerInvariant();
        var routeFound = _routes.TryGetValue(requestUrl, out var handler);

        if (routeFound) return handler(request);

        return new HttpResponse
        {
            StatusCode = HttpStatusCode.NotFound
        };
    }

    private static HttpResponse Hello(HttpRequest httpRequest)
    {
        return new HttpResponse
        {
            StatusCode = HttpStatusCode.OK,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "text/plain"
            },
            BodyAsString = "Hello from C# in a web WASI worker!",
        };
    }

    private static HttpResponse Qr(HttpRequest httpRequest)
    {
        var raw = httpRequest.Body.AsBytes();
        if (raw != null && raw.Length > 0) 
        {
            var input = JsonSerializer.Deserialize<QrInput>(raw);
            if (input != null && input.Url != null)
            {
                var qr = QrCode.EncodeText(input.Url.ToLowerInvariant(), QrCode.Ecc.Medium);
                string svg = qr.ToSvgString(4);

                return new HttpResponse
                {
                    StatusCode = HttpStatusCode.OK,
                    Headers = new Dictionary<string, string>
                    {
                        ["Content-Type"] = "application/octet-stream"
                    },
                    BodyAsString = svg
                };
            }
        }

        return new HttpResponse
        {
            StatusCode = HttpStatusCode.BadRequest,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "text/plain"
            },
            BodyAsString = JsonSerializer.Serialize(new { error = "Invalid request" })
        };
    }
    record QrInput
    {
        public string Url { get; set; }
    }
}