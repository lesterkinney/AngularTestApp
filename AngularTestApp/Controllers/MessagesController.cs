using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Authorization;
using API.Interfaces;
using API.DTOs;
using API.Extensions;
using AutoMapper;
using System.Collections.Generic;
using API.Helpers;

namespace API.Controllers
{
    [Authorize]
    public class MessagesController : BaseApiController
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;

        public MessagesController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }

        [HttpPost]
        public async Task<ActionResult<MessageDto>> CreateMessage(CreateMessageDto createMessageDto)
        {
            var username = User.GetUserName();

            if(username == createMessageDto.RecipientUsername.ToLower())
            {
                return BadRequest("You cannot message yourself!");
            }

            var sender = await this.unitOfWork.UserRepository.GetUserByUsernameAsync(username);
            var recipient = await this.unitOfWork.UserRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername);

            if(recipient == null)
            {
                return NotFound();
            }

            var message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.UserName,
                RecipientUsername = recipient.UserName,
                Content = createMessageDto.Content
            };

            this.unitOfWork.MessageRepository.AddMessage(message);

            if(await this.unitOfWork.Complete())
            {
                return Ok(this.mapper.Map<MessageDto>(message));
            }

            return BadRequest("Failed to send message.");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessagesForUser([FromQuery] MessageParams messageParams)
        {
            messageParams.Username = User.GetUserName();
            var messages = await this.unitOfWork.MessageRepository.GetMessagesForUser(messageParams);
            Response.AddPaginationHeader(messages.CurrentPage, messages.PageSize, messages.TotalCount, messages.TotalPages);

            return messages;
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage(int id)
        {
            var username = User.GetUserName();
            var message = await this.unitOfWork.MessageRepository.GetMessage(id);

            if(message.Sender.UserName != username && message.Recipient.UserName != username)
            {
                return Unauthorized();
            }

            if (message.Sender.UserName == username) message.SenderDeleted = true;
            if (message.Recipient.UserName == username) message.RecipientDeleted = true;
            if (message.SenderDeleted && message.RecipientDeleted) this.unitOfWork.MessageRepository.DeleteMessage(message);
            if (await this.unitOfWork.Complete()) return Ok();

            return BadRequest("Problem deleting the message.");
        }
    }
}
