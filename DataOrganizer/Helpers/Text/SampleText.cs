namespace DataOrganizer.Helpers.Text;

/// <summary>
/// Placeholder text used to fill objects created for demonstration.
/// </summary>
internal static class SampleText
{
	#region Properties
	/// <summary>
	/// A paragraph of placeholder text, the same from run to run.
	/// </summary>
	public static string LoremIpsum { get; } = SampleFaker
		.Create()
		.Lorem
		.Paragraph();
	#endregion
}
