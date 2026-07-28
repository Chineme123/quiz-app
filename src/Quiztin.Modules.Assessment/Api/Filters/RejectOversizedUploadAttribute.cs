using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Quiztin.Modules.Assessment.Api.Filters
{
    /// <summary>
    /// Rejects an over-cap upload with 413 Payload Too Large before model binding reads the body,
    /// so an oversized source file gets the documented status (spec 0009, AC-7) rather than the 400
    /// that multipart form binding produces when it trips its own length limit. As a resource filter
    /// it runs before binding, and it checks only the Content-Length header, so nothing is buffered.
    /// The [RequestSizeLimit] / [RequestFormLimits] attributes stay on the action as the backstop for
    /// the rare request that streams a body with no Content-Length.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RejectOversizedUploadAttribute : Attribute, IAsyncResourceFilter
    {
        private readonly long _maxBytes;

        public RejectOversizedUploadAttribute(long maxBytes)
        {
            _maxBytes = maxBytes;
        }

        public Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            if (context.HttpContext.Request.ContentLength is long length && length > _maxBytes)
            {
                context.Result = new ObjectResult(new { error = $"The file is too large. The limit is {_maxBytes} bytes." })
                {
                    StatusCode = StatusCodes.Status413PayloadTooLarge
                };
                return Task.CompletedTask;
            }

            return next();
        }
    }
}
