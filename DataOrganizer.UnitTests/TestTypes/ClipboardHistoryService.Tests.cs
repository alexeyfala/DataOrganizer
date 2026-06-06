using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Clipboard;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using DataOrganizer.Messages;
using DataOrganizer.Services;
using DataOrganizer.UnitTests.Helpers;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardHistoryService)}"" type")]
internal class ClipboardHistoryServiceTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="ClipboardHistoryService.ClearAsync" />: clears entries and raises ClearedByUser.
	/// </summary>
	[Test]
	public async Task ClearAsync_Raises_ClearedByUser()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = CreateMock(messenger);

		ClipboardHistoryService sut = mock.Create<ClipboardHistoryService>();

		sut.Entries.Add(TextEntry("a", [1]));

		List<ClipboardHistoryChangeKind> received = Capture(messenger);

		// Act
		await sut.ClearAsync();

		// Assert
		sut.Entries
			.Should()
			.BeEmpty();

		received
			.Should()
			.Equal(ClipboardHistoryChangeKind.ClearedByUser);
	}

	/// <summary>
	/// Test of <see cref="ClipboardHistoryService.ClearEntriesAsync" />: clears entries and raises ClearedForStop.
	/// </summary>
	[Test]
	public async Task ClearEntriesAsync_Raises_ClearedForStop()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = CreateMock(messenger);

		ClipboardHistoryService sut = mock.Create<ClipboardHistoryService>();

		sut.Entries.Add(TextEntry("a", [1]));

		List<ClipboardHistoryChangeKind> received = Capture(messenger);

		// Act
		await sut.ClearEntriesAsync();

		// Assert
		sut.Entries
			.Should()
			.BeEmpty();

		received
			.Should()
			.Equal(ClipboardHistoryChangeKind.ClearedForStop);
	}

	/// <summary>
	/// Test of <see cref="ClipboardHistoryService.Merge" />: appends below current, dedupes by hash, no message.
	/// </summary>
	[Test]
	public void Merge_Appends_Below_Current_Skipping_Duplicates()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = CreateMock(messenger);

		ClipboardHistoryService sut = mock.Create<ClipboardHistoryService>();

		sut.Entries.Add(TextEntry("C", [2]));

		sut.Entries.Add(TextEntry("D", [9]));

		List<ClipboardHistoryChangeKind> received = Capture(messenger);

		// Act ("B" duplicates current "C" by hash [2] and is skipped).
		sut.Merge([TextEntry("A", [1]), TextEntry("B", [2])]);

		// Assert
		sut.Entries
			.Cast<ClipboardTextEntry>()
			.Select(x => x.Text)
			.Should()
			.Equal("C", "D", "A");

		received
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// Test of <see cref="ClipboardHistoryService.Merge" />: the history cap is enforced.
	/// </summary>
	[Test]
	public void Merge_Enforces_History_Cap()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = CreateMock(messenger);

		ClipboardHistoryService sut = mock.Create<ClipboardHistoryService>();

		for (int i = 0; i < 25; i++)
		{
			sut.Entries.Add(TextEntry($"cur{i}", [(byte)i]));
		}

		// Act
		sut.Merge([TextEntry("overflow", [200])]);

		// Assert
		sut.Entries
			.Should()
			.HaveCount(25);

		sut.Entries
			.Cast<ClipboardTextEntry>()
			.Should()
			.NotContain(x => x.Text == "overflow");
	}

	/// <summary>
	/// Test of <see cref="ClipboardHistoryService.RestoreAsync" />: moves the entry to the top and raises Updated.
	/// </summary>
	[Test]
	public async Task RestoreAsync_Moves_To_Top_And_Raises_Updated()
	{
		// Arrange
		IMessenger messenger = new WeakReferenceMessenger();

		using AutoMock mock = CreateMock(messenger);

		ClipboardHistoryService sut = mock.Create<ClipboardHistoryService>();

		ClipboardTextEntry target = TextEntry("target", [2]);

		sut.Entries.Add(TextEntry("other", [1]));

		sut.Entries.Add(target);

		List<ClipboardHistoryChangeKind> received = Capture(messenger);

		// Act
		await sut.RestoreAsync(target);

		// Assert
		sut.Entries[0]
			.Should()
			.Be(target);

		received
			.Should()
			.Contain(ClipboardHistoryChangeKind.Updated);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Records the kinds of every <see cref="ClipboardHistoryChangedMessage" /> sent on <paramref name="messenger" />.
	/// </summary>
	private static List<ClipboardHistoryChangeKind> Capture(IMessenger messenger)
	{
		List<ClipboardHistoryChangeKind> received = [];

		messenger.Register<ClipboardHistoryChangedMessage>(
			received,
			static (recipient, message) => ((List<ClipboardHistoryChangeKind>)recipient).Add(message.Kind));

		return received;
	}

	/// <summary>
	/// Builds an auto-mock container with a synchronous dispatcher and the supplied messenger.
	/// </summary>
	private static AutoMock CreateMock(IMessenger messenger)
	{
		return AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new InlineDispatcherAccessor())
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(Substitute.For<IClipboardAccessor>());

			builder.RegisterInstance(Substitute.For<IStorageAccessor>());

			builder.RegisterInstance(messenger);
		});
	}

	/// <summary>
	/// A minimal text entry with the given hash.
	/// </summary>
	private static ClipboardTextEntry TextEntry(string text, byte[] hash) => new()
	{
		Text = text,
		Html = null,
		Rtf = null,
		Hash = hash
	};
	#endregion
}
