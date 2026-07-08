using FluentAssertions;
using Seven.Infrastructure.Security;

namespace Seven.Tests.Unit;

/// <summary>
/// BCrypt 密码哈希单元测试
/// </summary>
public class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ShouldReturnDifferentHashEachTime()
    {
        var hash1 = _hasher.HashPassword("123456");
        var hash2 = _hasher.HashPassword("123456");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        const string password = "Seven@2026";
        var hash = _hasher.HashPassword(password);

        _hasher.VerifyPassword(password, hash).Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ShouldReturnFalse()
    {
        var hash = _hasher.HashPassword("123456");

        _hasher.VerifyPassword("wrong", hash).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("VeryLongPassword_With_Special!@#$%^&*()")]
    public void HashAndVerify_ShouldWorkForVariousPasswords(string password)
    {
        var hash = _hasher.HashPassword(password);
        _hasher.VerifyPassword(password, hash).Should().BeTrue();
    }
}
