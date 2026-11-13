using DataOrganizer.Controls.Text;

namespace DataOrganizer.Messages.Documents;

/// <summary>
/// Notification raised when blocks of an editor fold or unfold.
/// </summary>
internal sealed record FoldingChangedMessage(DocumentTextEditor Editor);
