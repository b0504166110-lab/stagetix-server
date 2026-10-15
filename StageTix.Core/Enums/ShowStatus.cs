namespace StageTix.Core.Enums;

/// <summary>The life cycle of a show.</summary>
public enum ShowStatus
{
    /// <summary>Being prepared by the admin. Not visible to customers.</summary>
    Draft,

    /// <summary>Published. Customers can see it and book seats.</summary>
    OnSale,

    /// <summary>Cancelled by the admin. All its orders are cancelled too.</summary>
    Cancelled
}
