using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Clipboard;

/// <summary>
/// Adds entries made up for trying the clipboard history to it.
/// </summary>
public interface IClipboardLogSeeder
{
	#region Methods
	/// <summary>
	/// Adds the sample entries below the current ones while the history has room, skipping those it already holds.
	/// </summary>
	Task SeedAsync();
	#endregion
}
