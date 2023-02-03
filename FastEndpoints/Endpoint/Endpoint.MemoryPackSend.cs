namespace FastEndpoints;

public abstract partial class Endpoint<TRequest, TResponse> : BaseEndpoint where TRequest : notnull, new()
{
    /// <summary>
    /// send the supplied response dto serialized as json to the client.
    /// </summary>
    /// <param name="response">the object to serialize to json</param>
    /// <param name="statusCode">optional custom http status code</param>
    /// <param name="cancellation">optional cancellation token. if not specified, the <c>HttpContext.RequestAborted</c> token is used</param>
    protected Task SendMemoryPackAsync(TResponse response, int statusCode = 200, CancellationToken cancellation = default)
    {
        _response = response;
        return HttpContext.Response.SendMemoryPackAsync(response, statusCode, cancellation);
    }

    /// <summary>
    /// sends an object serialized as json to the client. if a response interceptor has been defined,
    /// then that will be executed before the normal response is sent.
    /// </summary>
    /// <param name="response">the object to serialize to json</param>
    /// <param name="statusCode">optional custom http status code</param>
    /// <param name="cancellation">optional cancellation token. if not specified, the <c>HttpContext.RequestAborted</c> token is used</param>
    /// <exception cref="InvalidOperationException">will throw if an interceptor has not been defined against the endpoint or globally</exception>
    protected async Task SendInterceptedMemoryPackAsync(object response, int statusCode = 200, CancellationToken cancellation = default)
    {
        if (Definition.ResponseIntrcptr is null)
            throw new InvalidOperationException("Response interceptor has not been configured!");

        await RunResponseInterceptor(Definition.ResponseIntrcptr, response, statusCode, HttpContext, ValidationFailures, cancellation);

        if (!HttpContext.ResponseStarted())
            await HttpContext.Response.SendMemoryPackAsync(response, statusCode, cancellation);
    }

    /// <summary>
    /// send a 201 created response with a location header containing where the resource can be retrieved from.
    /// <para>HINT: if pointing to an endpoint with multiple verbs, make sure to specify the 'verb' argument and if pointing to a multi route endpoint, specify the 'routeNumber' argument.</para>
    /// <para>WARNING: this overload will not add a location header if you've set a custom endpoint name using .WithName() method. use the other overload that accepts a string endpoint name instead.</para>
    /// </summary>
    /// <typeparam name="TEndpoint">the type of the endpoint where the resource can be retrieved from</typeparam>
    /// <param name="routeValues">a route values object with key/value pairs of route information</param>
    /// <param name="responseBody">the content to be serialized in the response body</param>
    /// <param name="verb">only useful when pointing to a multi verb endpoint</param>
    /// <param name="routeNumber">only useful when pointing to a multi route endpoint</param>
    /// <param name="generateAbsoluteUrl">set to true for generating a absolute url instead of relative url for the location header</param>
    /// <param name="cancellation">optional cancellation token. if not specified, the <c>HttpContext.RequestAborted</c> token is used</param>
    protected Task SendCreatedAtMemoryPackAsync<TEndpoint>(object? routeValues,
                                                 TResponse responseBody,
                                                 Http? verb = null,
                                                 int? routeNumber = null,
                                                 bool generateAbsoluteUrl = false,
                                                 CancellationToken cancellation = default) where TEndpoint : IEndpoint
    {
        if (responseBody is not null)
            _response = responseBody;

        return HttpContext.Response.SendCreatedAtMemoryPackAsync<TEndpoint>(
            routeValues,
            responseBody,
            verb,
            routeNumber,
            generateAbsoluteUrl,
            cancellation);
    }

    /// <summary>
    /// send a 201 created response with a location header containing where the resource can be retrieved from.
    /// <para>WARNING: this method is only supported on single verb/route endpoints. it will not produce a `Location` header if used in a multi verb or multi route endpoint.</para>
    /// </summary>
    /// <param name="endpointName">the name of the endpoint to use for link generation (openapi route id)</param>
    /// <param name="routeValues">a route values object with key/value pairs of route information</param>
    /// <param name="responseBody">the content to be serialized in the response body</param>
    /// <param name="generateAbsoluteUrl">set to true for generating a absolute url instead of relative url for the location header</param>
    /// <param name="cancellation">optional cancellation token. if not specified, the <c>HttpContext.RequestAborted</c> token is used</param>
    protected Task SendCreatedAtMemoryPackAsync(string endpointName,
                                      object? routeValues,
                                      TResponse responseBody,
                                      bool generateAbsoluteUrl = false,
                                      CancellationToken cancellation = default)
    {
        if (responseBody is not null)
            _response = responseBody;

        return HttpContext.Response.SendCreatedAtMemoryPackAsync(
            endpointName,
            routeValues,
            responseBody,
            generateAbsoluteUrl,
            cancellation);
    }
}