using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Interceptors;

/// <summary>
/// Applies <see cref="SqlitePragmas" /> to every connection the context opens, including pooled ones.
/// </summary>
public sealed class SqlitePragmaInterceptor : DbConnectionInterceptor
{
	#region Methods
	/// <inheritdoc />
	public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
	{
		SqlitePragmas.Apply(connection);

		base.ConnectionOpened(connection, eventData);
	}

	/// <inheritdoc />
	public override async Task ConnectionOpenedAsync(
		DbConnection connection,
		ConnectionEndEventData eventData,
		CancellationToken cancellationToken = default)
	{
		await SqlitePragmas
			.ApplyAsync(connection, cancellationToken)
			.ConfigureAwait(false);

		await base
			.ConnectionOpenedAsync(connection, eventData, cancellationToken)
			.ConfigureAwait(false);
	}
	#endregion
}
