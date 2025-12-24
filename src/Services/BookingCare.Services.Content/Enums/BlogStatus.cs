namespace BookingCare.Shared.Common.Enums;

/// <summary>
/// Represents the status of a blog post.
/// </summary>
public enum BlogStatus
{
    /// <summary>
    /// The blog post is in draft state and not yet published.
    /// </summary>
    DRAFT,

    /// <summary>
    /// The blog post is published and visible to users.
    /// </summary>
    PUBLISHED,

    /// <summary>
    /// The blog post is archived and no longer actively displayed.
    /// </summary>
    ARCHIVED
}