using AutoMapper;
using Identity.Application.Users.CreateUser;
using Identity.Application.Users.GetMe;
using Identity.WebApi.Common;
using Identity.WebApi.Features.Users.CreateUser;
using Identity.WebApi.Features.Users.GetMe;
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
public class UsersController : BaseController
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

    /// <summary>Returns the profile of the authenticated user.</summary>
    /// <remarks>
    /// The access token is validated by the JWT middleware, so a missing, malformed or
    /// expired token is rejected before this action runs. The user is then loaded from the
    /// database, which stays the source of truth: an account deactivated after the token
    /// was issued is rejected as well.
    /// </remarks>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponseWithData<GetMeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var query = new GetMeQuery { UserId = GetCurrentUserId() };
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(new ApiResponseWithData<GetMeResponse>
        {
            Success = true,
            Message = "User retrieved successfully",
            Data = _mapper.Map<GetMeResponse>(result)
        });
    }
}
