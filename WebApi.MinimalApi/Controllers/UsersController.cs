using AutoMapper;
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
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);

        if (user == null)
        {
            return NotFound();
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
}