using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.Data;
using DatingApp.Dtos;
using DatingApp.Helpers;
using DatingApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DatingApp.Controllers
{
    [Authorize]
    [Route("api/users/{userId}/photos")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private readonly IDatingRepository _datingRepository;
        private readonly IMapper _mapper;
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private Cloudinary _cloundinay;

        public PhotosController(IDatingRepository datingRepository, IMapper mapper, IOptions<CloudinarySettings> cloudinaryConfig)
        {
            _datingRepository = datingRepository;
            _mapper = mapper;
            _cloudinaryConfig = cloudinaryConfig;

            Account acc = new Account(
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );

            _cloundinay = new Cloudinary(acc);
        }

        [HttpGet("{photoId}", Name = "GetPhoto")]
        public async Task<IActionResult> GetPhoto(int photoId)
        {
            var photoFromRepo = await _datingRepository.GetPhoto(photoId);

            var photo = _mapper.Map<PhotoForReturnDto>(photoFromRepo);

            return Ok(photo);
        }

        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(int userId, [FromForm]PhotoForCreationDto photoForCreationDto)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }

            var userFromRepo = await _datingRepository.GetUser(userId);

            var file = photoForCreationDto.File;

            var uploadResult = new ImageUploadResult();

            if (file.Length > 0)
            {
                using (var stream = file.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.Name, stream),
                        Transformation = new Transformation().Width(500).Height(500).Crop("fill").Gravity("face")
                    };

                    uploadResult = _cloundinay.Upload(uploadParams);
                }
            }

            photoForCreationDto.Url = uploadResult.Uri.ToString();
            photoForCreationDto.PublicId = uploadResult.PublicId;

            var photo = _mapper.Map<Photo>(photoForCreationDto);

            if (!userFromRepo.photos.Any(u => u.IsMain))
                photo.IsMain = true;

            userFromRepo.photos.Add(photo);

            if (await _datingRepository.SaveAll())
            {
                var photoToReturn = _mapper.Map<PhotoForReturnDto>(photo);

                return CreatedAtRoute("GetPhoto", new { photoId = photo.Id }, photoToReturn);
            }

            return BadRequest("Could not add photo");
        }

        [HttpPost("{photoId}/setMain")]
        public async Task<IActionResult> setMain(int userId, int photoId)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }

            var user = await _datingRepository.GetUser(userId);

            if (!user.photos.Any(p => p.Id == photoId))
            {
                return Unauthorized();
            }

            var photoFromRepo = await _datingRepository.GetPhoto(photoId);

            if (photoFromRepo.IsMain)
                return BadRequest("This is already the main photo");

            var currentMainPhoto = await _datingRepository.GetMainPhotoForUser(userId);

            currentMainPhoto.IsMain = false;

            photoFromRepo.IsMain = true;

            if (await _datingRepository.SaveAll())
            {
                return NoContent();
            }

            return BadRequest("Could not set photo to main");
        }

        [HttpDelete("{photoId}")]
        public async Task<IActionResult> DeletePhoto(int userId, int photoId)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }

            var user = await _datingRepository.GetUser(userId);

            if (!user.photos.Any(p => p.Id == photoId))
            {
                return Unauthorized();
            }

            var photoFromRepo = await _datingRepository.GetPhoto(photoId);

            if (photoFromRepo.IsMain)
                return BadRequest("You cannot delete your main photo");

            if (photoFromRepo.PublicId != null)
            {
                var deleteParamse = new DeletionParams(photoFromRepo.PublicId);
                var result = _cloundinay.Destroy(deleteParamse);

                if (result.Result == "ok")
                {
                    _datingRepository.Delete(photoFromRepo);
                }
            }

            if (photoFromRepo.PublicId == null) 
            {
                _datingRepository.Delete(photoFromRepo);
            }

            if (await _datingRepository.SaveAll())
            {
                return Ok();
            }

            return BadRequest("Failed to delete the photo");
        }
    }
}