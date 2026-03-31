using System.Security.Claims;
using Application.DTOS;
using Application.Interface;
using Microsoft.AspNetCore.Mvc;

namespace GH2_Main.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }


        [HttpGet("{id}")]
        public async Task<ActionResult> GetUser(int id)
        {
            try
            {
                var user = await _userService.GetByIdAsync(id);
                return Ok(user);
            }
            catch (Exception ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        [HttpPost("Register")]
        public async Task<ActionResult> CreateUser([FromBody] CreateUserDto userDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { Message = "Invalid input data.", Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

                var createdUser = await _userService.CreateAsync(userDto);
                return CreatedAtAction(nameof(GetUser), new { id = createdUser.UserId }, createdUser);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }


        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var (accessToken, refreshToken) = await _userService.LoginAsync(loginDto);

                var accessCookieOption = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/",
                    MaxAge = TimeSpan.FromHours(1)
                };
                Response.Cookies.Append("access_token", accessToken, accessCookieOption);

                var refreshCookieOption = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/",
                    Expires = DateTime.UtcNow.AddDays(7)
                };
                Response.Cookies.Append("refresh_token", refreshToken, refreshCookieOption);

                return Ok(new { access_token = accessToken, refresh_token = refreshToken, message = "Login successful" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var refreshToken = Request.Cookies["refresh_token"];
                if (string.IsNullOrEmpty(refreshToken))
                    throw new ArgumentNullException(nameof(refreshToken));

                await _userService.LogoutAsync(refreshToken);

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/",
                    Expires = DateTime.UtcNow.AddDays(-1)
                };

                Response.Cookies.Delete("access_token", cookieOptions);
                Response.Cookies.Delete("refresh_token", cookieOptions);

                return Ok(new { Message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }


        //[HttpGet("me")]
        //public async Task<IActionResult> GetCurrentUser()
        //{
        //    try
        //    {
        //        var identity = HttpContext.User.Identity as ClaimsIdentity;

        //        var user = await _userService.GetCurrentUserAsync(identity);
        //        return Ok(user);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Unauthorized(new { Message = ex.Message });
        //    }
        //}
    }
}
