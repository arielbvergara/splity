using System.Net;
using Amazon.Lambda.APIGatewayEvents;

namespace Splity.Shared.Common;

public static class ApiGatewayHelper
{
    /// <summary>
    /// Create a standardized API Gateway response
    /// </summary>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="body">Response body</param>
    /// <param name="headers">Response headers</param>
    /// <returns>API Gateway proxy response</returns>
    public static APIGatewayProxyResponse CreateApiGatewayProxyResponse(HttpStatusCode statusCode, string body,
        Dictionary<string, string>? headers = null)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = (int) statusCode,
            Body = body,
            Headers = headers ?? new Dictionary<string, string>(),
            IsBase64Encoded = false
        };
    }
}