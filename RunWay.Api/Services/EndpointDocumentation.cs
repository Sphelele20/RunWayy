using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace RunWay.Api.Services;
public class EndpointDocumentation : IOperationFilter {
 public void Apply(OpenApiOperation operation,OperationFilterContext context){
  var method=context.ApiDescription.HttpMethod??"GET";
  var route=context.ApiDescription.RelativePath??"";
  var auth=context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().ToList();
  var roles=string.Join(", ",auth.Select(a=>a.Roles).Where(r=>!string.IsNullOrEmpty(r)));
  var access=auth.Count==0?"Public endpoint.":string.IsNullOrEmpty(roles)?"Requires a signed-in user.":"Requires role: "+roles+". Event changes are restricted to the event owner or an Admin.";
  if(string.IsNullOrWhiteSpace(operation.Summary))operation.Summary=context.MethodInfo.DeclaringType?.Name.Replace("Controller","")+" — "+context.MethodInfo.Name;
  operation.Description=(operation.Description??"")+"\n\n"+access+" Request fields and data types are shown in the schema below. Event times are South African time (UTC+2).";
  if(auth.Count==0)operation.Security=new List<OpenApiSecurityRequirement>();
  else{operation.Responses.TryAdd("401",new(){Description="Missing, expired or revoked bearer session."});operation.Responses.TryAdd("403",new(){Description="Role or ownership does not permit this operation."});}
  if(method is "POST" or "PUT"){
   operation.Responses.TryAdd("400",new(){Description="Validation failed or the operation violates an event rule."});
   operation.Responses.TryAdd("409",new(){Description="Duplicate entry/category/position or a conflicting published result."});
  }
  if(route.Contains('{'))operation.Responses.TryAdd("404",new(){Description="The requested record or image does not exist."});
  if(method=="POST"&&route=="api/events")operation.Responses.TryAdd("201",new(){Description="Event created; Location points to the event endpoint."});
  if(method=="DELETE"||route=="api/results"||route.EndsWith("/status")||route.EndsWith("/cancel")||route=="api/auth/logout")
   operation.Responses.TryAdd("204",new(){Description="Operation completed successfully; no response body."});
 }
}
