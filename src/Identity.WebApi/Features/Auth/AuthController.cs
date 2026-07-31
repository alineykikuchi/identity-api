using AutoMapper;
using Identity.Application.Auth.Login;
using Identity.Application.Auth.Logout;
using Identity.Application.Auth.RefreshToken;
using Identity.WebApi.Common;
using Identity.WebApi.Features.Auth.Login;
using Identity.WebApi.Features.Auth.Logout;
using Identity.WebApi.Features.Auth.RefreshToken;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.WebApi.Features.Auth;

/// <summary>
/// Authentication endpoints. Business rules live in the use-case handlers; this
/// controller only translates between the HTTP and application layers.
/// </summary>
[ApiController]
[Route("api")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public AuthController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    /// <summary>
    /// Authenticates a user and returns an access token plus a refresh token.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponseWithData<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var command = _mapper.Map<LoginCommand>(request);
        var result = await _mediator.Send(command, cancellationToken);

        return Ok(new ApiResponseWithData<LoginResponse>
        {
            Success = true,
            Message = "Login successful",
            Data = _mapper.Map<LoginResponse>(result)
        });
    }

    /// <summary>
    /// Exchanges a valid refresh token for a new access + refresh token pair (rotation).
    /// The credential is the refresh token itself, so the endpoint is anonymous.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponseWithData<RefreshTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var command = _mapper.Map<RefreshTokenCommand>(request);
        var result = await _mediator.Send(command, cancellationToken);

        return Ok(new ApiResponseWithData<RefreshTokenResponse>
        {
            Success = true,
            Message = "Token refreshed successfully",
            Data = _mapper.Map<RefreshTokenResponse>(result)
        });
    }

    /// <summary>
    /// Terminates a session by revoking the given refresh token.
    /// </summary>
    /// <remarks>
    /// The operation is idempotent: an unknown, already revoked or expired token returns
    /// 204 just the same, so logout never fails for the client. Only a malformed body is
    /// rejected with 400.
    ///
    /// The access token currently held by the client stays valid until it expires
    /// (at most 15 minutes) — the expected behaviour of stateless JWTs, which are not
    /// checked against the database on every request. What logout stops is the ability to
    /// obtain new access tokens through <c>POST /api/refresh</c>.
    /// </remarks>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        var command = _mapper.Map<LogoutCommand>(request);
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}
