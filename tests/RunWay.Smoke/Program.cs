using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using RunWay.Contracts;
SQLitePCL.Batteries_V2.Init();
var root=Path.GetFullPath(args.FirstOrDefault()??Directory.GetCurrentDirectory());
var config=args.Length>1?args[1]:"Debug";
var scratch=Path.Combine(Path.GetTempPath(),"runway-smoke-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(scratch);
var logs=new List<string>();var checks=0;
var apiPort=FreePort();var webPort=FreePort();
var dbPath=Path.Combine(scratch,"smoke.db");
Process Start(string project,int port,Dictionary<string,string> env,string cwd){
 var dll=Path.Combine(root,"src",project,"bin",config,"net9.0",project+".dll");
 var info=new ProcessStartInfo("dotnet"){WorkingDirectory=cwd,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
 info.ArgumentList.Add(dll);info.Environment["ASPNETCORE_URLS"]=$"http://127.0.0.1:{port}";info.Environment["ASPNETCORE_ENVIRONMENT"]="Development";
 foreach(var entry in env)info.Environment[entry.Key]=entry.Value;
 var p=Process.Start(info)!;p.OutputDataReceived+=(_,e)=>{if(e.Data!=null)lock(logs)logs.Add(e.Data);};p.ErrorDataReceived+=(_,e)=>{if(e.Data!=null)lock(logs)logs.Add(e.Data);};p.BeginOutputReadLine();p.BeginErrorReadLine();return p;
}
using var apiProcess=Start("RunWay.Api",apiPort,new(){["ConnectionStrings__RaceDay"]="Data Source="+dbPath},scratch);
using var webProcess=Start("RunWay.Web",webPort,new(){["Api__BaseUrl"]=$"http://127.0.0.1:{apiPort}/",["DataProtection__Path"]=Path.Combine(scratch,"keys")},Path.Combine(root,"src","RunWay.Web"));
using var api=new HttpClient{BaseAddress=new Uri($"http://127.0.0.1:{apiPort}/")};
using var browser=new HttpClient(new HttpClientHandler{CookieContainer=new CookieContainer(),AllowAutoRedirect=false}){BaseAddress=new Uri($"http://127.0.0.1:{webPort}/")};
try{
 await Ready(api);await Ready(browser);
 async Task<HttpResponseMessage> Call(HttpMethod method,string path,object? body=null,string? token=null){
  using var req=new HttpRequestMessage(method,path);if(body!=null)req.Content=JsonContent.Create(body);if(token!=null)req.Headers.Authorization=new("Bearer",token);return await api.SendAsync(req);
 }
 async Task<HttpResponseMessage> Expect(HttpMethod method,string path,HttpStatusCode status,object? body=null,string? token=null){
  var res=await Call(method,path,body,token);Check(res.StatusCode==status,$"{method} {path}: expected {(int)status}, got {(int)res.StatusCode}: "+(res.StatusCode!=status?await res.Content.ReadAsStringAsync():""));return res;
 }
 async Task<AuthResponse> Register(string email,string role){
  var response=await Expect(HttpMethod.Post,"api/auth/register",HttpStatusCode.OK,new RegisterRequest{Email=email,Password="StrongTest!42",FirstName="Test",LastName=role,Role=role});
  return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
 }
 await Expect(HttpMethod.Post,"api/events",HttpStatusCode.Unauthorized,new EventRequest());
 await Expect(HttpMethod.Post,"api/auth/register",HttpStatusCode.BadRequest,new RegisterRequest{Email="admin@test.local",Password="StrongTest!42",FirstName="Test",LastName="Admin",Role="Admin"});
 var organiser=await Register("owner@test.local","Organiser");var other=await Register("other@test.local","Organiser");var runner=await Register("runner@test.local","Participant");
 await Expect(HttpMethod.Post,"api/auth/register",HttpStatusCode.BadRequest,new RegisterRequest{Email="runner@test.local",Password="StrongTest!42",FirstName="Duplicate",LastName="Test"});
 await Expect(HttpMethod.Post,"api/auth/login",HttpStatusCode.Unauthorized,new LoginRequest{Email="runner@test.local",Password="WrongPass!1"});
 var input=new EventRequest{Name="Test City 10K",Description="Integration test event",Location="Johannesburg",Date=DateTime.UtcNow.AddDays(7),Distance=10};
 var created=await Expect(HttpMethod.Post,"api/events",HttpStatusCode.Created,input,organiser.Token);
 var race=(await created.Content.ReadFromJsonAsync<EventDto>())!;var path=$"api/events/{race.Id}";
 await Expect(HttpMethod.Post,"api/events",HttpStatusCode.Forbidden,input,runner.Token);
 await Expect(HttpMethod.Put,path,HttpStatusCode.Forbidden,input,other.Token);
 await Expect(HttpMethod.Delete,path,HttpStatusCode.Forbidden,null,other.Token);
 await Expect(HttpMethod.Get,path+"/enrolments",HttpStatusCode.Forbidden,null,other.Token);
 var invalid=new EventRequest{Name="",Description="x",Location="x",Distance=-1};await Expect(HttpMethod.Post,"api/events",HttpStatusCode.BadRequest,invalid,organiser.Token);
 input.Date=DateTime.UtcNow.AddDays(-1);await Expect(HttpMethod.Post,"api/events",HttpStatusCode.BadRequest,input,organiser.Token);input.Date=DateTime.UtcNow.AddDays(7);
 var categoryResponse=await Expect(HttpMethod.Post,path+"/categories",HttpStatusCode.OK,new CategoryRequest{Name="Open"},organiser.Token);
 var category=(await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>())!;
 await Expect(HttpMethod.Post,path+"/categories",HttpStatusCode.Conflict,new CategoryRequest{Name="Open"},organiser.Token);
 await Expect(HttpMethod.Post,path+"/enrolments",HttpStatusCode.BadRequest,new EnrolRequest(99999),runner.Token);
 await Expect(HttpMethod.Post,path+"/enrolments",HttpStatusCode.Created,new EnrolRequest(category.Id),runner.Token);
 await Expect(HttpMethod.Post,path+"/enrolments",HttpStatusCode.Conflict,new EnrolRequest(category.Id),runner.Token);
 await Expect(HttpMethod.Delete,path+$"/categories/{category.Id}",HttpStatusCode.Conflict,null,organiser.Token);
 var mine=await (await Expect(HttpMethod.Get,"api/me/enrolments",HttpStatusCode.OK,null,runner.Token)).Content.ReadFromJsonAsync<List<EnrolmentDto>>();
 Check(mine!.Count==1 && mine[0].Status=="Confirmed","Entry immediately visible");
 var enrolment=mine[0];
 await Expect(HttpMethod.Put,path+$"/enrolments/{enrolment.Id}/result",HttpStatusCode.BadRequest,new ResultRequest{FinishTime="00:45:12",Position=3},organiser.Token);
 using(var db=new SqliteConnection("Data Source="+dbPath)){
  await db.OpenAsync();using var command=db.CreateCommand();command.CommandText="UPDATE Events SET Date=$date WHERE Id=$id";command.Parameters.AddWithValue("$date",DateTime.UtcNow.AddDays(-1));command.Parameters.AddWithValue("$id",race.Id);await command.ExecuteNonQueryAsync();
 }
 await Expect(HttpMethod.Post,path+"/enrolments",HttpStatusCode.BadRequest,new EnrolRequest(category.Id),runner.Token);
 await Expect(HttpMethod.Put,path+$"/enrolments/{enrolment.Id}/result",HttpStatusCode.BadRequest,new ResultRequest{FinishTime="bad",Position=0},organiser.Token);
 await Expect(HttpMethod.Put,path+$"/enrolments/{enrolment.Id}/result",HttpStatusCode.NoContent,new ResultRequest{FinishTime="00:45:12",Position=3},organiser.Token);
 var results=await (await Expect(HttpMethod.Get,"api/me/enrolments",HttpStatusCode.OK,null,runner.Token)).Content.ReadFromJsonAsync<List<EnrolmentDto>>();
 Check(results![0].FinishTime=="00:45:12"&&results[0].Position==3&&results[0].Status=="Completed","Published results visible to participant");
 await Expect(HttpMethod.Get,"api/me/enrolments",HttpStatusCode.Forbidden,null,organiser.Token);
 var updated=await (await Expect(HttpMethod.Put,"api/profile",HttpStatusCode.OK,new ProfileRequest{FirstName="Updated",LastName="Runner",Gender="Other",PhoneNumber="0821234567"},runner.Token)).Content.ReadFromJsonAsync<ProfileDto>();Check(updated!.FirstName=="Updated","Profile saved");
 var png=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aDY0AAAAASUVORK5CYII=");
 async Task<HttpResponseMessage> Upload(string endpoint,string token,byte[] bytes){
  using var req=new HttpRequestMessage(HttpMethod.Post,endpoint);req.Headers.Authorization=new("Bearer",token);using var form=new MultipartFormDataContent();form.Add(new ByteArrayContent(bytes),"file","test.png");req.Content=form;return await api.SendAsync(req);
 }
 var upload=await Upload(path+"/banner",organiser.Token,png);Check(upload.IsSuccessStatusCode,"Banner upload");
 var image=await (await Expect(HttpMethod.Get,path,HttpStatusCode.OK)).Content.ReadFromJsonAsync<EventDto>();
 var data=await api.GetByteArrayAsync("api/media/"+image!.BannerUrl!.Split('/').Last());Check(data.SequenceEqual(png),"Image round trip");
 Check((await Upload(path+"/banner",other.Token,png)).StatusCode==HttpStatusCode.Forbidden,"Banner ownership");
 Check((await Upload("api/profile/image",runner.Token,new byte[]{1,2,3})).StatusCode==HttpStatusCode.BadRequest,"Reject fake images");
 Check((await Upload("api/profile/image",runner.Token,new byte[2097153])).StatusCode==HttpStatusCode.BadRequest,"Reject oversized images");
 Check((await Upload("api/profile/image",runner.Token,png)).IsSuccessStatusCode,"Profile image upload");
 var anon=await browser.GetAsync("Participant/Entries");Check(anon.StatusCode==HttpStatusCode.Redirect&&anon.Headers.Location!.ToString().Contains("Account/Login"),"MVC protects anonymous pages");
 var loginPage=await browser.GetStringAsync("Account/Login");var csrf=Regex.Match(loginPage,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;Check(csrf.Length>0,"Login has anti-forgery token");
 var noCsrf=await browser.PostAsync("Account/Login",new FormUrlEncodedContent(new Dictionary<string,string>{{"Email","runner@test.local"},{"Password","StrongTest!42"}}));Check(noCsrf.StatusCode==HttpStatusCode.BadRequest,"MVC rejects missing anti-forgery token");
 var login=await browser.PostAsync("Account/Login",new FormUrlEncodedContent(new Dictionary<string,string>{{"Email","runner@test.local"},{"Password","StrongTest!42"},{"__RequestVerificationToken",WebUtility.HtmlDecode(csrf)}}));Check(login.StatusCode==HttpStatusCode.Redirect,"MVC API sign in");
 foreach(var page in new[]{"Participant/Entries","Participant/Results","Account/Profile","Events/Details/"+race.Id}){var response=await browser.GetAsync(page);Check(response.IsSuccessStatusCode,"MVC renders "+page);}
 var denied=await browser.GetAsync("Organiser/Dashboard");Check(denied.StatusCode==HttpStatusCode.Redirect&&denied.Headers.Location!.ToString().Contains("Denied"),"MVC restricts organiser pages");
 var profilePage=await browser.GetStringAsync("Account/Profile");csrf=Regex.Match(profilePage,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
 var logout=await browser.PostAsync("Account/Logout",new FormUrlEncodedContent(new Dictionary<string,string>{{"__RequestVerificationToken",WebUtility.HtmlDecode(csrf)}}));Check(logout.StatusCode==HttpStatusCode.Redirect,"MVC sign out");
 await Expect(HttpMethod.Post,"api/auth/logout",HttpStatusCode.NoContent,null,runner.Token);await Expect(HttpMethod.Get,"api/profile",HttpStatusCode.Unauthorized,null,runner.Token);
 await Expect(HttpMethod.Delete,path,HttpStatusCode.NoContent,null,organiser.Token);await Expect(HttpMethod.Get,path,HttpStatusCode.NotFound);
 Console.WriteLine($"PASS: {checks} integration checks.");
}catch(Exception e){Console.Error.WriteLine(e);lock(logs)foreach(var line in logs.TakeLast(100))Console.Error.WriteLine(line);Environment.ExitCode=1;}
finally{foreach(var p in new[]{apiProcess,webProcess})if(!p.HasExited)p.Kill(true);Console.WriteLine("Test data: "+scratch);}
void Check(bool condition,string description){if(!condition)throw new Exception("FAIL: "+description);checks++;Console.WriteLine("PASS: "+description);}
static async Task Ready(HttpClient client){for(var i=0;i<100;i++){try{if((await client.GetAsync("health")).IsSuccessStatusCode)return;}catch(HttpRequestException){}await Task.Delay(200);}throw new Exception("Application did not start.");}
static int FreePort(){var listener=new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback,0);listener.Start();var port=((System.Net.IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();return port;}
