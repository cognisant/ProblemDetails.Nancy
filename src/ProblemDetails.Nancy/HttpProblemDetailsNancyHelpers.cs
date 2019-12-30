// <copyright file="HttpProblemDetailsNancyHelpers.cs" company="Corsham Science">
// Copyright (c) Corsham Science. All rights reserved.
// </copyright>

namespace CorshamScience.ProblemDetails.Nancy
{
    using System;
    using System.Linq;
    using CR.ProblemDetails;
    using global::Nancy;
    using global::Nancy.Bootstrapper;
    using global::Nancy.TinyIoc;

    /// <summary>
    /// Helper methods used to implement <see cref="IHttpProblemDetails"/> usage with your Nancy APIs.
    /// </summary>
    public static class HttpProblemDetailsNancyHelpers
    {
        /// <summary>
        /// An <see cref="Action"/> which is performed before returning <see cref="IHttpProblemDetails"/> in the <see cref="ErrorPipeline"/>.
        /// </summary>
        /// <param name="context">The current <see cref="NancyContext"/>.</param>
        /// <param name="ex">The <see cref="Exception"/> which was thrown to trigger the <see cref="ErrorPipeline"/>.</param>
        /// <param name="problemDetails">The <see cref="IHttpProblemDetails"/> created or extraced from the <see cref="Exception"/>.</param>
        /// <remarks>
        /// If the throw <see cref="Exception"/> was a <see cref="HttpProblemDetailsException"/>, the <see cref="IHttpProblemDetails"/> will be taken from it,
        /// otherwise they will be build based on the current context and the content and type of the exception.
        /// </remarks>
        public delegate void NancyOnErrorHttpProblemAction(NancyContext context, Exception ex, IHttpProblemDetails problemDetails);

        /// <summary>
        /// Gets or sets the current <see cref="Action"/> performed when handling an <see cref="Exception"/> in the <see cref="ErrorPipeline"/>; called once <see cref="IHttpProblemDetails"/> have been established.
        /// </summary>
        public static NancyOnErrorHttpProblemAction OnErrorHttpProblemAction { get; set; }

        /// <summary>
        /// An <see cref="ErrorPipeline"/> extension to automatically return <see cref="IHttpProblemDetails"/> when an exception is while processing a request.
        /// </summary>
        /// <param name="context">The current <see cref="NancyContext"/> for the request.</param>
        /// <param name="exception">The <see cref="Exception"/> which was thrown to trigger the <see cref="ErrorPipeline"/>.</param>
        /// <returns>
        /// <see cref="IHttpProblemDetails"/> based on the <see cref="Exception"/> which was thrown, and the current <see cref="NancyContext"/>.
        /// If the <see cref="Exception"/> was a <see cref="HttpProblemDetailsException"/>, the <see cref="IHttpProblemDetails"/> from it will be used—otherwise,
        /// <see cref="IHttpProblemDetails"/> will be built based on the context's request <see cref="Url"/>, a generic title, and the type and message of the <see cref="Exception"/>.
        /// </returns>
        /// <example>
        /// This method can be added to the <see cref="ErrorPipeline"/> of a <see cref="INancyBootstrapper"/> in the following way:
        /// private sealed class ExampleBoostrapper : <see cref="DefaultNancyBootstrapper"/>
        /// {
        ///     protected override void RequestStartup(<see cref="TinyIoCContainer"/> container, <see cref="IPipelines"/> pipelines, <see cref="NancyContext"/> context)
        ///     {
        ///         base.RequestStartup(container, pipelines, context);
        ///         pipelines.OnError.AddItemToEndOfPipeline(<see cref="HttpProblemDetailsNancyHelpers"/>.<see cref="HttpProblemDetailsOnError"/>);
        ///         <see cref="HttpProblemDetailsNancyHelpers"/>.<see cref="OnErrorHttpProblemAction "/> = (c, ex, pd) => Log.Error(ex, $"Failed to handle a request; exception of type {ex.GetType().Name} was thrown, with message: '{ex.Message}'.", pd);
        ///     }
        /// }
        ///
        /// The <see cref="OnErrorHttpProblemAction"/> is optional; if it is set to null, no action will be executed.
        /// </example>
        public static dynamic HttpProblemDetailsOnError(NancyContext context, Exception exception)
        {
            var problemDetails = exception is HttpProblemDetailsException detailedException
                ? detailedException.ProblemDetails
                : new HttpProblemDetails(
                    context.Request.Url,
                    context.Request.Url,
                    "An error occured while processing your request.",
                    $"{exception.GetType().Name} - {(string.IsNullOrWhiteSpace(exception.Message) ? "No Message" : $"'{exception.Message}'")}.",
                    (int)HttpStatusCode.InternalServerError);

            OnErrorHttpProblemAction?.Invoke(context, exception, problemDetails);

            context.Response.ContentType =
                context.Request.Headers.Any(a => a.Key == "application/xml") && context.Request.Headers.Accept.All(a => a.Item1 != "application/json")
                    ? "application/problem+xml"
                    : "application/problem+json";

            context.Response.StatusCode = Enum.IsDefined(typeof(HttpStatusCode), problemDetails.Status)
                ? (HttpStatusCode)problemDetails.Status
                : HttpStatusCode.InternalServerError;

            return problemDetails;
        }
    }
}
