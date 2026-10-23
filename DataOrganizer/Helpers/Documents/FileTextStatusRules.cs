using DataOrganizer.Enums.Documents;
using System;

namespace DataOrganizer.Helpers.Documents;

/// <summary>
/// One reading of the status of the text of a file for every place that uses the text, each with reactions of its own.
/// </summary>
internal static class FileTextStatusRules
{
	#region Methods
	/// <summary>
	/// <c>True</c> when the status holds a text; otherwise runs the reaction to the status. Closed contents need none:
	/// they were told of where the key left them closed.
	/// </summary>
	public static bool HoldsText(
		FileTextStatus status,
		Action onDamaged,
		Action onNotText,
		Action onEmpty)
	{
		switch (status)
		{
			case FileTextStatus.Damaged:
				onDamaged();

				return false;

			case FileTextStatus.NotText:
				onNotText();

				return false;

			case FileTextStatus.Empty:
				onEmpty();

				return false;

			case FileTextStatus.Closed:
				return false;

			default:
				return true;
		}
	}
	#endregion
}
