﻿using System.Threading.Tasks;
using dotnet_chat.dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using PusherServer;
using WebApplication1.Data;
using WebApplication1.dtos;

namespace dotnet_chat.Controllers
{
    [Route("api")]
    [ApiController]
    public class ChatController : Controller
    {
        //Chaching temp
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;
        public ChatController(ApplicationDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }
        //Chaching temp


        //Creating user
        [HttpPost("create")]
        public async Task<ActionResult> Create(User dto)
        {
            // Check for valid nickname and password
            if (string.IsNullOrEmpty(dto.NickName) || string.IsNullOrEmpty(dto.Passwd))
            {
                return BadRequest("Nickname and Password are required.");
            }

            // Check if the user with the same nickname already exists
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.NickName == dto.NickName);
            if (existingUser != null)
            {
                return BadRequest("User already exists.");
            }

            // Find the highest existing ID
            int newId = 1; // Default ID if no users exist
            if (await _context.Users.AnyAsync())
            {
                newId = (int)(await _context.Users.MaxAsync(u => u.Id) + 1);
            }

            // Create a new User entity with the next ID
            var newUser = new User
            {
                Id = newId,
                NickName = dto.NickName,
                Passwd = dto.Passwd
            };

            // Add the user to the database
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Cache the user data (serialize it to JSON)
            var userJson = JsonConvert.SerializeObject(newUser);
            await _cache.SetStringAsync($"User_{newUser.Id}", userJson);

            // Return the response with cached user
            return Ok(new
            {
                Message = "User successfully created",
                User = newUser
            });
        }


        //Messages sender + API
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
                    message = dto.Message
                });

            return Ok(new string[] { });
        }


        //Delete user
        [HttpDelete("delete/{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            // found by id
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound("User does not exist.");
            }


            // User delete
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "User successfully deleted",
                DeletedUser = user
            });
        }


        //Search for name
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

        //Update user
        [HttpPut("update")]
        public async Task<ActionResult> UpdateUser(User user)
        {
            var userupdated = await _context.Users.FindAsync(user.Id);
            if (userupdated == null)
            {
                return NotFound("User not found");
            }

            userupdated.NickName = user.NickName;
            userupdated.Passwd = user.Passwd;
            await _context.SaveChangesAsync();
            return Ok(userupdated);

        }


        //Search for name adn passwd
        [HttpPost("getall")]
        public async Task<ActionResult> GetAll([FromBody] User dto)
        {
            
            if (string.IsNullOrEmpty(dto.NickName) || string.IsNullOrEmpty(dto.Passwd))
            {
                return BadRequest("Password and Nickname required");
            }

            // search for user with this name
            var existingUser = await _context.Users 
                                              .FirstOrDefaultAsync(u => u.NickName == dto.NickName);

            if (existingUser == null)
            {
                return BadRequest("User not found");
            }

           
            if (existingUser.Passwd != dto.Passwd)
            {
                return BadRequest("Password is incorrect");
            }

           
            return Ok(existingUser);
        }

    }
}