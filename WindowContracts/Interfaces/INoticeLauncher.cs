using System.Threading.Tasks;

namespace WindowContracts.Interfaces;

/// <summary>
/// Shows notices in windows of their own, which need no other window.
/// </summary>
public interface INoticeLauncher
{
	#region Methods
	/// <summary>
	/// Reports a database that cannot be worked with and waits until the report is closed.
	/// </summary>
	Task ShowStartupErrorAsync(string databaseFilePath);
	#endregion
}
