using System.Text.Json.Serialization;

namespace DataOrganizer.DTO.Updates;

/// <summary>
/// Subset of a GitHub release entry returned by the repository releases API.
/// </summary>
internal sealed record GitHubRelease
{
	#region Properties
	/// <summary>
	/// Git tag associated with the release (for example, "v0.1.0").
	/// </summary>
	[JsonPropertyName("tag_name")]
	public string? TagName { get; init; }

	/// <summary>
	/// Web page of the release.
	/// </summary>
	[JsonPropertyName("html_url")]
	public string? HtmlUrl { get; init; }

	/// <summary>
	/// Indicates whether the release is an unpublished draft.
	/// </summary>
	[JsonPropertyName("draft")]
	public bool Draft { get; init; }

	/// <summary>
	/// Indicates whether the release is marked as a pre-release.
	/// </summary>
	[JsonPropertyName("prerelease")]
	public bool Prerelease { get; init; }
	#endregion
}
