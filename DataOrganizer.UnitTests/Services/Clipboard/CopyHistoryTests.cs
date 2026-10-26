using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Messages.Clipboard;
using DataOrganizer.Services.Clipboard;
using Hierarchy.Dto;
using Hierarchy.UnitTests.Factories;
using NSubstitute;
using System;
using System.Linq;

namespace DataOrganizer.UnitTests.Services.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(CopyHistory)}"" type")]
internal class CopyHistoryTests
{
	#region Methods
	/// <summary>
	/// <see cref="CopyHistory.Add" />: a new copied file goes to the top of the history.
	/// </summary>
	[Test]
	public void Add_Inserts_A_New_File_At_The_Top()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(2)];

		using AutoMock mock = AutoMock.GetLoose();

		CopyHistory sut = mock.Create<CopyHistory>();

		sut.Add(files[0], showAtOnce: false);

		// Act
		sut.Add(files[1], showAtOnce: false);

		// Assert
		sut.ItemIds
			.Should()
			.Equal(files[1].Id, files[0].Id);
	}

	/// <summary>
	/// <see cref="CopyHistory.Add" />: a file already in the history moves to its top without a second entry.
	/// </summary>
	[Test]
	public void Add_Moves_A_Listed_File_To_The_Top_Once()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(3)];

		using AutoMock mock = AutoMock.GetLoose();

		CopyHistory sut = mock.Create<CopyHistory>();

		sut.Add(files[2], showAtOnce: false);

		sut.Add(files[1], showAtOnce: false);

		sut.Add(files[0], showAtOnce: false);

		// Act
		sut.Add(files[2], showAtOnce: false);

		// Assert
		sut.ItemIds
			.Should()
			.Equal(files[2].Id, files[0].Id, files[1].Id);
	}

	/// <summary>
	/// <see cref="CopyHistory.Add" />: a shown list hears of the copy only when it moves the file at once.
	/// </summary>
	[Test]
	public void Add_Tells_A_Shown_List_Only_When_It_Moves_At_Once([Values] bool showAtOnce)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		IMessenger messenger = new StrongReferenceMessenger();

		CopyHistoryFileAddedMessage? sent = null;

		messenger.Register<CopyHistoryFileAddedMessage>(this, (_, message) => sent = message);

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(messenger));

		CopyHistory sut = mock.Create<CopyHistory>();

		// Act
		sut.Add(file, showAtOnce);

		// Assert
		sent
			.Should()
			.Be(showAtOnce ? new CopyHistoryFileAddedMessage(file) : null);
	}

	/// <summary>
	/// <see cref="CopyHistory.Clear" />: the history forgets every file and the chosen one, and a shown list hears of it.
	/// </summary>
	[Test]
	public void Clear_Forgets_Every_File_And_The_Chosen_One()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(2)];

		IMessenger messenger = new StrongReferenceMessenger();

		CopyHistoryClearedMessage? sent = null;

		messenger.Register<CopyHistoryClearedMessage>(this, (_, message) => sent = message);

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(messenger));

		CopyHistory sut = mock.Create<CopyHistory>();

		sut.Add(files[1], showAtOnce: false);

		sut.Add(files[0], showAtOnce: false);

		sut.SelectedItemId = files[0].Id;

		// Act
		sut.Clear();

		// Assert
		sut.ItemIds
			.Should()
			.BeEmpty();

		sut.SelectedItemId
			.Should()
			.BeEmpty();

		sent
			.Should()
			.NotBeNull();
	}

	/// <summary>
	/// <see cref="CopyHistory.Load" />: a history left without files of the tree keeps no chosen file.
	/// </summary>
	[Test]
	public void Load_Keeps_No_Chosen_File_Without_Files_Of_The_Tree()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IWindowSettingsStore windowSettings = Substitute.For<IWindowSettingsStore>();

			Guid storedId = Guid.NewGuid();

			windowSettings
				.Read<CopyHistoryViewSettings>()
				.Returns(new CopyHistoryViewSettings
				{
					ItemIds = [storedId],
					SelectedItemId = storedId
				});

			builder.RegisterInstance(windowSettings);
		});

		CopyHistory sut = mock.Create<CopyHistory>();

		// Act
		sut.Load([ItemDtoFactory.CreateFileDto()]);

		// Assert
		sut.SelectedItemId
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="CopyHistory.Load" />: a history without a file stays empty.
	/// </summary>
	[Test]
	public void Load_Keeps_The_History_Empty_Without_Its_File()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IWindowSettingsStore windowSettings = Substitute.For<IWindowSettingsStore>();

			// The file is missing or cannot be read.
			windowSettings
				.Read<CopyHistoryViewSettings>()
				.Returns((CopyHistoryViewSettings?)null);

			builder.RegisterInstance(windowSettings);
		});

		CopyHistory sut = mock.Create<CopyHistory>();

		// Act
		sut.Load([ItemDtoFactory.CreateFileDto()]);

		// Assert
		sut.ItemIds
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="CopyHistory.Load" />: the history keeps the stored files that the tree holds, in the stored order, and the
	/// chosen one.
	/// </summary>
	[Test]
	public void Load_Keeps_The_Stored_Files_Of_The_Tree_And_The_Chosen_One()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(3)];

		Guid selectedId = files[0].Id;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IWindowSettingsStore windowSettings = Substitute.For<IWindowSettingsStore>();

			windowSettings
				.Read<CopyHistoryViewSettings>()
				.Returns(new CopyHistoryViewSettings
				{
					ItemIds = [files[2].Id, Guid.NewGuid(), files[0].Id],
					SelectedItemId = selectedId
				});

			builder.RegisterInstance(windowSettings);
		});

		CopyHistory sut = mock.Create<CopyHistory>();

		// Act
		sut.Load(files);

		// Assert
		sut.ItemIds
			.Should()
			.Equal(files[2].Id, files[0].Id);

		sut.SelectedItemId
			.Should()
			.Be(selectedId);
	}

	/// <summary>
	/// <see cref="CopyHistory.Remove" />: the history forgets the file, and a shown list hears of it.
	/// </summary>
	[Test]
	public void Remove_Forgets_The_File()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(2)];

		IMessenger messenger = new StrongReferenceMessenger();

		CopyHistoryFileRemovedMessage? sent = null;

		messenger.Register<CopyHistoryFileRemovedMessage>(this, (_, message) => sent = message);

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(messenger));

		CopyHistory sut = mock.Create<CopyHistory>();

		sut.Add(files[1], showAtOnce: false);

		sut.Add(files[0], showAtOnce: false);

		// Act
		sut.Remove(files[1]);

		// Assert
		sut.ItemIds
			.Should()
			.Equal(files[0].Id);

		sent
			.Should()
			.Be(new CopyHistoryFileRemovedMessage(files[1]));
	}

	/// <summary>
	/// <see cref="CopyHistory.Save" />: the history writes its files, latest first, and the chosen one over its file.
	/// </summary>
	[Test]
	public void Save_Writes_The_Files_And_The_Chosen_One()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(2)];

		IWindowSettingsStore windowSettings = Substitute.For<IWindowSettingsStore>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(windowSettings));

		CopyHistory sut = mock.Create<CopyHistory>();

		sut.Add(files[1], showAtOnce: false);

		sut.Add(files[0], showAtOnce: false);

		sut.SelectedItemId = files[1].Id;

		// Act
		sut.Save();

		// Assert
		windowSettings
			.Received(1)
			.Write(Arg.Is<CopyHistoryViewSettings>(x => x.ItemIds.SequenceEqual(new[] { files[0].Id, files[1].Id })
				&& x.SelectedItemId == files[1].Id));
	}
	#endregion
}
