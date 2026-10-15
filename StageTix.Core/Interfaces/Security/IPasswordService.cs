namespace StageTix.Core.Interfaces.Security;

/// <summary>Hashes and verifies passwords. Plain passwords are never stored.</summary>
public interface IPasswordService
{
    string Hash(string password);

    bool Verify(string passwordHash, string password);
}
