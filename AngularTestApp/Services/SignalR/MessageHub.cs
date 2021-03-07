using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Services.SignalR
{
    public class MessageHub : Hub
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        private readonly IHubContext<PresenceHub> presenceHub;
        private readonly PresenceTracker tracker;

        public MessageHub(IUnitOfWork unitOfWork, IMapper mapper, IHubContext<PresenceHub> presenceHub, PresenceTracker tracker)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
            this.presenceHub = presenceHub;
            this.tracker = tracker;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var otherUser = httpContext.Request.Query["user"].ToString();
            var groupName = GetGroupName(Context.User.GetUserName(), otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            var group = await AddToGroup(groupName);
            await Clients.Group(groupName).SendAsync("UpdatedGroup", group);
            var messages = await this.unitOfWork.MessageRepository.GetMessageThread(Context.User.GetUserName(), otherUser);

            if (this.unitOfWork.HasChanges()) await this.unitOfWork.Complete();
            
            await Clients.Caller.SendAsync("ReceiveMessageThread", messages);
        }

        public override async Task OnDisconnectedAsync(Exception ex)
        {
            var group = await RemoveFromMessageGroup();
            await Clients.Group(group.Name).SendAsync("UpdatedGroup", group);
            await base.OnDisconnectedAsync(ex);
        }

        public async Task SendMessage(CreateMessageDto createMessageDto)
        {
            var username = Context.User.GetUserName();

            if (username == createMessageDto.RecipientUsername.ToLower())
            {
                throw new HubException("You cannot send messages to yourself.");
            }

            var sender = await this.unitOfWork.UserRepository.GetUserByUsernameAsync(username);
            var recipient = await this.unitOfWork.UserRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername);

            if (recipient == null)
            {
                throw new HubException($"User Not Found: {username}");
            }

            var message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.UserName,
                RecipientUsername = recipient.UserName,
                Content = createMessageDto.Content
            };

            var groupName = GetGroupName(sender.UserName, recipient.UserName);
            var group = await this.unitOfWork.MessageRepository.GetMessageGroup(groupName);
            if(group.Connections.Any(x => x.Username == recipient.UserName))
            {
                message.DateRead = DateTime.UtcNow;
            }
            else
            {
                var connections = await this.tracker.GetConnectionsForUser(recipient.UserName);
                if(connections != null)
                {
                    await this.presenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived", new 
                    {
                        username = sender.UserName,
                        knownAs = sender.KnownAs
                    });
                }
            }

            this.unitOfWork.MessageRepository.AddMessage(message);

            if (await this.unitOfWork.Complete())
            {
                await Clients.Group(groupName).SendAsync("NewMessage", this.mapper.Map<MessageDto>(message));
            }
        }

        private async Task<Group> AddToGroup(string groupName)
        {
            var group = await this.unitOfWork.MessageRepository.GetMessageGroup(groupName);
            var connection = new Connection(Context.ConnectionId, Context.User.GetUserName());

            if(group == null)
            {
                group = new Group(groupName);
                this.unitOfWork.MessageRepository.AddGroup(group);
            }

            group.Connections.Add(connection);

            if (await this.unitOfWork.Complete()) return group;

            throw new HubException("Failed to join group.");
        }

        private async Task<Group> RemoveFromMessageGroup()
        {
            var group = await this.unitOfWork.MessageRepository.GetGroupForConnection(Context.ConnectionId);
            var connection = group.Connections.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            this.unitOfWork.MessageRepository.RemoveConnection(connection);
            if (await this.unitOfWork.Complete()) return group;

            throw new HubException("Failed to remove message Group");
        }

        private string GetGroupName(string caller, string other)
        {
            var stringCompare = string.CompareOrdinal(caller, other) < 0;
            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
        }
    }
}
