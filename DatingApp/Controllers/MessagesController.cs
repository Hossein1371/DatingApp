using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.Data;
using DatingApp.Dtos;
using DatingApp.Helpers;
using DatingApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.Controllers {
    [ServiceFilter (typeof (LogUserActivity))]
    [Authorize]
    [ApiController]
    [Route ("api/users/{userId}/[controller]")]
    public class MessagesController : ControllerBase {
        private readonly IMapper _mapper;
        private readonly IDatingRepository _repo;
        public MessagesController (IDatingRepository repo, IMapper mapper) {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet ("{messageId}", Name = "GetMessage")]
        public async Task<IActionResult> GetMessage (int userId, int messageId) {
            if (userId != int.Parse (User.FindFirst (ClaimTypes.NameIdentifier).Value))
                return Unauthorized ();

            var messageFromRepo = await _repo.GetMessage (messageId);

            if (messageFromRepo == null)
                return NotFound ();

            return Ok (messageFromRepo);
        }

        [HttpGet]
        public async Task<IActionResult> GetMessagesFroUser (int userId, [FromQuery] MessageParams messageParams) {
            if (userId != int.Parse (User.FindFirst (ClaimTypes.NameIdentifier).Value))
                return Unauthorized ();

            messageParams.UserId = userId;

            var messagesFromRepo = await _repo.GetMessagesForUser (messageParams);

            var messages = _mapper.Map<IEnumerable<MessageToReturnDto>> (messagesFromRepo);

            Response.AddPagination (messagesFromRepo.CurrentPage, messagesFromRepo.PageSize,
                messagesFromRepo.TotalCount, messagesFromRepo.TotalCount);

            return Ok (messages);
        }

        [HttpGet ("thread/{recipientId}")]
        public async Task<IActionResult> GetMessageThread (int userId, int recipientId) {
            if (userId != int.Parse (User.FindFirst (ClaimTypes.NameIdentifier).Value))
                return Unauthorized ();

            var messageFromRepo = await _repo.GetMessageThread (userId, recipientId);

            var messageThread = _mapper.Map<IEnumerable<MessageToReturnDto>> (messageFromRepo);

            return Ok (messageThread);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMessage (int userId, MessageForCreationDto messageForCreationDto) {
            var sender = await _repo.GetUser (userId);

            if (sender.Id != int.Parse (User.FindFirst (ClaimTypes.NameIdentifier).Value))
                return Unauthorized ();

            messageForCreationDto.SenderId = userId;

            var recipient = await _repo.GetUser (messageForCreationDto.RecipientId);

            if (recipient == null)
                return BadRequest ("Could not find user");

            var message = _mapper.Map<Message> (messageForCreationDto);

            _repo.Add (message);

            if (await _repo.SaveAll ()) {
                var messageToReturn = _mapper.Map<MessageToReturnDto> (message);
                return CreatedAtRoute ("GetMessage", new { messageId = message.Id }, messageToReturn);
            }

            throw new Exception ("Creating the message failed on save");
        }
    }
}