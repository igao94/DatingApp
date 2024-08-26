using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class MessagesController(IUnitOfWork unitOfWork, IMapper mapper) : BaseApiController
{
    [HttpPost]
    public async Task<ActionResult<MessageDto>> CreateMessage(CreateMessageDto createMessageDto)
    {
        var username = User.GetUserName();

        if (username == createMessageDto.RecipientUsername.ToLower())
            return BadRequest("You cannot message yourself.");

        var sender = await unitOfWork.UserRepository.GetUserByUsernameAsync(username);

        var recipient = await unitOfWork.UserRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername);

        if (recipient is null || sender is null || sender.UserName is null || recipient.UserName is null)
            return BadRequest("Cannot send message at this time.");

        var message = new Message
        {
            Sender = sender,
            Recipient = recipient,
            SenderUsername = sender.UserName,
            RecipientUsername = recipient.UserName,
            Content = createMessageDto.Content
        };

        unitOfWork.MessagesRepository.AddMessage(message);

        if (await unitOfWork.CompleteAsync()) return Ok(mapper.Map<MessageDto>(message));

        return BadRequest("Failed to save message.");
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessagesForUser(
        [FromQuery] MessageParams messageParams)
    {
        messageParams.Username = User.GetUserName();

        var messages = await unitOfWork.MessagesRepository.GetMessagesForUserAsync(messageParams);

        Response.AddPaginationHeader(messages);

        return Ok(messages);
    }

    [HttpGet("thread/{username}")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessageThreads(string username)
    {
        var currentUsername = User.GetUserName();

        return Ok(await unitOfWork.MessagesRepository.GetMessageThreadAsync(currentUsername, username));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteMessage(int id)
    {
        var username = User.GetUserName();

        var message = await unitOfWork.MessagesRepository.GetMessageAsync(id);

        if (message is null) return BadRequest("Cannot delete this message.");

        if (message.SenderUsername != username && message.RecipientUsername != null) return Forbid();

        if (message.SenderUsername == username) message.SenderDeleted = true;

        if (message.RecipientUsername == username) message.RecipientDeleted = true;

        if (message is { SenderDeleted: true, RecipientDeleted: true })
            unitOfWork.MessagesRepository.DeleteMessage(message);

        if (await unitOfWork.CompleteAsync()) return Ok();

        return BadRequest("Problem deleting message.");
    }
}
