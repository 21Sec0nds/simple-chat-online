using System.Threading.Tasks;
using dotnet_chat.dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using PusherServer;
using WebApplication1.Data;
using WebApplication1.dtos;
using WebApplication1.hash;

namespace dotnet_chat.Controllers
{
    [Route("api")]
    [ApiController]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly TokenGeneration _tokenService;

        // Only one constructor to accept all dependencies 
        public ChatController(ApplicationDbContext context, IDistributedCache cache, TokenGeneration tokenService)
        {
            _context = context;
            _cache = cache;
            _tokenService = tokenService;
        }
        //=============================================================================CREATEUSER=============================================================================
        [HttpPost("create")]
        public async Task<ActionResult> Create(User dto)
        {
            if (string.IsNullOrEmpty(dto.NickName) || string.IsNullOrEmpty(dto.Passwd))
            {
                return BadRequest("Nickname and Password are required.");
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.NickName == dto.NickName);
            if (existingUser != null)
            {
                return BadRequest("User already exists.");
            }

            int newId = 1;
            if (await _context.Users.AnyAsync())
            {
                newId = (int)(await _context.Users.MaxAsync(u => u.Id) + 1);
            }

            var passwordHasher = new PasswordHasher<User>();
            var newUser = new User
            {
                Id = newId,
                NickName = dto.NickName,
                Passwd = passwordHasher.HashPassword(null, dto.Passwd),
                avatarUrl = dto.avatarUrl
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();


            var token = _tokenService.GenerateToken(newUser.Id.GetValueOrDefault());


            return Ok(new
            {
                Message = "User successfully created",
                User = newUser,
                Token = token
            });
        }
        //=============================================================================PUSHERAPI=============================================================================
        [HttpPost("messages")]
        public async Task<ActionResult> Message(MessageDTO dto)
        {
            var options = new PusherOptions
            {
                Cluster = "eu",
                Encrypted = true
            };

            var pusher = new Pusher(
                "1897639",
                "de0d85dfc195bed6c21c",
                "ffb71c2ffea8dd12c287",
                options);

            await pusher.TriggerAsync(
                "chat",
                "message",
                new
                {
                    username = dto.Username,
                    message = dto.Message,
                    avatar = dto.Avatar
                });

            return Ok(new string[] { });
        }
        //=============================================================================DELETUSER=============================================================================
        [HttpDelete("delete/{id}")]
        public async Task<ActionResult> Delete(int id, [FromHeader(Name = "Authorization")] string token, [FromBody] string password)
        {
            try
            {
             
                var userIdFromToken = _tokenService.DecodeToken(token.Replace("Bearer", ""));

                if (userIdFromToken != id)
                {
                    return Unauthorized("You can only delete your own account");
                }

                var user = await _context.Users.FindAsync(id);
                if (user == null) return NotFound("User not found");

                var passwordHasher = new PasswordHasher<User>();
                var verificationResult = passwordHasher.VerifyHashedPassword(null, user.Passwd, password);

                if (verificationResult == PasswordVerificationResult.Failed)
                {
                    return Unauthorized("Password is incorrect");
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return Ok("User deleted successfully");
            }
            catch
            {
                return Unauthorized("Invalid token");
            }
        }
        //=============================================================================SEARCHFORNAME=============================================================================
        [HttpPost("getname")]
        public async Task<ActionResult> GetByNickname([FromBody] User dto)
        {
            // verify if the name is gaven 
            if (string.IsNullOrEmpty(dto.NickName))
            {
                return BadRequest("Nickname of user required.");
            }

            // search for name
            var existingUser = await _context.Users
                                              .FirstOrDefaultAsync(u => u.NickName == dto.NickName);

            if (existingUser == null)
            {
                return NotFound("User not found.");
            }

            // Return user 
            return Ok(existingUser);
        }
        //=============================================================================UPDATEUSER=============================================================================
        [HttpPut("update/{id}")]
        public async Task<ActionResult> UpdateUser(int id, [FromBody] User user)
        {
            // Find the user by id
            var userToUpdate = await _context.Users.FindAsync(id);
            if (userToUpdate == null)
            {
                return NotFound("User not found");
            }

            // Update the user properties
            userToUpdate.NickName = user.NickName;


            // Save changes
            await _context.SaveChangesAsync();

            return Ok(userToUpdate);
        }

        //=============================================================================GETALL=============================================================================
        //Search for name adn passwd
        [HttpPost("getall")]
        public async Task<ActionResult> GetAll([FromBody] User dto)
        {
            if (string.IsNullOrEmpty(dto.NickName) || string.IsNullOrEmpty(dto.Passwd))
            {
                return BadRequest("Password and Nickname required");
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.NickName == dto.NickName);
            if (existingUser == null)
            {
                return BadRequest("User not found");
            }

            var passwordHasher = new PasswordHasher<User>();
            var verificationResult = passwordHasher.VerifyHashedPassword(null, existingUser.Passwd, dto.Passwd);

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return BadRequest("Password is incorrect");
            }

            return Ok(existingUser);
        }
        //=============================================================================GETUSER=============================================================================
        [HttpGet("getuser/{id}")]
        public async Task<ActionResult> GetUser(int id)
        {
            var cacheKey = $"user_{id}";
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (cachedData != null)
            {
                return Ok(JsonConvert.DeserializeObject<User>(cachedData));
            }

            var existUser = await _context.Users.FindAsync(id);
            if (existUser == null) return NotFound("User not found");

            var serializedUser = JsonConvert.SerializeObject(existUser);
            await _cache.SetStringAsync(cacheKey, serializedUser, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });

            return Ok(existUser);
        }
        //=============================================================================GETIMG=============================================================================
        [HttpGet("getimg/{id}")]
        public async Task<ActionResult> GetImg(int id)
        {
            var existuser = await _context.Users.FindAsync(id);
            if (existuser == null) return NotFound("User not found");

            if (string.IsNullOrEmpty(existuser.avatarUrl))
            {
                return Ok("User has default avatar");
            }
            return Ok(existuser.avatarUrl);
        }
        //=============================================================================EDITIMG=============================================================================
        [HttpPut("editimg/{id}")]
        public async Task<ActionResult> EditImg(int id, [FromBody] User img)
        {
            var existuser = await _context.Users.FindAsync(id);
            if (existuser == null) return NotFound("User not found");

            existuser.avatarUrl = img.avatarUrl;

            await _context.SaveChangesAsync();

            return Ok("Avatar updated successfully");
        }
    }
}
