namespace Splity.Shared.Common;

public static class LambdaFunctionResponseHelper
{
    public static LambdaFunctionResponse CreateSuccessResponse(object data) =>
        new()
        {
            Data = data,
            Success = true
        };

    public static LambdaFunctionResponse CreateErrorResponse(string errorMessage) =>
        new()
        {
            ErrorMessage = errorMessage,
            Success = false
        };
}