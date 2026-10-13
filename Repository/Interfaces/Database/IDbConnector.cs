using Repository.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Interfaces.Database;

/// <summary>
/// Connects to the database and tells whether it accepts changes.
/// </summary>
public interface IDbConnector
{
	#region Properties
	/// <summary>
	/// The outcome of the last <see cref="ConnectAsync" />.
	/// </summary>
	DbConnectionStatus ConnectionStatus { get; }

	/// <summary>
	/// <c>True</c> while the database accepts changes.
	/// </summary>
	bool IsWritable { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Establishes a connection to the database, creating or migrating it as needed.
	/// Tells apart a file that cannot be opened and a schema that does not match this version;
	/// housekeeping failures are logged and do not affect the result.
	/// </summary>
	Task<DbConnectionStatus> ConnectAsync(CancellationToken token = default);

	/// <inheritdoc cref="IDbContextService.GetDbFilePath" />
	string GetDbFilePath();
	#endregion
}
