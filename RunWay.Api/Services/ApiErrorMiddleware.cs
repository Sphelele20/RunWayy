using Microsoft.EntityFrameworkCore;
namespace RunWay.Api.Services;
public class ApiErrorMiddleware(RequestDelegate next,ILogger<ApiErrorMiddleware> logger) {
 public async Task InvokeAsync(HttpContext context) {
  try { await next(context); }
  catch(OperationCanceledException) when(context.RequestAborted.IsCancellationRequested) {}
  catch(Exception e) when(!context.Response.HasStarted) {
   var status=e is ArgumentException?400:e is DbUpdateException?409:500;
   if(status==500)logger.LogError(e,"API request failed");
   context.Response.Clear();context.Response.StatusCode=status;
   await context.Response.WriteAsJsonAsync(new {title="Request could not be completed",status,detail=status==400?e.Message:status==409?"This change conflicts with another record. Refresh and try again.":"The service could not complete your request. Please try again."});
  }
 }
}
