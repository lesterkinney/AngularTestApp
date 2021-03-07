using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Authorize]
    public class AppUsersController : BaseApiController
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        private readonly IPhotoService photoService;

        public AppUsersController(IUnitOfWork unitOfWork, IMapper mapper, IPhotoService photoService)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
            this.photoService = photoService;
        }

        // GET: api/AppUsers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers([FromQuery] UserParams userParams)
        {
            var gender = await this.unitOfWork.UserRepository.GetUserGender(User.GetUserName());
            userParams.CurrentUsername = User.GetUserName();

            if(string.IsNullOrEmpty(userParams.Gender))
            {
                userParams.Gender = gender == "male" ? "female" : "male";
            }

            var users = await this.unitOfWork.UserRepository.GetMembersAsync(userParams);
            var usersToReturn = this.mapper.Map<IEnumerable<MemberDto>>(users);
            Response.AddPaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);

            return Ok(usersToReturn);
        }

        // GET: api/AppUsers/username
        [HttpGet("{username}", Name = "GetUser")]
        public async Task<ActionResult<AppUser>> GetAppUserByName( string username)
        {
            var user = await this.unitOfWork.UserRepository.GetMemberAsync(username);
            var userToReturn = this.mapper.Map<MemberDto>(user);
            return Ok(userToReturn);
        }

        //PUT: api/AppUsers/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut]
        public async Task<ActionResult> UpdateAppUser(MemberUpdateDto memberToUpdate)
        {
            var user = await this.unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUserName());
            this.mapper.Map(memberToUpdate, user);
            this.unitOfWork.UserRepository.Update(user);
            if (await this.unitOfWork.Complete()) return NoContent();
            {
                return BadRequest("Failed to update user");
            }
        }

        //POST: api/AppUsers
        //To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
        {
            var user = await this.unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUserName());
            var result = await this.photoService.AddPhotoAsync(file);
            if(result.Error != null)
            {
                return BadRequest(result.Error.Message);
            }

            var photo = new Photo
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };

            if(user.Photos.Count == 0)
            {
                photo.IsMain = true;
            }

            user.Photos.Add(photo);

            if(await this.unitOfWork.Complete())
            {
                return CreatedAtRoute("GetUser",  new { username = user.UserName}, this.mapper.Map<Photo, PhotoDto>(photo));
            }

            return BadRequest("Problem adding photo.");
        }

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {
            var user = await this.unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUserName());
            var photo = user.Photos.FirstOrDefault(p => p.Id == photoId);
            if (photo.IsMain) return BadRequest("Photo is already main photo.");

            var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);
            if(currentMain != null)
            {
                currentMain.IsMain = false;
            }

            photo.IsMain = true;

            await this.unitOfWork.Complete();

            return NoContent();
        }


        //DELETE: api/AppUsers/5
        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId)
        {

            var user = await this.unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUserName());
            var photo = user.Photos.FirstOrDefault(p => p.Id == photoId);

            if (photo == null) return NotFound();
            if (photo.IsMain) return BadRequest("Cannot delete 'Main' photo.");
            if(photo.PublicId != null)
            {
                var result = await this.photoService.DeletePhotoAsync(photo.PublicId);
                if (result.Error != null) return BadRequest(result.Error.Message);
            }

            user.Photos.Remove(photo);
            if (await this.unitOfWork.Complete()) return Ok();

            return BadRequest("Failed to delete the photo.");
        }
    }
}
