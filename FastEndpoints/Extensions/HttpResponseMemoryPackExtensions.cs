using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using static FastEndpoints.Config;

namespace FastEndpoints;

public static class HttpResponseMemoryPackExtensions
{
    /// <summary>
    /// send the supplied response dto serialized as json to the client.
    /// </summary>
    /// <param name="response">the object to serialize to json</param>
    /// <param name="statusCode">optional custom http status code</param>
    /// <param name="cancellation">optional cancellation token. if not specified, the <c>HttpContext.RequestAborted</c> token is used.</param>
    public static Task SendMemoryPackAsync<TResponse>(this HttpResponse rsp,
                                            TResponse response,
                                            int statusCode = 200,
                                            CancellationToken cancellation = default)
    {
        rsp.HttpContext.MarkResponseStart();
        rsp.StatusCode = statusCode;
        return SerOpts.ResponseSerializer(rsp, response, "application/x-memorypack", null, cancellation.IfDefault(rsp));
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
    /// <param name="cancellation">optional cancellation token. if not specified, the <c>HttpContext.RequestAborted</c> token is used.</param>
    public static Task SendCreatedAtMemoryPackAsync<TEndpoint>(this HttpResponse rsp,
                                                     object? routeValues,
                                                     object? responseBody,
                                                     Http? verb = null,
                                                     int? routeNumber = null,
                                                     bool generateAbsoluteUrl = false,
                                                     CancellationToken cancellation = default) where TEndpoint : IEndpoint
    {
        return SendCreatedAtMemoryPackAsync(
            rsp,
            typeof(TEndpoint).EndpointName(verb?.ToString(), routeNumber),
            routeValues,
            responseBody,
            generateAbsoluteUrl,
            cancellation.IfDefault(rsp));
    }

    /// <summary>
    /// send a 201 created response with a location header containing where the resource can be retrieved from.
    /// <para>WARNING: this method is only supported on single verb/route endpoints. it will not produce a `Location` header if used in a multi verb or multi route endpoint.</para>
    /// </summary>
    /// <param name="endpointName">the name of the endpoint to use for link generation (openapi route id)</param>
    /// <param name="routeValues">a route values object with key/value pairs of route information</param>
    /// <param name="responseBody">the content to be serialized in the response body</param>
    /// <param name="generateAbsoluteUrl">set to true for generating a absolute url instead of relative url for the location header</param>
    /// <param name="cancellation">optional cancellation token. if not specified, the <c>HttpContext.RequestAborted</c> token is used.</param>
    public static Task SendCreatedAtMemoryPackAsync(this HttpResponse rsp,
                                          string endpointName,
                                          object? routeValues,
                                          object? responseBody,
                                          bool generateAbsoluteUrl = false,
                                          CancellationToken cancellation = default)
    {
        var linkGen = Config.ServiceResolver.TryResolve<LinkGenerator>() ?? //this is null for unit tests
                      rsp.HttpContext.RequestServices?.GetService<LinkGenerator>() ?? //so get it from httpcontext (only applies to unit tests). do not change to Resolve<T>() here
                      throw new InvalidOperationException("Service provider is null on the current HttpContext! Set it yourself for unit tests.");

        rsp.HttpContext.MarkResponseStart();
        rsp.StatusCode = 201;
        rsp.Headers.Location = generateAbsoluteUrl
                               ? linkGen.GetUriByName(rsp.HttpContext, endpointName, routeValues)
                               : linkGen.GetPathByName(endpointName, routeValues);

        return responseBody is null
               ? rsp.StartAsync(cancellation.IfDefault(rsp))
               : SerOpts.ResponseSerializer(rsp, responseBody, "application/x-memorypack", null, cancellation.IfDefault(rsp));
    }

    /// <summary>
    /// send an http 200 ok response with the supplied response dto serialized as json to the client.
    /// </summary>
    /// <param name="response">the object to serialize to json</param>
    /// <param name="cancellation">optional cancellation token. if not specified, the <c>HttpContext.RequestAborted</c> token is used.</param>
    public static Task SendOkMemoryPackAsync<TResponse>(this HttpResponse rsp,
                                              TResponse response,
                                              CancellationToken cancellation = default)
    {
        rsp.HttpContext.MarkResponseStart();
        rsp.StatusCode = 200;
        return SerOpts.ResponseSerializer(rsp, response, "application/x-memorypack", null, cancellation.IfDefault(rsp));
    }

#pragma warning disable CS0649
    //this avoids allocation of a new struct instance on every call
    private static readonly CancellationToken defaultToken;
    private static CancellationToken IfDefault(this CancellationToken token, HttpResponse httpResponse)
        => token.Equals(defaultToken)
           ? httpResponse.HttpContext.RequestAborted
           : token;
#pragma warning restore CS0649
}