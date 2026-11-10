using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace RunWay.Web;
public class ApiException(HttpStatusCode status,string message):Exception(message){public HttpStatusCode Status=>status;}
public class ApiClient(HttpClient http,IHttpContextAccessor accessor) {
 public async Task<T> Get<T>(string path)=> (await Send<T>(HttpMethod.Get,path))!;
 public Task<T?> Post<T>(string path,object? body)=>Send<T>(HttpMethod.Post,path,body);
 public Task<T?> Put<T>(string path,object body)=>Send<T>(HttpMethod.Put,path,body);
 public Task Delete(string path)=>Send<object>(HttpMethod.Delete,path);
 public async Task Upload(string path,IFormFile file){
  if(file.Length is <=0 or >2097152)throw new ApiException(HttpStatusCode.BadRequest,"Choose a PNG or JPEG image up to 2 MB.");
  using var form=new MultipartFormDataContent();using var stream=file.OpenReadStream();
  var content=new StreamContent(stream);content.Headers.ContentType=new MediaTypeHeaderValue("application/octet-stream");form.Add(content,"file",Path.GetFileName(file.FileName));
  await Send<object>(HttpMethod.Post,path,form);
 }
 public async Task<byte[]> Media(string name){
  using var response=await http.GetAsync("api/media/"+Uri.EscapeDataString(name));
  if(!response.IsSuccessStatusCode)throw new ApiException(response.StatusCode,"Image unavailable.");
  return await response.Content.ReadAsByteArrayAsync();
 }
 private async Task<T?> Send<T>(HttpMethod method,string path,object? body=null){
  using var req=new HttpRequestMessage(method,path);
  var token=accessor.HttpContext?.Session.GetString("ApiToken");if(token!=null)req.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);
  if(body is HttpContent hc)req.Content=hc;else if(body!=null)req.Content=JsonContent.Create(body);
  using var response=await http.SendAsync(req);
  if(!response.IsSuccessStatusCode){
   var message="The request could not be completed. Please check your details and try again.";
   try{using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());var root=json.RootElement;
    if(root.TryGetProperty("message",out var m))message=m.GetString()??message;
    else if(root.TryGetProperty("errors",out var errors))message=string.Join(" ",errors.EnumerateObject().SelectMany(e=>e.Value.EnumerateArray()).Select(e=>e.GetString()));
   }catch(JsonException){}
   throw new ApiException(response.StatusCode,message);
  }
  if(response.StatusCode==HttpStatusCode.NoContent)return default;
  return await response.Content.ReadFromJsonAsync<T>();
 }
}
public class ApiErrorFilter : IAsyncExceptionFilter {
 public async Task OnExceptionAsync(ExceptionContext context){
  if(context.Exception is not (ApiException or HttpRequestException or TaskCanceledException))return;
  if(context.Exception is ApiException{Status:HttpStatusCode.Unauthorized}){
   context.HttpContext.Session.Clear();await context.HttpContext.SignOutAsync();
   context.Result=new RedirectToActionResult("Login","Account",null);
  }else{
   var message=context.Exception is ApiException e?e.Message:"The RaceDay API is unavailable. Start the API and try again.";
   context.Result=new ViewResult{ViewName="Error",StatusCode=context.Exception is ApiException a?(int)a.Status:503,ViewData=new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),context.ModelState){{"Message",message}}};
  }
  context.ExceptionHandled=true;
 }
}
