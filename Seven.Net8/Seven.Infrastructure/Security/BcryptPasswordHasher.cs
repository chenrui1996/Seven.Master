using Seven.Application.Interfaces;

namespace Seven.Infrastructure.Security;

/// <summary>
/// BCrypt 密码哈希实现，workFactor=12 提供足够的计算成本抵御暴力破解。
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    /// <inheritdoc />
    public string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    /// <inheritdoc />
    public bool VerifyPassword(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
