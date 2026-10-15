namespace StageTix.Tests.TestHelpers;

/// <summary>A clock that always returns the same moment, so "in the past / in the future" checks are deterministic.</summary>
public sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
{
    public static readonly DateTime DefaultNow = new(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public FixedTimeProvider() : this(DefaultNow)
    {
    }

    public DateTime UtcNow { get; } = utcNow;

    public override DateTimeOffset GetUtcNow() => new(UtcNow);
}
