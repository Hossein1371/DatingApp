using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.Data;
using DatingApp.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;
        public UsersController(IDatingRepository repo, IMapper mapper)
        {
            _mapper = mapper;
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _repo.GetUsers();
            var usersToReturn = _mapper.Map<IEnumerable<UserForListDto>>(users);
            return Ok(usersToReturn);
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUser(int userId)
        {
            var user = await _repo.GetUser(userId);
            var userToReturn = _mapper.Map<UserForDetailedDto>(user);
            return Ok(userToReturn);
        }

        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateUser(int userId, UserForUpdateDto userForUpdateDto) 
        {
           if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)) {
               return Unauthorized();
           }     

           var userFromRepo = await _repo.GetUser(userId);
           _mapper.Map(userForUpdateDto, userFromRepo);

           if(await _repo.SaveAll()) {
               return NoContent();
           }

           throw new Exception($"Updatin user {userId} failed on save");
        }
    }
}