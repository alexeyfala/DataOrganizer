using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Extensions.Editor;
using DataOrganizer.Helpers.Editor;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Hierarchy;
using DataOrganizer.Interfaces.Seeding;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Seeding;

/// <inheritdoc cref="ISeedingScenarios" />
public sealed class SeedingScenarios : ISeedingScenarios
{
	#region Data
	/// <inheritdoc cref="IClipboardLogSeeder" />
	private readonly IClipboardLogSeeder _clipboardLogSeeder;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="ISampleSeeder" />
	private readonly ISampleSeeder _sampleSeeder;
	#endregion

	#region Constructors
	public SeedingScenarios(
		IClipboardLogSeeder clipboardLogSeeder,
		ILogger logger,
		IMessenger messenger,
		ISampleSeeder sampleSeeder)
	{
		_clipboardLogSeeder = clipboardLogSeeder;

		_logger = logger;

		_messenger = messenger;

		_sampleSeeder = sampleSeeder;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task SeedClipboardHistoryAsync()
	{
		_logger.LogInformation("Seed clipboard history");

		try
		{
			await _clipboardLogSeeder
				.SeedAsync()
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException("The sample clipboard entries could not be added.", ex);
		}
	}

	/// <inheritdoc />
	public Task SeedLargeSamplesAsync(Collection<ExplorerItemDtoBase> hierarchy)
	{
		return SeedAsync("Seed large samples", _sampleSeeder.SeedLargeAsync, hierarchy);
	}

	/// <inheritdoc />
	public Task SeedSamplesAsync(Collection<ExplorerItemDtoBase> hierarchy)
	{
		return SeedAsync("Seed samples", _sampleSeeder.SeedAsync, hierarchy);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Adds to the hierarchy the run of samples that <paramref name="seed" /> writes to the database, with the progress bar
	/// on.
	/// </summary>
	private async Task SeedAsync(
		string logMessage,
		Func<CancellationToken, Task<FolderDto>> seed,
		Collection<ExplorerItemDtoBase> hierarchy)
	{
		_logger.LogInformation(logMessage);

		try
		{
			using ProgressScope _ = _messenger.ShowProgress();

			FolderDto run = await seed(CancellationToken.None).ConfigureAwait(true);

			hierarchy.Add(run);
		}
		catch (Exception ex)
		{
			_logger.LogException("The sample objects could not be added.", ex);
		}
	}
	#endregion
}
