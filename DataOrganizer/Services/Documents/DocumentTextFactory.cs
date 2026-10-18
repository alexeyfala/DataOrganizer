using DataOrganizer.Interfaces.Documents;

namespace DataOrganizer.Services.Documents;

public sealed class DocumentTextFactory : IDocumentTextFactory
{
	#region Methods
	/// <inheritdoc />
	public IDocumentText Create() => new AvaloniaDocumentText();
	#endregion
}
