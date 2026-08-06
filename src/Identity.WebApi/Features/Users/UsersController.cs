using AutoMapper;
using Identity.Application.Users.ChangePassword;
using Identity.Application.Users.CreateUser;
using Identity.Application.Users.GetMe;
using Identity.Application.Users.UpdateUser;
using Identity.WebApi.Common;
using Identity.WebApi.Features.Users.ChangePassword;
using Identity.WebApi.Features.Users.CreateUser;
using Identity.WebApi.Features.Users.GetMe;
using Identity.WebApi.Features.Users.UpdateUser;
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

    /// <summary>Updates the profile of the authenticated user.</summary>
    /// <remarks>
    /// Only the display name can be changed for now: changing the email requires a
    /// verification flow that does not exist yet.
    /// </remarks>
    [HttpPut("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponseWithData<UpdateUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var command = _mapper.Map<UpdateUserCommand>(request);
        command.UserId = GetCurrentUserId();

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(new ApiResponseWithData<UpdateUserResponse>
        {
            Success = true,
            Message = "User updated successfully",
            Data = _mapper.Map<UpdateUserResponse>(result)
        });
    }

    /// <summary>Changes the password of the authenticated user.</summary>
    /// <remarks>
    /// Every refresh token of the user is revoked, which signs the account out of other
    /// devices — the point of changing a password that may have leaked. The current client
    /// is signed out as well and has to log in again. Access tokens already issued stay
    /// valid until they expire (at most 15 minutes), the expected behaviour of stateless JWTs.
    /// </remarks>
    [HttpPut("me/password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var command = _mapper.Map<ChangePasswordCommand>(request);
        command.UserId = GetCurrentUserId();

        await _mediator.Send(command, cancellationToken);

        return Ok(new ApiResponse
        {
            Success = true,
            Message = "Password changed successfully. Please sign in again."
        });
    }
}
