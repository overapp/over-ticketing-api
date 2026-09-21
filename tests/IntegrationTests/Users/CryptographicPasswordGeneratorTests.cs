using Infrastructure.Authentication;
using Shouldly;

namespace IntegrationTests.Users;

public sealed class CryptographicPasswordGeneratorTests
{
    private readonly CryptographicPasswordGenerator _generator = new();

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(16)]
    [InlineData(32)]
    public void Generate_Should_ProducePasswordWithRequestedLength(int length)
    {
        string password = _generator.Generate(length);

        password.Length.ShouldBe(length);
    }

    [Fact]
    public void Generate_Should_MeetIdentityComplexityRequirements()
    {
        for (int i = 0; i < 50; i++)
        {
            string password = _generator.Generate(16);

            password.Any(char.IsUpper).ShouldBeTrue();
            password.Any(char.IsLower).ShouldBeTrue();
            password.Any(char.IsDigit).ShouldBeTrue();
            password.Any(ch => !char.IsLetterOrDigit(ch)).ShouldBeTrue();
        }
    }
}
