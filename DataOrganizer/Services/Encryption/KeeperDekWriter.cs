using DataOrganizer.Interfaces.Encryption;
using Repository.Interfaces.Database;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Encryption;

/// <inheritdoc cref="IKeeperDekWriter" />
public sealed class KeeperDekWriter : IKeeperDekWriter
{
	#region Data
	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;
	#endregion

	#region Constructors
	public KeeperDekWriter(IDbAccess dbAccess) => _dbAccess = dbAccess;
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<bool> WriteAsync(
		IPasswordKeeper keeper,
		byte[] wrappedDek,
		CancellationToken token = default)
	{
		if (!await _dbAccess
			.UpdateEncryptedDekAsync(keeper.Id, wrappedDek, token)
			.ConfigureAwait(false))
		{
			return false;
		}

		keeper.EncryptedDek = wrappedDek;

		return true;
	}
	#endregion
}
