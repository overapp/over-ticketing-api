using System.Security.Cryptography;
using Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace IntegrationTests.Auth;

public sealed class JwtOptionsValidatorTests
{
    private readonly JwtOptionsValidator _validator = new();

    [Fact]
    public void Validate_Should_Succeed_WhenAllValuesAreValid()
    {
        // Arrange
        JwtOptions options = CreateValidOptions();

        // Act
        ValidateOptionsResult result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Should_ReportEveryMissingValue_WhenSectionIsEmpty()
    {
        // Act
        ValidateOptionsResult result = _validator.Validate(null, new JwtOptions());

        // Assert
        result.Failed.ShouldBeTrue();
        result.Failures.ShouldBe(
            [
                "'Jwt:Issuer' is required.",
                "'Jwt:Audience' is required.",
                "'Jwt:ExpirationInMinutes' must be greater than 0.",
                "'Jwt:PrivateKey' is required.",
                "'Jwt:PublicKey' is required."
            ],
            ignoreOrder: true);
    }

    [Fact]
    public void Validate_Should_Fail_WhenPrivateKeyIsNotBase64()
    {
        // Arrange
        JwtOptions options = CreateValidOptions();
        options.PrivateKey = "not-base64!";

        // Act
        ValidateOptionsResult result = _validator.Validate(null, options);

        // Assert
        result.Failures.ShouldHaveSingleItem()
            .ShouldBe("'Jwt:PrivateKey' is not a valid base64-encoded PKCS#8 DER RSA key.");
    }

    [Fact]
    public void Validate_Should_Fail_WhenPublicKeyIsNotAnRsaKey()
    {
        // Arrange
        JwtOptions options = CreateValidOptions();
        options.PublicKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        // Act
        ValidateOptionsResult result = _validator.Validate(null, options);

        // Assert
        result.Failures.ShouldHaveSingleItem()
            .ShouldBe("'Jwt:PublicKey' is not a valid base64-encoded SubjectPublicKeyInfo DER RSA key.");
    }

    [Fact]
    public void Validate_Should_Fail_WhenPublicKeyBelongsToAnotherKeyPair()
    {
        // Arrange
        JwtOptions options = CreateValidOptions();
        options.PublicKey = CreateValidOptions().PublicKey;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, options);

        // Assert
        result.Failures.ShouldHaveSingleItem()
            .ShouldBe("'Jwt:PublicKey' does not match 'Jwt:PrivateKey'; tokens signed by this instance would fail validation.");
    }

    private static JwtOptions CreateValidOptions()
    {
        using var rsa = RSA.Create(2048);

        return new JwtOptions
        {
            PrivateKey = Convert.ToBase64String(rsa.ExportPkcs8PrivateKey()),
            PublicKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo()),
            Issuer = "issuer",
            Audience = "audience",
            ExpirationInMinutes = 60
        };
    }
}
