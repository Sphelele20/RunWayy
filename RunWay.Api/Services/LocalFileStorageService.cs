using System.Text.RegularExpressions;
namespace RunWay.Api.Services;
// Development only. The Web project still accesses every image through the API.
public class LocalFileStorageService(IConfiguration config,IWebHostEnvironment env) : IFileStorageService {
 private string Root => config["Storage:LocalPath"] ?? Path.Combine(env.ContentRootPath,"App_Data","images");
 private string? Resolve(string name) {
  if(!Regex.IsMatch(name,@"\A(events|profiles)/[a-f0-9-]{36}\.(jpg|jpeg|png|webp)\z")) return null;
  return Path.Combine(Root,name.Replace('/',Path.DirectorySeparatorChar));
 }
 public async Task<string> UploadAsync(IFormFile file,string folder,CancellationToken ct=default) {
  var error=ImageValidator.Validate(file);if(error!=null)throw new ArgumentException(error);
  if(folder is not ("events" or "profiles"))throw new ArgumentException("Invalid image folder.");
  var name=folder+"/"+Guid.NewGuid()+Path.GetExtension(file.FileName).ToLowerInvariant();
  var path=Resolve(name)!;Directory.CreateDirectory(Path.GetDirectoryName(path)!);
  await using var target=File.Create(path);await file.CopyToAsync(target,ct);return name;
 }
 public Task<(Stream Content,string ContentType)?> OpenReadAsync(string name,CancellationToken ct=default) {
  var path=Resolve(name);
  if(path==null||!File.Exists(path))return Task.FromResult<(Stream,string)?>(null);
  return Task.FromResult<(Stream,string)?>((File.OpenRead(path),ImageValidator.ContentType(name)));
 }
 public Task DeleteAsync(string? name,CancellationToken ct=default) {
  var path=name==null?null:Resolve(name);if(path!=null&&File.Exists(path))File.Delete(path);return Task.CompletedTask;
 }
}
