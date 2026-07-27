using AutoMapper;
using Identity.Application.Users.CreateUser;
using Identity.WebApi.Common;
using Identity.WebApi.Features.Users.CreateUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.WebApi.Features.Users;

/// <summary>
/// User account endpoints. Business rules live in the use-case handlers; this
/// controller only translates between the HTTP and application layers.
/// </summary>
[ApiController]
[Route("api")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public UsersController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    /// <summary>Registers a new user. Request validation runs via the MediatR pipeline.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponseWithData<CreateUserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var command = _mapper.Map<CreateUserCommand>(request);
        var result = await _mediator.Send(command, cancellationToken);

        return Created(string.Empty, new ApiResponseWithData<CreateUserResponse>
        {
            Success = true,
            Message = "User registered successfully",
            Data = _mapper.Map<CreateUserResponse>(result)
        });
    }
}
