using Bogus;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Enums.Clipboard;
using DataOrganizer.Helpers;
using DataOrganizer.Helpers.Clipboard;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Messages.Clipboard;
using DataOrganizer.Models.Clipboard;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Clipboard;

public sealed class ClipboardLogSeeder : IClipboardLogSeeder
{
	#region Data
	/// <inheritdoc cref="IClipboardLogMerger" />
	private readonly IClipboardLogMerger _clipboardLog;

	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;
	#endregion

	#region Constructors
	public ClipboardLogSeeder(
		IClipboardLogMerger clipboardLog,
		IDispatcherAccessor dispatcher,
		IMessenger messenger)
	{
		_clipboardLog = clipboardLog;

		_dispatcher = dispatcher;

		_messenger = messenger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task SeedAsync()
	{
		// The images are drawn and encoded, so the work leaves the caller's thread.
		await Task
			.CompletedTask
			.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

		Faker faker = SampleFaker.Create();

		// The hashes are made as for a captured copy, so the history tells the samples apart in the same way.
		ClipboardLogEntryBase[] entries =
		[
			.. SampleClipboardContents
				.CreateTexts(faker)
				.Select(static x => ClipboardLogEntryFactory.CreateText(
					x.Text,
					x.Html,
					x.Rtf,
					ClipboardHistory.ComputeTextEntryHash(x.Text, x.Html, x.Rtf))),
			.. SampleClipboardContents
				.CreateImages()
				.Select(static x => ClipboardLogEntryFactory.CreateImage(x, SHA256.HashData(x))),
			.. SampleClipboardContents
				.CreateFileLists(faker)
				.Select(static x => ClipboardLogEntryFactory.CreateFiles(x, ClipboardHistory.HashFiles(x)))
		];

		// The entries of the history belong to the UI thread.
		await _dispatcher
			.PostAsync(() => _clipboardLog.Merge(entries))
			.ConfigureAwait(false);

		// Merge sends no message, so the history is saved and its new number of entries announced here.
		_messenger.Send(new ClipboardLogChangedMessage(ClipboardLogChangeKind.Updated));

		_messenger.Send(new ClipboardLogEntryCountChangedMessage());
	}
	#endregion
}
