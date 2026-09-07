using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Identity.Common.Security;
using Identity.Domain.Entities;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Identity.Unit.Common.Security;

public class JwtTokenGeneratorTests
{
    private const string SecretKey = "UnitTestsOnlySecretKeyThatIsLongEnoughForHmacSha256";

    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly JwtTokenGenerator _generator;

    public JwtTokenGeneratorTests()
    {
        _configuration["Jwt:SecretKey"].Returns(SecretKey);
        _generator = new JwtTokenGenerator(_configuration);
    }

    private static readonly User SampleUser = new("person@example.com", "stored-hash");

    private static JwtSecurityToken Read(string token) => new JwtSecurityTokenHandler().ReadJwtToken(token);

    private static string[] RolesOf(JwtSecurityToken jwt) =>
        jwt.Claims.Where(c => c.Type == "role" || c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray();

    [Fact(DisplayName = "Without an audience the token carries no aud claim")]
    public void Given_NoAudience_When_Generating_Then_TokenHasNoAudience()
    {
        var jwt = Read(_generator.GenerateToken(SampleUser));

        jwt.Audiences.Should().BeEmpty();
    }

    [Fact(DisplayName = "The audience is emitted as the aud claim")]
    public void Given_Audience_When_Generating_Then_TokenCarriesIt()
    {
        var jwt = Read(_generator.GenerateToken(SampleUser, "sample-app"));

        jwt.Audiences.Should().ContainSingle().Which.Should().Be("sample-app");
    }

    [Fact(DisplayName = "A user with no roles gets a token with no role claim at all")]
    public void Given_NoRoles_When_Generating_Then_TokenHasNoRoleClaim()
    {
        var jwt = Read(_generator.GenerateToken(SampleUser, "sample-app", Array.Empty<string>()));

        RolesOf(jwt).Should().BeEmpty();
    }

    [Fact(DisplayName = "Each role becomes its own role claim")]
    public void Given_Roles_When_Generating_Then_TokenCarriesOneClaimEach()
    {
        var jwt = Read(_generator.GenerateToken(SampleUser, "sample-app", new[] { "Admin", "Customer" }));

        RolesOf(jwt).Should().BeEquivalentTo("Admin", "Customer");
    }

    [Fact(DisplayName = "The token always carries the user's id and email")]
    public void Given_AnyUser_When_Generating_Then_TokenCarriesIdentity()
    {
        var jwt = Read(_generator.GenerateToken(SampleUser, "sample-app", new[] { "Admin" }));

        jwt.Claims.Should().Contain(c => c.Value == SampleUser.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Value == SampleUser.Email);
    }

    [Fact(DisplayName = "The user's id is emitted as the standard sub claim, never as nameid")]
    public void Given_AnyUser_When_Generating_Then_IdIsInTheSubClaim()
    {
        // Guards the wire format itself: consumers that decode the payload by hand
        // (front-ends, non-.NET services) look for "sub". Asserting only on the value
        // would let the claim silently go back to being named "nameid".
        var jwt = Read(_generator.GenerateToken(SampleUser, "sample-app"));

        jwt.Claims.Should().ContainSingle(c => c.Type == JwtRegisteredClaimNames.Sub)
            .Which.Value.Should().Be(SampleUser.Id.ToString());

        jwt.Claims.Should().NotContain(c => c.Type == "nameid");
    }

    [Fact(DisplayName = "A missing signing key is rejected rather than producing an unsigned token")]
    public void Given_NoSecretKey_When_Generating_Then_Throws()
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration["Jwt:SecretKey"].Returns((string?)null);

        var act = () => new JwtTokenGenerator(configuration).GenerateToken(SampleUser);

        act.Should().Throw<ArgumentException>();
    }
}
