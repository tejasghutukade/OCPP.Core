using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using OCPP.Core.Database;
using OCPP.Core.WebAPI.Dtos;

namespace OCPP.Core.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    private readonly IMapper _mapper;
    private readonly ManagementApiClient _auth0ManagementApiClient;

    public UserController(ManagementApiClient auth0ManagementApiClient ,ILogger<UserController> logger,IMapper mapper)
    {
        _logger = logger;
        _mapper = mapper;
        _auth0ManagementApiClient = auth0ManagementApiClient;
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var response = await _auth0ManagementApiClient.Users.GetAsync(id);
        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Post(UserDto user)
    {
        try
        {
            var authUser = new UserCreateRequest
            {
                Email = user.Email,
                Password = user.Password,
                Connection = "Username-Password-Authentication",
            };
            var response = await _auth0ManagementApiClient.Users.CreateAsync(authUser);
            if (response != null)
            {
                return Created("~/api/User",new {id=response.UserId});
            }
            else
            {
                return BadRequest();
            }
        }catch(Exception ex)
        {
            _logger.LogError(ex, "Error while adding Creating User");
            return BadRequest();
        }
    }
}