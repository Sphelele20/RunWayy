using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RunWay.Contracts;
namespace RunWay.Web;
public class AccountController(ApiClient api):Controller {
 [HttpGet] public IActionResult Login()=>View(new LoginRequest());
 [HttpPost] public async Task<IActionResult> Login(LoginRequest input){
  if(!ModelState.IsValid)return View(input);
  try{var auth=(await api.Post<AuthResponse>("api/auth/login",input))!;await SignIn(auth);return Destination(auth.User.Role);}
  catch(ApiException e){ModelState.AddModelError("",e.Message);return View(input);}
 }
 [HttpGet] public IActionResult Register()=>View(new RegisterRequest());
 [HttpPost] public async Task<IActionResult> Register(RegisterRequest input){
  if(!ModelState.IsValid)return View(input);
  try{var auth=(await api.Post<AuthResponse>("api/auth/register",input))!;await SignIn(auth);return Destination(auth.User.Role);}
  catch(ApiException e){ModelState.AddModelError("",e.Message);return View(input);}
 }
 private IActionResult Destination(string role)=>role=="Organiser"?RedirectToAction("Dashboard","Organiser"):RedirectToAction("Index","Events");
 private async Task SignIn(AuthResponse auth){
  HttpContext.Session.Clear();HttpContext.Session.SetString("ApiToken",auth.Token);HttpContext.Session.SetString("Role",auth.User.Role);
  var identity=new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,auth.User.Id),new Claim(ClaimTypes.Name,auth.User.FullName),new Claim(ClaimTypes.Role,auth.User.Role)},CookieAuthenticationDefaults.AuthenticationScheme);
  await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(identity),new AuthenticationProperties{ExpiresUtc=new DateTimeOffset(DateTime.SpecifyKind(auth.ExpiresAt,DateTimeKind.Utc))});
 }
 [HttpPost,Authorize] public async Task<IActionResult> Logout(){
  try{await api.Post<object>("api/auth/logout",null);}catch(ApiException){}catch(HttpRequestException){}catch(TaskCanceledException){}
  HttpContext.Session.Clear();await HttpContext.SignOutAsync();return RedirectToAction("Index","Events");
 }
 public IActionResult Denied(){Response.StatusCode=403;return View();}
 [HttpGet,Authorize] public async Task<IActionResult> Profile(){var u=await api.Get<ProfileDto>("api/profile");ViewBag.Profile=u;return View(new ProfileRequest{FirstName=u.FirstName,LastName=u.LastName,Gender=u.Gender,PhoneNumber=u.PhoneNumber});}
 [HttpPost,Authorize] public async Task<IActionResult> Profile(ProfileRequest input,IFormFile? image){
  if(ModelState.IsValid){
   try{await api.Put<ProfileDto>("api/profile",input);if(image!=null)await api.Upload("api/profile/image",image);TempData["Success"]="Your profile has been updated.";return RedirectToAction(nameof(Profile));}
   catch(ApiException e){ModelState.AddModelError("",e.Message);}
  }
  ViewBag.Profile=await api.Get<ProfileDto>("api/profile");return View(input);
 }
}
public class EventsController(ApiClient api):Controller {
 public async Task<IActionResult> Index(string? search,string? type){
  ViewBag.Search=search;ViewBag.Type=type;
  return View(await api.Get<List<EventDto>>("api/events?search="+Uri.EscapeDataString(search??"")+"&type="+Uri.EscapeDataString(type??"")));
 }
 public async Task<IActionResult> Details(int id)=>View(await api.Get<EventDto>("api/events/"+id));
 [HttpPost,Authorize(Roles="Participant")] public async Task<IActionResult> Enrol(int id,int categoryId){
  try{await api.Post<object>($"api/events/{id}/enrolments",new EnrolRequest(categoryId));TempData["Success"]="Entry confirmed. You are on the start list!";return RedirectToAction("Entries","Participant");}
  catch(ApiException e){TempData["Error"]=e.Message;return RedirectToAction(nameof(Details),new{id});}
 }
}
[Authorize(Roles="Participant")]
public class ParticipantController(ApiClient api):Controller {
 public async Task<IActionResult> Entries()=>View(await api.Get<List<EnrolmentDto>>("api/me/enrolments"));
 public async Task<IActionResult> Results()=>View((await api.Get<List<EnrolmentDto>>("api/me/enrolments")).Where(n=>n.FinishTime!=null).ToList());
}
[Authorize(Roles="Organiser")]
public class OrganiserController(ApiClient api):Controller {
 public async Task<IActionResult> Dashboard()=>View(await api.Get<List<EventDto>>("api/events?mine=true&upcoming=false"));
 [HttpGet] public IActionResult Create()=>View("Edit",new EventRequest());
 [HttpGet] public async Task<IActionResult> Edit(int id)=>View(await api.Get<EventDto>($"api/events/{id}"));
 [HttpPost] public async Task<IActionResult> Save(EventRequest input,IFormFile? banner){
  if(!ModelState.IsValid)return View("Edit",input);
  try{
   var e=input.Id==0?await api.Post<EventDto>("api/events",input):await api.Put<EventDto>($"api/events/{input.Id}",input);
   input.Id=e!.Id;
   if(banner!=null)await api.Upload($"api/events/{e.Id}/banner",banner);
   TempData["Success"]="Event saved.";return RedirectToAction(nameof(Manage),new{id=e.Id});
  }catch(ApiException e){ModelState.AddModelError("",e.Message);return View("Edit",input);}
 }
 public async Task<IActionResult> Manage(int id){
  ViewBag.Enrolments=await api.Get<List<EnrolmentDto>>($"api/events/{id}/enrolments");
  return View(await api.Get<EventDto>($"api/events/{id}"));
 }
 [HttpPost] public async Task<IActionResult> Delete(int id){await api.Delete($"api/events/{id}");TempData["Success"]="Event deleted.";return RedirectToAction(nameof(Dashboard));}
 [HttpPost] public async Task<IActionResult> AddCategory(int id,CategoryRequest input){
  if(ModelState.IsValid){try{await api.Post<CategoryDto>($"api/events/{id}/categories",input);TempData["Success"]="Category added.";}catch(ApiException e){TempData["Error"]=e.Message;}}
  else TempData["Error"]="Enter a category name (up to 100 characters) and description (up to 255).";
  return RedirectToAction(nameof(Manage),new{id});
 }
 [HttpPost] public async Task<IActionResult> RemoveCategory(int id,int categoryId){try{await api.Delete($"api/events/{id}/categories/{categoryId}");}catch(ApiException e){TempData["Error"]=e.Message;}return RedirectToAction(nameof(Manage),new{id});}
 [HttpPost] public async Task<IActionResult> PublishResult(int id,int enrolmentId,ResultRequest input){
  if(ModelState.IsValid){try{await api.Put<object>($"api/events/{id}/enrolments/{enrolmentId}/result",input);TempData["Success"]="Result published.";}catch(ApiException e){TempData["Error"]=e.Message;}}
  else TempData["Error"]="Enter a positive finishing position and a time in HH:mm:ss format.";
  return RedirectToAction(nameof(Manage),new{id});
 }
}
public class MediaController(ApiClient api):Controller {
 [HttpGet("/Media/{name}")] public async Task<IActionResult> Image(string name){try{return File(await api.Media(name),name.EndsWith(".png")?"image/png":"image/jpeg");}catch(ApiException){return NotFound();}}
}
public class HomeController:Controller { public IActionResult Error()=>View("Error"); }
