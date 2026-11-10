using System.Text.RegularExpressions;
using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
namespace RunWay.Api;
// The MVC project has no Azure SDK reference or storage credentials.
public class ImageStore(IConfiguration config,IWebHostEnvironment env) {
 private string Mode=>config["Storage:Provider"]??"Local";
 private BlobContainerClient Container(){
  var connection=config["Storage:ConnectionString"];
  var service=string.IsNullOrWhiteSpace(connection)
   ?new BlobServiceClient(new Uri(config["Storage:ServiceUri"]??throw new InvalidOperationException("Configure Storage:ServiceUri.")),new DefaultAzureCredential())
   :new BlobServiceClient(connection);
  return service.GetBlobContainerClient(config["Storage:Container"]??"raceday-images");
 }
 private string Folder=>config["Storage:LocalPath"]??Path.Combine(env.ContentRootPath,"App_Data","images");
 public async Task<string> Save(IFormFile file){
  if(file.Length is <=0 or >2097152)throw new ArgumentException("Choose a PNG or JPEG image up to 2 MB.");
  using var buffer=new MemoryStream();await file.CopyToAsync(buffer);var bytes=buffer.ToArray();
  var png=bytes.Length>=8 && bytes.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10});
  var jpeg=bytes.Length>=3 && bytes[0]==255 && bytes[1]==216 && bytes[2]==255;
  if(!png&&!jpeg)throw new ArgumentException("The file must contain a valid PNG or JPEG image.");
  var ext=png?".png":".jpg";var name=Guid.NewGuid().ToString("N")+ext;var contentType=png?"image/png":"image/jpeg";
  if(Mode=="Azure"){
   var container=Container();
   // Provision the private container once in Azure; runtime only needs data permissions.
   buffer.Position=0;await container.GetBlobClient(name).UploadAsync(buffer,new BlobUploadOptions{HttpHeaders=new BlobHttpHeaders{ContentType=contentType}});
  }else{
   if(!env.IsDevelopment() && !config.GetValue<bool>("Storage:AllowLocal"))throw new InvalidOperationException("Configure Azure storage for deployment.");
   Directory.CreateDirectory(Folder);await File.WriteAllBytesAsync(Path.Combine(Folder,name),bytes);
  }
  return "/Media/"+name;
 }
 public async Task<byte[]?> Read(string name){
  if(!Regex.IsMatch(name,@"\A[a-f0-9]{32}\.(png|jpg)\z"))return null;
  if(Mode=="Azure"){try{return (await Container().GetBlobClient(name).DownloadContentAsync()).Value.Content.ToArray();}catch(RequestFailedException e)when(e.Status==404){return null;}}
  var path=Path.Combine(Folder,name);return File.Exists(path)?await File.ReadAllBytesAsync(path):null;
 }
}
