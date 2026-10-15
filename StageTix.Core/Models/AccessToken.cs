namespace StageTix.Core.Models;

/// <summary>A signed JWT and the moment it stops being valid (UTC).</summary>
public sealed record AccessToken(string Token, DateTime ExpiresAt);
