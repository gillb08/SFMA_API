using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SFMA_API.Models.Dtos.Response;
using SFMA_API.Services.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;

namespace SFMA_API.Services.Handlers
{
    public static class ExceptionHandler
    {
        public static void ConfigureException(this IApplicationBuilder app, IWebHostEnvironment hostEnvironment)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.ContentType = "application/json";

                    IExceptionHandlerFeature? exceptionHandleFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (exceptionHandleFeature != null)
                    {
                        var error = exceptionHandleFeature.Error;
                        int statusCode = StatusCodes.Status500InternalServerError;
                        object? data = null;

                        switch (error)
                        {
                            case DuplicateRecordException dup:
                                statusCode = StatusCodes.Status409Conflict;
                                data = new { error = "DUPLICATE_RECORD", collisionField = dup.CollisionField, existingScholar = dup.ExistingScholar };
                                break;
                            case DeadlineElapsedException dead:
                                statusCode = StatusCodes.Status403Forbidden;
                                data = new { error = dead.DeadlineCode };
                                break;
                            case RecordLockedException:
                                statusCode = StatusCodes.Status423Locked;
                                break;
                            case InvalidSubmissionFormatException inv:
                                statusCode = StatusCodes.Status400BadRequest;
                                data = new { error = "INVALID_SUBMISSION_FORMAT", requiredFormat = inv.RequiredFormat };
                                break;
                            case InvalidDataException:
                            case InvalidOperationException:
                            case KeyNotFoundException:
                            case ArgumentException:
                            case SecurityTokenException:
                                statusCode = StatusCodes.Status400BadRequest;
                                break;
                            case DbUpdateException:
                                statusCode = StatusCodes.Status409Conflict;
                                break;
                            case UnauthorizedAccessException:
                                statusCode = StatusCodes.Status403Forbidden;
                                break;
                            case OperationCanceledException:
                                statusCode = StatusCodes.Status401Unauthorized;
                                break;
                            default:
                                statusCode = StatusCodes.Status500InternalServerError;
                                break;
                        }

                        context.Response.StatusCode = statusCode;

                        var err = new ErrorResponse
                        {
                            Success = false,
                            Status = statusCode,
                            Message = hostEnvironment.IsProduction() && statusCode == StatusCodes.Status500InternalServerError
                                ? "We currently cannot complete this request process. Please retry or contact our support"
                                : error.Message,
                            Data = data
                        };

                        var serializerSettings = new JsonSerializerSettings
                        {
                            ContractResolver = new CamelCasePropertyNamesContractResolver()
                        };
                        string msg = JsonConvert.SerializeObject(err, serializerSettings);
                        await context.Response.WriteAsync(msg);
                    }
                });
            });
        }
    }
}
