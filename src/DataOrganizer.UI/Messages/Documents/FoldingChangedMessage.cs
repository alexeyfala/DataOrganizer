using DataOrganizer.UI.Controls.Text;

namespace DataOrganizer.UI.Messages.Documents;

/// <summary>
/// Notification raised when blocks of an editor fold or unfold.
/// </summary>
/// <param name="Editor">Editor whose blocks folded or unfolded.</param>
internal sealed record FoldingChangedMessage(DocumentTextEditor Editor);
