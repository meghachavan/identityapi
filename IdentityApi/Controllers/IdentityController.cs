using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using IdentityApi.Helpers;
using IdentityApi.Infrastructure;
using IdentityApi.Models;
using IdentityApi.Models.ViewModels;
using Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace IdentityApi.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class IdentityController : ControllerBase
    {
        private IdentityDBContext db;
        private IConfiguration config;
        public IdentityController(IdentityDBContext dbContext, IConfiguration configuration)
        {
            db = dbContext;
            config = configuration;
        }
        [HttpPost("register",Name ="RegisterUser")]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<dynamic>> Register(Users user)
        {
            TryValidateModel(user);
            if(ModelState.IsValid)
            {
                user.Status = "Not Verified";
                await db.Users.AddAsync(user);
                await db.SaveChangesAsync();
                await SendVerificationMailAsync(user);
                return Created("", new
                {
                    user.Id, user.FullName, user.UserName, user.Email
                });
            }
            else
            {
                return BadRequest(ModelState);
            }
        }

        [HttpPost("token", Name ="GetToken")]
        public ActionResult<dynamic> GetToken(LoginModel model)
        {
            TryValidateModel(model);
            if(ModelState.IsValid)
            {
                var user = db.Users.SingleOrDefault(s => s.UserName == model.UserName && s.Password == model.Password && s.Status == "Verified");
                if(user!=null)
                {
                    //return token
                    var token = GenerateToken(user);
                    return Ok(new { user.FullName, user.Email, user.UserName, user.Role, Token=token});
                }
                else
                {
                    return Unauthorized();
                }
            }
            else
            {
                return BadRequest();
            }
        }

        [NonAction]
        private string GenerateToken(Users user)
        {
            var claims=new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub,user.FullName),
                new Claim(JwtRegisteredClaimNames.Email,user.Email),
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
            };
            claims.Add(new Claim(JwtRegisteredClaimNames.Aud, "catalogapi"));
            claims.Add(new Claim(JwtRegisteredClaimNames.Aud, "paymentapi"));
            claims.Add(new Claim(JwtRegisteredClaimNames.Aud, "basketapi"));
            claims.Add(new Claim(JwtRegisteredClaimNames.Aud, "orderapi"));

            //if (user.UserName == "meghachavan")
            {
                claims.Add(new Claim(ClaimTypes.Role, user.Role));
            }
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.GetValue<string>("Jwt:secret")));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: config.GetValue<string>("jwt:issuer"),
                audience:null,// config.GetValue<string>("jwt:audience"),
                claims: claims,
                expires: DateTime.Now.AddMilliseconds(30),
                signingCredentials: credentials
                );
            string tokenstring = new JwtSecurityTokenHandler().WriteToken(token);
            return tokenstring;                
        }

        [NonAction]
        private async Task SendVerificationMailAsync(Users user)
        {
            var userObj = new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.UserName
            };
            var messageText = JsonConvert.SerializeObject(userObj);
            StorageAccountHelper storageHelper = new StorageAccountHelper();
            storageHelper.StorageConnectionString = config.GetConnectionString("StorageConnection");
            await storageHelper.SendMessageAsync(messageText, "users");
        }
    }
}