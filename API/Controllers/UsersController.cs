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
public class UsersController(IUnitOfWork unitOfWork,
    IPhotoService photoService,
    IMapper mapper) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers([FromQuery] UserParams userParams)
    {
        userParams.CurrentUsername = User.GetUserName();

        var users = await unitOfWork.UserRepository.GetMembersAsync(userParams);

        Response.AddPaginationHeader(users);

        return Ok(users);
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<MemberDto>> GetUser(string username)
    {
        var user = await unitOfWork.UserRepository.GetMemberAsync(username);

        if (user is null) return NotFound();

        return Ok(user);
    }

    [HttpPut]
    public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
    {
        var user = await unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUserName());

        if (user is null) return NotFound("Could not find user.");

        mapper.Map(memberUpdateDto, user);

        if (await unitOfWork.CompleteAsync()) return NoContent();

        return BadRequest("Failed to update user.");
    }

    [HttpPost("add-photo")]
    public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
    {
        var user = await unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUserName());

        if (user is null) return NotFound("Cannot update user.");

        var result = await photoService.AddPhotoAsync(file);

        if (result.Error is not null) return BadRequest(result.Error.Message);

        var photo = new Photo
        {
            Url = result.SecureUrl.AbsoluteUri,
            PublicId = result.PublicId
        };

        if (user.Photos.Count == 0) photo.IsMain = true;

        user.Photos.Add(photo);

        if (await unitOfWork.CompleteAsync())
        {
            return CreatedAtAction(nameof(GetUser),
                new { username = user.UserName },
                        mapper.Map<PhotoDto>(photo));
        }

        return BadRequest("Problem adding photo.");
    }

    [HttpPut("set-main-photo/{photoId:int}")]
    public async Task<ActionResult> SetMainPhoto(int photoId)
    {
        var user = await unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUserName());

        if (user is null) return NotFound("Could not find user.");

        var photo = user.Photos.FirstOrDefault(p => p.Id == photoId);

        if (photo is null || photo.IsMain) return BadRequest("Cannot use this as main photo.");

        var currentMainPhoto = user.Photos.FirstOrDefault(p => p.IsMain);

        if (currentMainPhoto is not null) currentMainPhoto.IsMain = false;

        photo.IsMain = true;

        if (await unitOfWork.CompleteAsync()) return NoContent();

        return BadRequest("Problem setting main photo.");
    }

    [HttpDelete("delete-photo/{photoId:int}")]
    public async Task<ActionResult> DeletePhoto(int photoId)
    {
        var user = await unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUserName());

        if (user is null) return NotFound("User not found.");

        var photo = user.Photos.FirstOrDefault(p => p.Id == photoId);

        if (photo is null || photo.IsMain) return BadRequest("This photo cannot be deleted.");

        if (photo.PublicId is not null)
        {
            var result = await photoService.DeletePhotoAsync(photo.PublicId);

            if (result.Error is not null) return BadRequest(result.Error.Message);
        }

        user.Photos.Remove(photo);

        if (await unitOfWork.CompleteAsync()) return Ok();

        return BadRequest("Problem deleting photo.");
    }

}
