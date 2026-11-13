using Avalonia.Controls.Templates;

namespace DataOrganizer.Templates.Selectors;

/// <summary>
/// Template for the content of one kind.
/// </summary>
internal sealed class ContentKindTemplate
{
	#region Properties
	/// <summary>
	/// Kind of the content the template shows.
	/// </summary>
	public object? Kind { get; set; }

	/// <summary>
	/// Template that shows the content.
	/// </summary>
	public IDataTemplate? Template { get; set; }
	#endregion
}
