namespace StageTix.Core.Enums;

/// <summary>The role of a user. Determines which endpoints the user may call.</summary>
public enum UserRole
{
    /// <summary>Browses shows and books seats.</summary>
    Customer,

    /// <summary>Hall manager: manages shows and all orders.</summary>
    Admin
}
