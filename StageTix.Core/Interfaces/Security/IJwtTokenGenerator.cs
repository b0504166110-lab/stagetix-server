using StageTix.Core.Entities;
using StageTix.Core.Models;

namespace StageTix.Core.Interfaces.Security;

/// <summary>Creates a signed, time-limited JWT with the user's id and role as claims.</summary>
public interface IJwtTokenGenerator
{
    AccessToken Generate(User user);
}
