using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RunWay.Contracts;
namespace RunWay.Api;
public class TokenHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,ILoggerFactory logger,UrlEncoder encoder,RaceDb db)
 : AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder) {
 public static string Hash(string token)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
 protected override async Task<AuthenticateResult> HandleAuthenticateAsync(){
  var header=Request.Headers.Authorization.ToString();
  if(!header.StartsWith("Bearer ",StringComparison.OrdinalIgnoreCase)) return AuthenticateResult.NoResult();
  var hash=Hash(header[7..]);
  var token=await db.Tokens.Include(t=>t.User).SingleOrDefaultAsync(t=>t.Hash==hash && t.ExpiresAt>DateTime.UtcNow);
  if(token is null) return AuthenticateResult.Fail("Session expired.");
  var claims=new[]{new Claim(ClaimTypes.NameIdentifier,token.UserId),new Claim(ClaimTypes.Name,token.User.Email!),new Claim(ClaimTypes.Role,token.User.Role)};
  return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims,Scheme.Name)),Scheme.Name));
 }
}
[ApiController,Route("api/auth")]
public class AuthController(UserManager<ApplicationUser> users,RaceDb db) : ControllerBase {
 [HttpPost("register"),AllowAnonymous]
 public async Task<IActionResult> Register(RegisterRequest request){
  var user=new ApplicationUser{UserName=request.Email,Email=request.Email,FirstName=request.FirstName.Trim(),LastName=request.LastName.Trim(),Gender=request.Gender,Role=request.Role};
  var result=await users.CreateAsync(user,request.Password);
  if(!result.Succeeded) return BadRequest(new{message=string.Join(" ",result.Errors.Select(e=>e.Description))});
  return Ok(await Issue(user));
 }
 [HttpPost("login"),AllowAnonymous]
 public async Task<IActionResult> Login(LoginRequest request){
  var user=await users.FindByEmailAsync(request.Email);
  if(user is null || await users.IsLockedOutAsync(user)) return Unauthorized(new{message="Invalid credentials or account temporarily locked."});
  if(!await users.CheckPasswordAsync(user,request.Password)){
   await users.AccessFailedAsync(user);
   return Unauthorized(new{message="Invalid credentials or account temporarily locked."});
  }
  await users.ResetAccessFailedCountAsync(user);
  return Ok(await Issue(user));
 }
 private async Task<AuthResponse> Issue(ApplicationUser user){
  var token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));var expires=DateTime.UtcNow.AddHours(2);
  db.Tokens.RemoveRange(await db.Tokens.Where(t=>t.UserId==user.Id && t.ExpiresAt<DateTime.UtcNow).ToListAsync());
  db.Tokens.Add(new(){Hash=TokenHandler.Hash(token),UserId=user.Id,ExpiresAt=expires});
  await db.SaveChangesAsync();return new(token,expires,user.Dto());
 }
 [HttpPost("logout"),Authorize]
 public async Task<IActionResult> Logout(){
  var hash=TokenHandler.Hash(Request.Headers.Authorization.ToString()[7..]);
  await db.Tokens.Where(t=>t.Hash==hash).ExecuteDeleteAsync();return NoContent();
 }
}
[ApiController,Route("api/profile"),Authorize]
public class ProfileController(UserManager<ApplicationUser> users,ImageStore images) : ControllerBase {
 private async Task<ApplicationUser> Current()=> (await users.GetUserAsync(User))!;
 [HttpGet] public async Task<ProfileDto> Get()=> (await Current()).Dto();
 [HttpPut] public async Task<IActionResult> Update(ProfileRequest input){
  var u=await Current();u.FirstName=input.FirstName.Trim();u.LastName=input.LastName.Trim();u.Gender=input.Gender;u.PhoneNumber=input.PhoneNumber;
  var result=await users.UpdateAsync(u);return result.Succeeded?Ok(u.Dto()):BadRequest(new{message="Unable to update your profile."});
 }
 [HttpPost("image"),RequestSizeLimit(3*1024*1024)]
 public async Task<IActionResult> Image(IFormFile file){
  var u=await Current();u.ImageUrl=await images.Save(file);
  var result=await users.UpdateAsync(u);return result.Succeeded?Ok(u.Dto()):BadRequest(new{message="Unable to save the image."});
 }
}
