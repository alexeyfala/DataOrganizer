using DataOrganizer.Documents.Editor.Interfaces;

namespace DataOrganizer.UI.Services.Documents;

/// <inheritdoc cref="IDocumentTextFactory" />
public sealed class DocumentTextFactory : IDocumentTextFactory
{
	#region Methods
	/// <inheritdoc />
	public IDocumentText Create() => new AvaloniaDocumentText();
	#endregion
}
