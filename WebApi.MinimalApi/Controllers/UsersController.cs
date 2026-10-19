using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return NotFound();
        }
        
        var user = _userRepository.FindById(userId);

        if (user == null)
        {
            return NotFound();
        }
        
        if (HttpMethods.IsHead(Request.Method))
        {
            Response.ContentType = "application/json; charset=utf-8";
            return Ok();
        }

        return Ok(_mapper.Map<UserDto>(user));
    }

    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] UserToCreateDto user)
    {
        if (user == null)
        {
            return BadRequest();
        }
        if (user.Login != null && !user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("Login", "Логин должен состоять только из букв и цифр");
        }
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var userEntity = _mapper.Map<UserEntity>(user);
        var createdUserEntity = _userRepository.Insert(userEntity);
        
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }
    
    [HttpPut("{userId}")]
    [Consumes("application/json")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UserToUpdateDto user)
    {
        if (user == null || userId == Guid.Empty)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var userEntity = _mapper.Map(user, new UserEntity(userId));

        _userRepository.UpdateOrInsert(userEntity, out var inserted);

        if (inserted)
        {
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id);
        }

        return NoContent();
    }
    
    [HttpPatch("{userId}")]
    [Consumes("application/json-patch+json", "application/json")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser(
        [FromRoute] Guid userId,
        [FromBody] JsonPatchDocument<UserToUpdateDto> patchDoc)
    {
        if (patchDoc == null)
        {
            return BadRequest();
        }
        
        if (userId == Guid.Empty)
        {
            return NotFound();
        }

        var userEntity = _userRepository.FindById(userId);
        if (userEntity == null)
        {
            return NotFound();
        }

        var userToUpdateDto = _mapper.Map<UserToUpdateDto>(userEntity);
        patchDoc.ApplyTo(userToUpdateDto, ModelState);
        TryValidateModel(userToUpdateDto);

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        _mapper.Map(userToUpdateDto, userEntity);
        
        _userRepository.Update(userEntity);

        return NoContent();
    }
    
    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return NotFound();
        }

        var userEntity = _userRepository.FindById(userId);
        if (userEntity == null)
        {
            return NotFound();
        }

        _userRepository.Delete(userId);

        return NoContent();
    }
    
}