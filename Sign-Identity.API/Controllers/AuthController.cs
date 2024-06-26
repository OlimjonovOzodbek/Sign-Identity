﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Sign_Identity.Application.Services.AuthServices;
using Sign_Identity.Domain.DTOs;
using Sign_Identity.Domain.Entities.Auth;

namespace Sign_Identity.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IAuthService _authService;

        public AuthController(SignInManager<User> signInManager, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IAuthService authService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _authService = authService;
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterDTO registerDTO)
        {
            if (!ModelState.IsValid)
            {
                throw new Exception("Validation error");
            }
            if (string.IsNullOrWhiteSpace(registerDTO.Email))
            {
                throw new Exception("Validation error");
            }
            if (string.IsNullOrWhiteSpace(registerDTO.Username))
            {
                throw new Exception("Validation error");
            }
            if (string.IsNullOrWhiteSpace(registerDTO.FirstName))
            {
               throw new Exception("Validation error");
            }
            if (string.IsNullOrWhiteSpace(registerDTO.LastName))
            {
                throw new Exception("Validation error");
            }

            var check = await _userManager.FindByEmailAsync(registerDTO.Email);

            if (check != null)
            {
                return BadRequest("You already registered");
            }

            var user = new User()
            {
                Email = registerDTO.Email,
                UserName = registerDTO.Username,
                FirstName = registerDTO.FirstName,
                LastName = registerDTO.LastName,
                Age = registerDTO.Age
            };

            var result = await _userManager.CreateAsync(user, registerDTO.Password);
            foreach(var role in registerDTO.Roles)
            {
                await _userManager.AddToRoleAsync(user, role);
            }
            if (!result.Succeeded)
            {
                return BadRequest("Something went wrong");
            }

            return Ok("Bluetooth device is ready to pend");
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDTO loginDTO)
        {
            if (!ModelState.IsValid)
            {
               throw new Exception("Something went wrong");
            }

            var user = await _userManager.FindByEmailAsync(loginDTO.Email);

            if (user is null)
                return NotFound("Email not found");

            //var result = await _signInManager.PasswordSignInAsync(user: user, password: loginDTO.Password, isPersistent: false, lockoutOnFailure: false);

           /* if (!result.Succeeded)
                return Unauthorized("Something went wrong in Authorization");*/

            var tokenDTO = await _authService.GenerateToken(user);


            if (tokenDTO.IsSuccess == false || tokenDTO.Token == "" || tokenDTO.Token is null)
            {
                throw new Exception("Something went wrong!!");
            }

            HttpContext.Response.Cookies.Append("accessToken", tokenDTO.Token);

            return Ok(tokenDTO);

        }

        [HttpGet]
        [Authorize(Roles = "Admin, Teacher")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                return Ok(await _userManager.Users.ToListAsync());
            }
            catch
            {
                return NotFound("Users are not found");
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin, Teacher")]
        public async Task<IActionResult> GetUserById(string id)
        {
            try
            {
                var result = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == id);
                if (result is null)
                {
                    return NotFound("User is not found");
                }
                return Ok(result);
            }
            catch
            {
                return NotFound("User is not found");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin, Teacher, Student")]
        public async Task<IActionResult> LogOut()
        {
            try
            {
                await _signInManager.SignOutAsync();

                HttpContext.Response.Cookies.Delete("accessToken");

                return Ok("Loged Out");
            }
            catch(Exception ex) 
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
