using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
namespace RunWay.Api.Services;
public interface IFileStorageService {
 Task<string> UploadAsync(IFormFile file,string folder,CancellationToken ct=default);
 Task<(Stream Content,string ContentType)?> OpenReadAsync(string blobName,CancellationToken ct=default);
 Task DeleteAsync(string? blobName,CancellationToken ct=default);
}
public static class ImageValidator {
 public const long MaxBytes=5*1024*1024;
 public static string? Validate(IFormFile? file) {
  if(file is null || file.Length==0)return "No file was uploaded.";
  if(file.Length>MaxBytes)return "The image must be 5 MB or smaller.";
  var ext=Path.GetExtension(file.FileName).ToLowerInvariant();
  if(ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))return "Only .jpg, .jpeg, .png and .webp images are allowed.";
  using var stream=file.OpenReadStream();Span<byte> header=stackalloc byte[12];var count=stream.Read(header);
  var png=count>=8&&header[..8].SequenceEqual(new byte[]{137,80,78,71,13,10,26,10});
  var jpeg=count>=3&&header[0]==255&&header[1]==216&&header[2]==255;
  var webp=count>=12&&System.Text.Encoding.ASCII.GetString(header[..4])=="RIFF"&&System.Text.Encoding.ASCII.GetString(header[8..12])=="WEBP";
  return (ext==".png"&&png || (ext is ".jpg" or ".jpeg")&&jpeg || ext==".webp"&&webp)?null:"The file contents do not match a supported image.";
 }
 public static string ContentType(string name)=>Path.GetExtension(name).ToLowerInvariant() switch{".png"=>"image/png",".webp"=>"image/webp",_=>"image/jpeg"};
}
public class AzureBlobStorageService : IFileStorageService {
 private readonly BlobContainerClient _container;
 private readonly ILogger<AzureBlobStorageService> _logger;
 private readonly SemaphoreSlim _initLock=new(1,1);
 private bool _initialised;
 public AzureBlobStorageService(IConfiguration config,ILogger<AzureBlobStorageService> logger) {
  _logger=logger;var container=config["AzureStorage:ContainerName"]??"raceday-images";
  var serviceUri=config["AzureStorage:ServiceUri"];
  _container=string.IsNullOrWhiteSpace(serviceUri)
   ?new BlobContainerClient(config["AzureStorage:ConnectionString"]??throw new InvalidOperationException("Configure AzureStorage:ConnectionString or ServiceUri."),container)
   :new BlobServiceClient(new Uri(serviceUri),new DefaultAzureCredential()).GetBlobContainerClient(container);
 }
 private async Task EnsureContainerAsync(CancellationToken ct) {
  if(_initialised)return;await _initLock.WaitAsync(ct);
  try{if(_initialised)return;await _container.CreateIfNotExistsAsync(PublicAccessType.None,cancellationToken:ct);_initialised=true;}
  finally{_initLock.Release();}
 }
 public async Task<string> UploadAsync(IFormFile file,string folder,CancellationToken ct=default) {
  var error=ImageValidator.Validate(file);if(error!=null)throw new ArgumentException(error);
  if(folder is not ("events" or "profiles"))throw new ArgumentException("Invalid image folder.");
  await EnsureContainerAsync(ct);
  var name=folder+"/"+Guid.NewGuid()+Path.GetExtension(file.FileName).ToLowerInvariant();
  await using var stream=file.OpenReadStream();
  await _container.GetBlobClient(name).UploadAsync(stream,new BlobUploadOptions{HttpHeaders=new BlobHttpHeaders{ContentType=ImageValidator.ContentType(name)}},ct);
  return name;
 }
 public async Task<(Stream Content,string ContentType)?> OpenReadAsync(string name,CancellationToken ct=default) {
  await EnsureContainerAsync(ct);var blob=_container.GetBlobClient(name);
  try{var download=await blob.DownloadStreamingAsync(cancellationToken:ct);return(download.Value.Content,ImageValidator.ContentType(name));}
  catch(Azure.RequestFailedException e)when(e.Status==404){return null;}
 }
 public async Task DeleteAsync(string? name,CancellationToken ct=default) {
  if(string.IsNullOrWhiteSpace(name))return;
  try{await EnsureContainerAsync(ct);await _container.GetBlobClient(name).DeleteIfExistsAsync(cancellationToken:ct);}
  catch(Azure.RequestFailedException e){_logger.LogWarning(e,"Old image cleanup failed; saved records remain usable.");}
 }
}
