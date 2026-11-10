using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using RunWay.Api.Dtos;
using RunWay.Api.Services;
using RunWay.Api.Data;
var root=Path.GetFullPath(args.FirstOrDefault()??".");var configuration=args.Length>1?args[1]:"Debug";
var external=args.Length>3;var scratch=Path.Combine(Path.GetTempPath(),"poe-smoke-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(scratch);
var apiUrl=external?args[2]:$"http://127.0.0.1:{FreePort()}/";var webUrl=external?args[3]:$"http://127.0.0.1:{FreePort()}/";
var logs=new List<string>();var processes=new List<Process>();int checks=0;
Process Start(string name,string url,Dictionary<string,string> values){
 var info=new ProcessStartInfo("dotnet"){WorkingDirectory=Path.Combine(root,name),UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
 info.ArgumentList.Add(Path.Combine(root,name,"bin",configuration,"net9.0",name+".dll"));
 info.Environment["ASPNETCORE_URLS"]=url;info.Environment["ASPNETCORE_ENVIRONMENT"]="Development";
 info.Environment["DataProtection__Path"]=Path.Combine(scratch,name,"keys");
 foreach(var pair in values)info.Environment[pair.Key]=pair.Value;
 var process=Process.Start(info)!;process.OutputDataReceived+=(_,e)=>{if(e.Data!=null)lock(logs)logs.Add(e.Data);};process.ErrorDataReceived+=(_,e)=>{if(e.Data!=null)lock(logs)logs.Add(e.Data);};process.BeginOutputReadLine();process.BeginErrorReadLine();processes.Add(process);return process;
}
if(!external){
 Start("RunWay.Api",apiUrl,new(){["ConnectionStrings__DefaultConnection"]="Data Source="+Path.Combine(scratch,"test.db"),["Storage__LocalPath"]=Path.Combine(scratch,"images"),["Seed__DemoData"]="true"});
 Start("RunWay.Web",webUrl,new(){["Api__BaseUrl"]=apiUrl});
}
var json=new JsonSerializerOptions(JsonSerializerDefaults.Web){Converters={new JsonStringEnumConverter()}};
using var api=new HttpClient{BaseAddress=new Uri(apiUrl),Timeout=TimeSpan.FromSeconds(15)};
HttpClient Browser()=>new(new HttpClientHandler{CookieContainer=new CookieContainer(),AllowAutoRedirect=false}){BaseAddress=new Uri(webUrl),Timeout=TimeSpan.FromSeconds(15)};
try{
 await Ready(api);using var browser=Browser();await Ready(browser);
 async Task<HttpResponseMessage> Expect(HttpMethod method,string path,HttpStatusCode status,object? body=null,string? token=null){
  using var req=new HttpRequestMessage(method,path);if(body!=null)req.Content=JsonContent.Create(body,options:json);if(token!=null)req.Headers.Authorization=new("Bearer",token);
  var result=await api.SendAsync(req);Check(result.StatusCode==status,$"{method} {path}: expected {(int)status}, received {(int)result.StatusCode}"+(result.StatusCode==status?"":": "+await result.Content.ReadAsStringAsync()));return result;
 }
 async Task<T> Read<T>(HttpResponseMessage response)=>(await response.Content.ReadFromJsonAsync<T>(json))!;
 async Task<AuthResponse> Login(string email,string password)=>await Read<AuthResponse>(await Expect(HttpMethod.Post,"api/auth/login",HttpStatusCode.OK,new LoginRequest{Email=email,Password=password}));
 async Task<AuthResponse> Register(string role)=>await Read<AuthResponse>(await Expect(HttpMethod.Post,"api/auth/register",HttpStatusCode.OK,new RegisterRequest{Email=Guid.NewGuid()+"@test.local",Password="TestStrong!42",FirstName="Smoke",LastName=role,Role=role}));
 var organiser=await Login("organiser@raceday.com","Organiser@123");var participant=await Login("participant@raceday.com","Participant@123");var admin=await Login("admin@admin.com","Admin@123");
 var other=await Register("Organiser");var runner=await Register("Participant");var second=await Register("Participant");
 var events=await Read<List<EventDto>>(await Expect(HttpMethod.Get,"api/events",HttpStatusCode.OK));
 Check(events.Count>=3&&events.All(e=>!e.IsPast),"Demo upcoming events rendered");
 var filtered=await Read<List<EventDto>>(await Expect(HttpMethod.Get,"api/events?search=Sunrise&eventType=Road&minDistance=9&maxDistance=11",HttpStatusCode.OK));
 Check(filtered.Count==1,"Search, type and distance filters");
 await Expect(HttpMethod.Post,"api/auth/register",HttpStatusCode.BadRequest,new RegisterRequest{Email="bad@test.local",Password="TestStrong!42",FirstName="Bad",LastName="Admin",Role="Admin"});
 await Expect(HttpMethod.Post,"api/auth/login",HttpStatusCode.Unauthorized,new LoginRequest{Email="participant@raceday.com",Password="wrong"});
 var input=new EventRequest{Name="Smoke test race",Description="Temporary integration fixture",Date=RaceClock.Now.AddDays(7),Location="Johannesburg",DistanceKm=10,EventType=EventType.Road};
 await Expect(HttpMethod.Post,"api/events",HttpStatusCode.Unauthorized,input);
 await Expect(HttpMethod.Post,"api/events",HttpStatusCode.Forbidden,input,runner.Token);
 var created=await Read<EventDto>(await Expect(HttpMethod.Post,"api/events",HttpStatusCode.Created,input,organiser.Token));var eventPath="api/events/"+created.Id;
 await Expect(HttpMethod.Put,eventPath,HttpStatusCode.Forbidden,input,other.Token);
 await Expect(HttpMethod.Delete,eventPath,HttpStatusCode.Forbidden,null,other.Token);
 input.Date=RaceClock.Now.AddDays(-1);await Expect(HttpMethod.Post,"api/events",HttpStatusCode.BadRequest,input,organiser.Token);input.Date=RaceClock.Now.AddDays(7);
 input.Name="Updated test race";await Expect(HttpMethod.Put,eventPath,HttpStatusCode.OK,input,organiser.Token);
 var c=await Read<CategoryDto>(await Expect(HttpMethod.Post,eventPath+"/categories",HttpStatusCode.OK,new CategoryRequest{Name="Open",DistanceKm=10},organiser.Token));
 await Expect(HttpMethod.Post,eventPath+"/categories",HttpStatusCode.Conflict,new CategoryRequest{Name="Open",DistanceKm=10},organiser.Token);
 await Expect(HttpMethod.Put,"api/categories/"+c.Id,HttpStatusCode.Forbidden,new CategoryRequest{Name="Bad edit",DistanceKm=10},other.Token);
 var entry=await Read<EnrolmentDto>(await Expect(HttpMethod.Post,"api/enrolments",HttpStatusCode.OK,new EnrolRequest{EventId=created.Id,CategoryId=c.Id},runner.Token));
 Check(entry.Status==EnrolmentStatus.Pending,"Entry is immediately pending");
 await Expect(HttpMethod.Post,"api/enrolments",HttpStatusCode.Conflict,new EnrolRequest{EventId=created.Id,CategoryId=c.Id},runner.Token);
 await Expect(HttpMethod.Post,"api/enrolments",HttpStatusCode.BadRequest,new EnrolRequest{EventId=created.Id,CategoryId=999999},second.Token);
 await Expect(HttpMethod.Delete,"api/categories/"+c.Id,HttpStatusCode.Conflict,null,organiser.Token);
 await Expect(HttpMethod.Put,$"api/enrolments/{entry.Id}/status",HttpStatusCode.Forbidden,new UpdateEnrolmentStatusRequest{Status=EnrolmentStatus.Confirmed},other.Token);
 await Expect(HttpMethod.Put,$"api/enrolments/{entry.Id}/status",HttpStatusCode.NoContent,new UpdateEnrolmentStatusRequest{Status=EnrolmentStatus.Confirmed},organiser.Token);
 await Expect(HttpMethod.Put,"api/results",HttpStatusCode.BadRequest,new ResultRequest{EnrolmentId=entry.Id,FinishTime=TimeSpan.FromMinutes(45),Position=1},organiser.Token);
 await Expect(HttpMethod.Put,$"api/enrolments/{entry.Id}/cancel",HttpStatusCode.NoContent,null,runner.Token);
 var reentry=await Read<EnrolmentDto>(await Expect(HttpMethod.Post,"api/enrolments",HttpStatusCode.OK,new EnrolRequest{EventId=created.Id,CategoryId=c.Id},runner.Token));Check(reentry.Id==entry.Id,"Re-enrolment reuses the entry");
 var past=(await Read<List<EventDto>>(await Expect(HttpMethod.Get,"api/events/mine",HttpStatusCode.OK,null,organiser.Token))).First(e=>e.IsPast);
 var startList=await Read<List<EnrolmentDto>>(await Expect(HttpMethod.Get,$"api/events/{past.Id}/enrolments",HttpStatusCode.OK,null,organiser.Token));
 var row=startList.First(e=>e.ParticipantId==participant.UserId);
 await Expect(HttpMethod.Put,"api/results",HttpStatusCode.NoContent,new ResultRequest{EnrolmentId=row.Id,FinishTime=TimeSpan.FromMinutes(90),Position=row.Position!.Value},organiser.Token);
 await Expect(HttpMethod.Put,$"api/enrolments/{row.Id}/status",HttpStatusCode.Conflict,new UpdateEnrolmentStatusRequest{Status=EnrolmentStatus.Cancelled},organiser.Token);
 var otherRow=startList.First(e=>e.Id!=row.Id);
 await Expect(HttpMethod.Put,"api/results",HttpStatusCode.Conflict,new ResultRequest{EnrolmentId=otherRow.Id,FinishTime=TimeSpan.FromMinutes(95),Position=row.Position.Value},organiser.Token);
 var results=await Read<List<ResultDto>>(await Expect(HttpMethod.Get,"api/results/mine",HttpStatusCode.OK,null,participant.Token));Check(results.Count>=2&&results.All(r=>r.TotalFinishers>=r.Position),"Personal result history and finisher totals");
 await Expect(HttpMethod.Put,"api/profile",HttpStatusCode.OK,new UpdateProfileRequest{FirstName="Updated",LastName="Participant",Gender=Gender.Other,PhoneNumber="0821234567"},runner.Token);
 await Expect(HttpMethod.Put,"api/profile",HttpStatusCode.BadRequest,new {firstName="X",lastName="Y",gender=99},runner.Token);
 await Expect(HttpMethod.Get,"api/admin/users",HttpStatusCode.Forbidden,null,runner.Token);
 await Expect(HttpMethod.Get,"api/admin/users",HttpStatusCode.OK,null,admin.Token);
 var png=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aDY0AAAAASUVORK5CYII=");
 async Task<HttpResponseMessage> Upload(string path,string token,byte[] data){
  using var req=new HttpRequestMessage(HttpMethod.Post,path);req.Headers.Authorization=new("Bearer",token);using var form=new MultipartFormDataContent();var bytes=new ByteArrayContent(data);bytes.Headers.ContentType=new("image/png");form.Add(bytes,"file","image.png");req.Content=form;return await api.SendAsync(req);
 }
 var banner=await Upload(eventPath+"/banner",organiser.Token,png);Check(banner.IsSuccessStatusCode,"API banner upload");
 var bannerEvent=await Read<EventDto>(banner);Check((await api.GetByteArrayAsync("api/files/"+bannerEvent.BannerImage)).SequenceEqual(png),"Banner bytes through API");
 Check((await Upload(eventPath+"/banner",other.Token,png)).StatusCode==HttpStatusCode.Forbidden,"Upload ownership");
 Check((await Upload("api/profile/picture",runner.Token,new byte[]{1,2,3})).StatusCode==HttpStatusCode.BadRequest,"Reject fake image signatures");
 Check((await Upload("api/profile/picture",runner.Token,new byte[5*1024*1024+1])).StatusCode==HttpStatusCode.BadRequest,"Reject oversized images");
 Check((await Upload("api/profile/picture",runner.Token,png)).IsSuccessStatusCode,"Profile image upload");
 Check((await api.GetAsync("api/files/other/not-an-image.txt")).StatusCode==HttpStatusCode.NotFound,"Reject unsupported file paths");
 var css=await browser.GetByteArrayAsync("css/site.css");Check(css.Length>5000,"Original theme stylesheet served");
 var anonymous=await browser.GetAsync("Organiser/Dashboard");Check(anonymous.StatusCode==HttpStatusCode.Redirect,"MVC protects organiser dashboard");
 async Task BrowserLogin(HttpClient client,string email,string password){
  var html=await client.GetStringAsync("Account/Login");var csrf=Token(html);Check(csrf.Length>0,"Login anti-forgery field");
  var response=await client.PostAsync("Account/Login",new FormUrlEncodedContent(new Dictionary<string,string>{{"Email",email},{"Password",password},{"__RequestVerificationToken",csrf}}));Check(response.StatusCode==HttpStatusCode.Redirect,"MVC API login "+email);
 }
 var badCsrf=await browser.PostAsync("Account/Login",new FormUrlEncodedContent(new Dictionary<string,string>{{"Email","participant@raceday.com"},{"Password","Participant@123"}}));Check(badCsrf.StatusCode==HttpStatusCode.BadRequest,"MVC rejects missing anti-forgery token");
 await BrowserLogin(browser,"organiser@raceday.com","Organiser@123");
 foreach(var page in new[]{"Organiser/Dashboard","Organiser/Events","Organiser/CreateEvent","Organiser/EditEvent/"+created.Id,"Organiser/Categories?eventId="+created.Id,"Organiser/Enrolments?eventId="+created.Id,"Organiser/Results?eventId="+past.Id,"Profile/Index"}){
  var response=await browser.GetAsync(page);Check(response.IsSuccessStatusCode,"MVC renders "+page);
 }
 var eventForm=await browser.GetStringAsync("Organiser/CreateEvent");
 var formResponse=await browser.PostAsync("Organiser/CreateEvent",new FormUrlEncodedContent(new Dictionary<string,string>{
  {"Name","MVC form test race"},{"Description","Created through the original organiser form"},{"Location","Cape Town"},{"Date",RaceClock.Now.AddDays(14).ToString("yyyy-MM-ddTHH:mm")},{"DistanceKm","10.5"},{"EventType","Road"},{"__RequestVerificationToken",Token(eventForm)}
 }));
 Check(formResponse.StatusCode==HttpStatusCode.Redirect,"MVC event creation form");
 var uiEventId=int.Parse(Regex.Match(formResponse.Headers.Location!.ToString(),"eventId=(\\d+)").Groups[1].Value);
 var categoryPage=await browser.GetStringAsync(formResponse.Headers.Location);
 var categoryForm=await browser.PostAsync("Organiser/SaveCategory",new FormUrlEncodedContent(new Dictionary<string,string>{
  {"Form.EventId",uiEventId.ToString()},{"Form.Id",""},{"Form.Name","10.5 km Open"},{"Form.DistanceKm","10.5"},{"__RequestVerificationToken",Token(categoryPage)}
 }));
 Check(categoryForm.StatusCode==HttpStatusCode.Redirect,"MVC category creation form");
 var uiCategories=await Read<List<CategoryDto>>(await Expect(HttpMethod.Get,$"api/events/{uiEventId}/categories",HttpStatusCode.OK));
 Check(uiCategories.Count==1&&uiCategories[0].DistanceKm==10.5m,"MVC category binds its nested fields and decimal distance");
 using var runnerBrowser=Browser();await BrowserLogin(runnerBrowser,runner.Email,"TestStrong!42");
 var detailsPage=await runnerBrowser.GetStringAsync("Events/Details/"+uiEventId);
 var entered=await runnerBrowser.PostAsync("Enrolments/Create",new FormUrlEncodedContent(new Dictionary<string,string>{
  {"eventId",uiEventId.ToString()},{"categoryId",uiCategories[0].Id.ToString()},{"__RequestVerificationToken",Token(detailsPage)}
 }));
 Check(entered.StatusCode==HttpStatusCode.Redirect&&entered.Headers.Location!.ToString().Contains("Enrolments"),"MVC participant enrolment form");
 var uiEntries=await Read<List<EnrolmentDto>>(await Expect(HttpMethod.Get,"api/enrolments/mine",HttpStatusCode.OK,null,runner.Token));
 Check(uiEntries.Any(e=>e.EventId==uiEventId),"MVC enrolment saved through API");
 var profileHtml=await runnerBrowser.GetStringAsync("Profile/Index");
 using(var pictureForm=new MultipartFormDataContent()){
  pictureForm.Add(new StringContent(Token(profileHtml)),"__RequestVerificationToken");
  var pictureBytes=new ByteArrayContent(png);pictureBytes.Headers.ContentType=new("image/png");pictureForm.Add(pictureBytes,"picture","profile.png");
  Check((await runnerBrowser.PostAsync("Profile/UploadPicture",pictureForm)).StatusCode==HttpStatusCode.Redirect,"MVC profile photo upload form");
 }
 var profile=await Read<ProfileDto>(await Expect(HttpMethod.Get,"api/profile",HttpStatusCode.OK,null,runner.Token));
 Check((await runnerBrowser.GetByteArrayAsync("img/"+profile.ProfileImage)).SequenceEqual(png),"MVC image proxy displays the uploaded profile");
 await Expect(HttpMethod.Delete,"api/events/"+uiEventId,HttpStatusCode.NoContent,null,organiser.Token);

 using var participantBrowser=Browser();await BrowserLogin(participantBrowser,"participant@raceday.com","Participant@123");
 foreach(var page in new[]{"Home/Index","Enrolments/Index","Results/Mine","Profile/Index","Events/Details/"+created.Id})Check((await participantBrowser.GetAsync(page)).IsSuccessStatusCode,"MVC renders "+page);
 var restricted=await participantBrowser.GetAsync("Organiser/Dashboard");Check(restricted.StatusCode==HttpStatusCode.Redirect,"MVC role restriction");
 var home=await participantBrowser.GetStringAsync("Home/Index");var logout=await participantBrowser.PostAsync("Account/Logout",new FormUrlEncodedContent(new Dictionary<string,string>{{"__RequestVerificationToken",Token(home)}}));Check(logout.StatusCode==HttpStatusCode.Redirect,"MVC sign out");
 Check((await participantBrowser.GetAsync("Enrolments/Index")).StatusCode==HttpStatusCode.Redirect,"MVC session cleared");
 using var adminBrowser=Browser();await BrowserLogin(adminBrowser,"admin@admin.com","Admin@123");
 foreach(var page in new[]{"Home/AdminDashboard","UserRoles/Index","UserRoles/Manage?userId="+runner.UserId})Check((await adminBrowser.GetAsync(page)).IsSuccessStatusCode,"MVC renders "+page);
 await Expect(HttpMethod.Post,"api/auth/logout",HttpStatusCode.NoContent,null,runner.Token);await Expect(HttpMethod.Get,"api/profile",HttpStatusCode.Unauthorized,null,runner.Token);
 await Expect(HttpMethod.Delete,eventPath,HttpStatusCode.NoContent,null,organiser.Token);await Expect(HttpMethod.Get,eventPath,HttpStatusCode.NotFound);
 Console.WriteLine($"PASS: {checks} integration checks.");
}catch(Exception e){Console.Error.WriteLine(e);lock(logs)foreach(var line in logs.TakeLast(80))Console.Error.WriteLine(line);Environment.ExitCode=1;}
finally{foreach(var process in processes){if(!process.HasExited)process.Kill(true);process.Dispose();}Console.WriteLine("Test data: "+scratch);}
void Check(bool ok,string description){if(!ok)throw new Exception("FAIL: "+description);checks++;Console.WriteLine("PASS: "+description);}
static string Token(string html)=>WebUtility.HtmlDecode(Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
static int FreePort(){var l=new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback,0);l.Start();var p=((System.Net.IPEndPoint)l.LocalEndpoint).Port;l.Stop();return p;}
static async Task Ready(HttpClient c){for(var i=0;i<100;i++){try{if((await c.GetAsync("health")).IsSuccessStatusCode)return;}catch(HttpRequestException){}await Task.Delay(200);}throw new Exception("Application failed to start.");}
