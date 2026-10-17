using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.Dto.Dataset;
using DataOrganizer.Dto.Dialogs;
using DataOrganizer.Helpers.Dataset;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Messages.Editor;
using DataOrganizer.Models.Dataset;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.Services.Documents;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.UnitTests.Fakes;
using DataOrganizer.ViewModels;
using DynamicData.Binding;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Repository.Dto;
using Repository.Enums;
using Repository.Exceptions;
using Repository.Interfaces.Database;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(DatasetEditorViewModel)}"" type")]
internal class DatasetEditorViewModelTests
{
	#region Data
	/// <summary>
	/// Number of the records that the tests of the stored place load.
	/// </summary>
	private const int RecordCount = 50;

	/// <summary>
	/// Index of the record at the top in the tests of the stored place.
	/// </summary>
	private const int TopRecordIndex = 30;

	/// <summary>
	/// Distance from the top edge of the record at the top to the top of the list.
	/// </summary>
	private const double WithinRecordOffset = 7.0;
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="DatasetEditorViewModel.AddGroupAsync" />: adds a group to the parent group (expanding it) or to the root records and persists the change.
	/// </summary>
	[Test]
	public async Task AddGroupAsync_Adds_Group([Values] bool addToGroup)
	{
		// Arrange
		string name = RandomString.Create(10);

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		RecordsGroup? group = null;

		if (addToGroup)
		{
			group = new()
			{
				IsExpanded = false
			};
		}

		// Act
		await sut.AddGroupAsync(name, group);

		// Assert
		if (addToGroup && group is not null)
		{
			group.Children
				.Should()
				.Contain(x => x is RecordsGroup);

			group
				.IsExpanded
				.Should()
				.BeTrue();
		}
		else
		{
			sut.Records
				.Should()
				.Contain(x => x is RecordsGroup);
		}

		await dbAccess.Received(1).UpdateFileContentsAsync(
			Arg.Any<Guid>(),
			Arg.Any<byte[]>(),
			Arg.Any<DateTime?>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.AddGroupAsync" />: an edit of the records reports the date of change it is written
	/// with.
	/// </summary>
	[Test]
	public async Task AddGroupAsync_Reports_The_Date_Of_Change()
	{
		// Arrange
		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.UpdateFileContentsAsync(default, default!, default, default)
				.ReturnsForAnyArgs(true);

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder.RegisterInstance<TimeProvider>(time);
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		DateTime? reported = null;

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setUpdatedAtCallback: x => reported = x));

		// Act
		await sut.AddGroupAsync(RandomString.Create(10), null);

		// Assert
		reported
			.Should()
			.Be(time.GetLocalNow().LocalDateTime);
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.AddGroupAsync" />: the records of a protected dataset are saved encrypted under its
	/// keeper.
	/// </summary>
	[Test]
	public async Task AddGroupAsync_Saves_The_Records_Encrypted_Under_The_Keeper()
	{
		// Arrange
		byte[] contents = RandomValues.CreateBytes(10);

		Guid keeperId = Guid.NewGuid();

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(contents);

			contentCipher
				.TryEncrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: keeperId));

		// Act
		await sut.AddGroupAsync(RandomString.Create(10), null);

		// Assert
		contentCipher
			.Received(1)
			.TryEncrypt(keeperId, ContentIdentity.Contents, contents);

		await dbAccess.Received(1).UpdateFileContentsAsync(
			Arg.Any<Guid>(),
			Arg.Any<byte[]>(),
			Arg.Any<DateTime?>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.AddGroupAsync" />: records the keeper refuses to encrypt are not written.
	/// </summary>
	[Test]
	public async Task AddGroupAsync_Writes_Nothing_When_The_Encryption_Is_Refused()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			// The cipher swallows the failure and answers with a refusal.
			contentCipher
				.TryEncrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns((byte[]?)null);

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: Guid.NewGuid()));

		// Act
		await sut.AddGroupAsync(RandomString.Create(10), null);

		// Assert
		await dbAccess
			.DidNotReceiveWithAnyArgs()
			.UpdateFileContentsAsync(default, default!, default, default);
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.AddKeyValueAsync" />: adds a key-value record to the parent group (expanding it) or to the root records and persists the change.
	/// </summary>
	[Test]
	public async Task AddKeyValueAsync_Adds_Key_And_Value_Record([Values] bool addToGroup)
	{
		// Arrange
		string key = RandomString.Create(10);

		string value = key;

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		RecordsGroup? group = null;

		if (addToGroup)
		{
			group = new()
			{
				IsExpanded = false
			};
		}

		// Act
		await sut.AddKeyValueAsync(key, value, group);

		// Assert
		if (addToGroup && group is not null)
		{
			group.Children
				.Should()
				.Contain(x => x is KeyValueRecord);

			group
				.IsExpanded
				.Should()
				.BeTrue();
		}
		else
		{
			sut.Records
				.Should()
				.Contain(x => x is KeyValueRecord);
		}

		await dbAccess.Received(1).UpdateFileContentsAsync(
			Arg.Any<Guid>(),
			Arg.Any<byte[]>(),
			Arg.Any<DateTime?>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.AddKeyValueAsync" />: a new record is hidden by default only when the dataset is encrypted.
	/// </summary>
	[Test]
	public async Task AddKeyValueAsync_Hides_New_Record_When_Encrypted([Values] bool isEncrypted)
	{
		// Arrange
		string text = RandomString.Create(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(Substitute.For<IDbAccess>());

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: isEncrypted ? Guid.NewGuid() : null));

		// Act
		await sut.AddKeyValueAsync(text, text, group: null);

		// Assert
		sut.Records
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeOfType<KeyValueRecord>()
			.Which
			.IsHidden
			.Should()
			.Be(isEncrypted);
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.AddKeyValueCommand" />: the text entered into an encrypted dataset is declared sensitive to the dialog.
	/// </summary>
	[Test]
	public async Task AddKeyValueCommand_Declares_An_Encrypted_Dataset_Sensitive([Values] bool isEncrypted)
	{
		// Arrange
		ITextInputPrompt textInputPrompt = Substitute.For<ITextInputPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(textInputPrompt));

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: isEncrypted ? Guid.NewGuid() : null));

		// Act
		await sut
			.AddKeyValueCommand
			.ExecuteAsync(null);

		// Assert
		await textInputPrompt.Received(1).RequestKeyValueInputAsync(
			Arg.Is<KeyValueInputParameters>(x => x.IsSensitive == isEncrypted),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.AddKeyValueCommand" />: the value input field is masked only when the dataset is encrypted.
	/// </summary>
	[Test]
	public async Task AddKeyValueCommand_Masks_Value_Input_When_Encrypted([Values] bool isEncrypted)
	{
		// Arrange
		ITextInputPrompt textInputPrompt = Substitute.For<ITextInputPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(textInputPrompt));

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: isEncrypted ? Guid.NewGuid() : null));

		// Act
		await sut
			.AddKeyValueCommand
			.ExecuteAsync(null);

		// Assert
		await textInputPrompt.Received(1).RequestKeyValueInputAsync(
			Arg.Is<KeyValueInputParameters>(x => x!.MaskValueInput == isEncrypted),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.AddValueAsync" />: adds a value record to the parent group (expanding it) or to the root records and persists the change.
	/// </summary>
	[Test]
	public async Task AddValueAsync_Adds_Value_Record([Values] bool addToGroup)
	{
		// Arrange
		string value = RandomString.Create(10);

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		RecordsGroup? group = null;

		if (addToGroup)
		{
			group = new()
			{
				IsExpanded = false
			};
		}

		// Act
		await sut.AddValueAsync(value, group);

		// Assert
		if (addToGroup && group is not null)
		{
			group.Children
				.Should()
				.Contain(x => x is ValueRecord);

			group
				.IsExpanded
				.Should()
				.BeTrue();
		}
		else
		{
			sut.Records
				.Should()
				.Contain(x => x is ValueRecord);
		}

		await dbAccess.Received(1).UpdateFileContentsAsync(
			Arg.Any<Guid>(),
			Arg.Any<byte[]>(),
			Arg.Any<DateTime?>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.AddValueAsync" />: a new record is hidden by default only when the dataset is encrypted.
	/// </summary>
	[Test]
	public async Task AddValueAsync_Hides_New_Record_When_Encrypted([Values] bool isEncrypted)
	{
		// Arrange
		string text = RandomString.Create(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(Substitute.For<IDbAccess>());

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: isEncrypted ? Guid.NewGuid() : null));

		// Act
		await sut.AddValueAsync(text, group: null);

		// Assert
		sut.Records
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeOfType<ValueRecord>()
			.Which
			.IsHidden
			.Should()
			.Be(isEncrypted);
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.AddValueCommand" />: the value input dialog masks the input only when the dataset is encrypted.
	/// </summary>
	[Test]
	public async Task AddValueCommand_Masks_Input_When_Encrypted([Values] bool isEncrypted)
	{
		// Arrange
		ITextInputPrompt textInputPrompt = Substitute.For<ITextInputPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(textInputPrompt));

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: isEncrypted ? Guid.NewGuid() : null));

		// Act
		await sut
			.AddValueCommand
			.ExecuteAsync(null);

		// Assert
		await textInputPrompt.Received(1).RequestKeyValueInputAsync(
			Arg.Is<KeyValueInputParameters>(x => x!.MaskKeyInput == isEncrypted),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ContainerLoaded" />: deserializes the file contents and populates the records, marking the view model initialized.
	/// </summary>
	[Test]
	public async Task ContainerLoaded_Adds_Records()
	{
		// Arrange
		DatasetRecordBase[] records = [.. new SampleRecords().CreateRandomRecords()];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents contents = new()
			{
				Contents = RandomValues.CreateBytes(10),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			IJsonSerializer jsonSerializer = Substitute.For<IJsonSerializer>();

			jsonSerializer
				.Deserialize<DatasetRecordBase[]>(Arg.Any<byte[]>())
				.Returns(records);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder.RegisterInstance(jsonSerializer);
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		// Act
		await sut.ContainerLoaded();

		// Assert
		sut.IsInitialized
			.Should()
			.BeTrue();

		sut.Records
			.Should()
			.Contain(records);
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ContainerLoaded" />: the records of a protected dataset come from its contents
	/// opened with the session key of its keeper.
	/// </summary>
	[Test]
	public async Task ContainerLoaded_Adds_The_Decrypted_Records()
	{
		// Arrange
		DatasetRecordBase[] records = [.. new SampleRecords().CreateRandomRecords()];

		Guid keeperId = Guid.NewGuid();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			byte[] stored = RandomValues.CreateBytes(16);

			byte[] opened = RandomValues.CreateBytes(10);

			ValidatedContents contents = new()
			{
				Contents = stored,
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecrypt(keeperId, ContentIdentity.Contents, stored)
				.Returns(opened);

			IJsonSerializer jsonSerializer = Substitute.For<IJsonSerializer>();

			jsonSerializer
				.Deserialize<DatasetRecordBase[]>(opened)
				.Returns(records);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(jsonSerializer);
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: keeperId));

		// Act
		await sut.ContainerLoaded();

		// Assert
		sut.Records
			.Should()
			.Contain(records);
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ContainerLoaded" />: contents that fail their check close the editor for
	/// changes, and the loading still ends.
	/// </summary>
	[Test]
	public async Task ContainerLoaded_Closes_The_Editor_When_The_Contents_Are_Damaged()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents contents = new()
			{
				Contents = RandomValues.CreateBytes(10),
				IsValid = false
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		// Act
		await sut.ContainerLoaded();

		// Assert
		sut.IsContentUnavailable
			.Should()
			.BeTrue();

		sut.IsInitialized
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ContainerLoaded" />: contents the session key does not open close the editor for
	/// changes before any record is read.
	/// </summary>
	[Test]
	public async Task ContainerLoaded_Closes_The_Editor_When_The_Decryption_Fails()
	{
		// Arrange
		IJsonSerializer jsonSerializer = Substitute.For<IJsonSerializer>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents contents = new()
			{
				Contents = RandomValues.CreateBytes(10),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			// The key does not open the contents.
			contentCipher
				.TryDecrypt(Arg.Any<Guid>(), ContentIdentity.Contents, Arg.Any<byte[]>())
				.Returns((byte[]?)null);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(jsonSerializer);
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: Guid.NewGuid()));

		// Act
		await sut.ContainerLoaded();

		// Assert
		sut.IsContentUnavailable
			.Should()
			.BeTrue();

		jsonSerializer
			.DidNotReceive()
			.Deserialize<DatasetRecordBase[]>(Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ContainerLoaded" />: a read the database could not answer closes
	/// the editor for changes, and the loading still ends.
	/// </summary>
	[Test]
	public async Task ContainerLoaded_Closes_The_Editor_When_The_Read_Fails()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		// Act
		await sut.ContainerLoaded();

		// Assert
		sut.IsContentUnavailable
			.Should()
			.BeTrue();

		sut.IsInitialized
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ContainerLoaded" />: contents that could not be read close every command that
	/// changes the records.
	/// </summary>
	[TestCaseSource(nameof(EditCommands))]
	public async Task ContainerLoaded_Disables_The_Edit_Commands_When_The_Read_Fails(Func<DatasetEditorViewModel, ICommand> command)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		// Act
		await sut.ContainerLoaded();

		// Assert
		command(sut).CanExecute(null)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ContainerLoaded" />: a dataset without records has no place to come back to, so its
	/// stored state is left unread.
	/// </summary>
	[Test]
	public async Task ContainerLoaded_Does_Not_Read_The_Stored_State_Without_Records()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents contents = new()
			{
				Contents = [],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		// Act
		await sut.ContainerLoaded();

		// Assert
		await dbAccess
			.DidNotReceive()
			.GetFileEditorStateAsync(Arg.Any<Guid>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ContainerLoaded" />: the state read with the records is neither reported to the file
	/// nor saved again.
	/// </summary>
	[Test]
	public async Task ContainerLoaded_Does_Not_Save_The_Stored_State_Back()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			SystemTextJsonSerializer serializer = new();

			ValidatedContents contents = new()
			{
				Contents = serializer.SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			DatasetEditorState state = new()
			{
				TopRecordIndex = TopRecordIndex,
				WithinRecordOffset = WithinRecordOffset
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(serializer.Serialize(state));

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		string? reported = null;

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setEditorStateCallback: x => reported = x));

		// Act
		await sut.ContainerLoaded();

		// Assert
		reported
			.Should()
			.BeNull();

		await dbAccess
			.DidNotReceive()
			.UpdateFileEditorStateAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ContainerLoaded" />: once the contents are loaded, every command that changes the
	/// records can run.
	/// </summary>
	[TestCaseSource(nameof(EditCommands))]
	public async Task ContainerLoaded_Enables_The_Edit_Commands(Func<DatasetEditorViewModel, ICommand> command)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents contents = new()
			{
				Contents = [],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		// Act
		await sut.ContainerLoaded();

		// Assert
		command(sut).CanExecute(null)
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ContainerLoaded" />: a stored state that cannot be read leaves no place to come back
	/// to, and the editor stays open.
	/// </summary>
	[Test]
	public async Task ContainerLoaded_Leaves_The_View_State_Unset_For_A_Damaged_Stored_State()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents contents = new()
			{
				Contents = new SystemTextJsonSerializer().SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			// The stored state breaks off after its first character.
			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns("{");

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		// Act
		await sut.ContainerLoaded();

		// Assert
		sut.ViewState
			.Should()
			.BeNull();

		sut.IsContentUnavailable
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ContainerLoaded" />: the place stored for the dataset comes with its records.
	/// </summary>
	[Test]
	public async Task ContainerLoaded_Reads_The_Stored_State()
	{
		// Arrange
		DatasetEditorState state = new()
		{
			TopRecordIndex = TopRecordIndex,
			WithinRecordOffset = WithinRecordOffset
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			SystemTextJsonSerializer serializer = new();

			ValidatedContents contents = new()
			{
				Contents = serializer.SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(serializer.Serialize(state));

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		// Act
		await sut.ContainerLoaded();

		// Assert
		sut.ViewState
			.Should()
			.Be(state);
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ContainerLoaded" />: the stored state, which reaches the view through a binding, is
	/// set on the UI thread even when the store answers on another thread.
	/// </summary>
	[AvaloniaTest]
	public async Task ContainerLoaded_Sets_The_View_State_On_The_UI_Thread()
	{
		// Arrange
		bool? isUiThread = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			SystemTextJsonSerializer serializer = new();

			ValidatedContents contents = new()
			{
				Contents = serializer.SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			string? state = serializer.Serialize(new DatasetEditorState
			{
				TopRecordIndex = TopRecordIndex,
				WithinRecordOffset = WithinRecordOffset
			});

			IEditorStateStore editorStateStore = Substitute.For<IEditorStateStore>();

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			// The store answers on a thread of the pool, as a read from the database may.
			editorStateStore
				.ReadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
				.Returns(_ => Task.Run<string?>(async () =>
				{
					await Task.Delay(10);

					return state;
				}));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(editorStateStore);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		using IDisposable stateChanges = sut
			.WhenValueChanged(static x => x.ViewState, notifyOnInitialValue: false)
			.Subscribe(_ => isUiThread = Dispatcher.UIThread.CheckAccess());

		// Act
		await sut.ContainerLoaded();

		// Assert
		isUiThread
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ContainerLoaded" />: does not attempt deserialization when the file contents are empty, but still marks the view model initialized.
	/// </summary>
	[Test]
	public async Task ContainerLoaded_Skips_Empty_Contents_But_Marks_Initialized()
	{
		// Arrange
		IJsonSerializer jsonSerializer = Substitute.For<IJsonSerializer>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents contents = new()
			{
				Contents = [],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder.RegisterInstance(jsonSerializer);
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		// Act
		await sut.ContainerLoaded();

		// Assert
		sut.IsInitialized
			.Should()
			.BeTrue();

		jsonSerializer
			.DidNotReceive()
			.Deserialize<DatasetRecordBase[]>(Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ContainerLoaded" />: the editor starts from the state it is given, which the file
	/// keeps from an earlier editor, and leaves the stored one unread.
	/// </summary>
	[Test]
	public async Task ContainerLoaded_Starts_From_The_Given_State()
	{
		// Arrange
		DatasetEditorState state = new()
		{
			TopRecordIndex = TopRecordIndex,
			WithinRecordOffset = WithinRecordOffset
		};

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents contents = new()
			{
				Contents = new SystemTextJsonSerializer().SerializeToUtf8Bytes(DatasetRecordFactory.CreateValueRecords(RecordCount)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(initialEditorState: new SystemTextJsonSerializer().Serialize(state)));

		// Act
		await sut.ContainerLoaded();

		// Assert
		sut.ViewState
			.Should()
			.Be(state);

		await dbAccess
			.DidNotReceive()
			.GetFileEditorStateAsync(Arg.Any<Guid>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.CopyKeyValueToClipboardCommand" />: encrypted copies are flagged sensitive, plaintext copies are not.
	/// </summary>
	[Test]
	public async Task CopyKeyValueToClipboardCommand_Flags_Sensitive_When_Encrypted([Values] bool isEncrypted)
	{
		// Arrange
		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(clipboard);

			builder
				.RegisterType<ClipboardTextWriter>()
				.As<IClipboardTextWriter>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: isEncrypted ? Guid.NewGuid() : null));

		KeyValueRecord record = new()
		{
			Key = RandomString.Create(10),
			Value = RandomString.Create(10)
		};

		// Act
		await sut
			.CopyKeyValueToClipboardCommand
			.ExecuteAsync(record);

		// Assert
		if (isEncrypted)
		{
			await clipboard
				.Received(1)
				.SetTextAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), isSensitive: true);

			await clipboard
				.DidNotReceive()
				.SetTextAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), isSensitive: false);
		}
		else
		{
			await clipboard
				.Received(1)
				.SetTextAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), isSensitive: false);

			await clipboard
				.DidNotReceive()
				.SetTextAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), isSensitive: true);
		}
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.DeleteRecordAsync" />: removes the given record from the collection and persists the change.
	/// </summary>
	[Test]
	public async Task DeleteRecordAsync_Deletes_Record()
	{
		// Arrange
		DatasetRecordBase[] records = [.. new SampleRecords().CreateRandomRecords()];

		DatasetRecordBase toBeDeleted = records[0];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut
			.Records
			.AddRange(records);

		// Act
		await sut.DeleteRecordAsync(toBeDeleted);

		// Assert
		sut.Records
			.Should()
			.NotContain(toBeDeleted);

		await dbAccess.Received(1).UpdateFileContentsAsync(
			Arg.Any<Guid>(),
			Arg.Any<byte[]>(),
			Arg.Any<DateTime?>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.DeleteRecordAsync" />: a save the database turned down neither
	/// escapes nor puts the record back.
	/// </summary>
	[Test]
	public async Task DeleteRecordAsync_Keeps_The_Record_Out_When_The_Save_Fails()
	{
		// Arrange
		DatasetRecordBase[] records = [.. new SampleRecords().CreateRandomRecords()];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.UpdateFileContentsAsync(
					Arg.Any<Guid>(),
					Arg.Any<byte[]>(),
					Arg.Any<DateTime?>())
				.ThrowsAsync(new DatabaseNotWritableException(DbConnectionStatus.FileUnreadable, nameof(IDbAccess)));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut
			.Records
			.AddRange(records);

		// Act
		Func<Task> act = () => sut.DeleteRecordAsync(records[0]);

		// Assert
		await act
			.Should()
			.NotThrowAsync();

		// The file still holds the record, so a refused save leaves the view ahead of what is stored.
		sut.Records
			.Should()
			.NotContain(records[0]);
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.EditKeyValueAsync" />: updates the record's key and value, persisting only when the values actually changed.
	/// </summary>
	[Test]
	public async Task EditKeyValueAsync_Edits_Record([Values] bool isSameValue)
	{
		// Arrange
		string key = RandomString.Create(10);

		string value = RandomString.Create(10);

		KeyValueRecord target = new();

		if (isSameValue)
		{
			target.Key = key;

			target.Value = value;
		}

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		// Act
		await sut.EditKeyValueAsync(target, key, value);

		// Assert
		target.Key
			.Should()
			.Be(key);

		target.Value
			.Should()
			.Be(value);

		await dbAccess.Received(isSameValue ? 0 : 1).UpdateFileContentsAsync(
			Arg.Any<Guid>(),
			Arg.Any<byte[]>(),
			Arg.Any<DateTime?>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.EditKeyValueCommand" />: the text edited in an encrypted dataset is declared sensitive to the dialog.
	/// </summary>
	[Test]
	public async Task EditKeyValueCommand_Declares_An_Encrypted_Dataset_Sensitive([Values] bool isEncrypted)
	{
		// Arrange
		ITextInputPrompt textInputPrompt = Substitute.For<ITextInputPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(textInputPrompt));

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: isEncrypted ? Guid.NewGuid() : null));

		KeyValueRecord record = new()
		{
			Key = RandomString.Create(10),
			Value = RandomString.Create(10)
		};

		// Act
		await sut
			.EditKeyValueCommand
			.ExecuteAsync(record);

		// Assert
		await textInputPrompt.Received(1).RequestKeyValueInputAsync(
			Arg.Is<KeyValueInputParameters>(x => x!.IsSensitive == isEncrypted),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.EditKeyValueCommand" />: the value input field is masked only when the record is hidden.
	/// </summary>
	[Test]
	public async Task EditKeyValueCommand_Masks_Value_Input_When_Hidden([Values] bool isHidden)
	{
		// Arrange
		ITextInputPrompt textInputPrompt = Substitute.For<ITextInputPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(textInputPrompt));

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		KeyValueRecord record = new()
		{
			IsHidden = isHidden,
			Key = RandomString.Create(10),
			Value = RandomString.Create(10)
		};

		// Act
		await sut
			.EditKeyValueCommand
			.ExecuteAsync(record);

		// Assert
		await textInputPrompt.Received(1).RequestKeyValueInputAsync(
			Arg.Is<KeyValueInputParameters>(x => x!.MaskValueInput == isHidden),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.EditNoteAsync" />: updates the record's note and persists the change.
	/// </summary>
	[Test]
	public async Task EditNoteAsync_Edits_Note_Of_Record()
	{
		// Arrange
		string note = RandomString.Create(10);

		ValueRecord target = new();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		// Act
		await sut.EditNoteAsync(target, note);

		// Assert
		target.Note
			.Should()
			.Be(note);

		await dbAccess.Received(1).UpdateFileContentsAsync(
			Arg.Any<Guid>(),
			Arg.Any<byte[]>(),
			Arg.Any<DateTime?>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.EditNoteCommand" />: the note of an encrypted dataset is declared sensitive to the dialog.
	/// </summary>
	[Test]
	public async Task EditNoteCommand_Declares_The_Note_Of_An_Encrypted_Dataset_Sensitive([Values] bool isEncrypted)
	{
		// Arrange
		ITextInputPrompt textInputPrompt = Substitute.For<ITextInputPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			textInputPrompt
				.RequestMultilineTextAsync(
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<bool>(),
					Arg.Any<CancellationToken>())
				.Returns(new TextInputResult());

			builder.RegisterInstance(textInputPrompt);
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: isEncrypted ? Guid.NewGuid() : null));

		RecordsGroup record = new()
		{
			Name = RandomString.Create(10),
			Note = RandomString.Create(10)
		};

		// Act
		await sut
			.EditNoteCommand
			.ExecuteAsync(record);

		// Assert
		await textInputPrompt.Received(1).RequestMultilineTextAsync(
			Arg.Any<string>(),
			Arg.Any<string>(),
			isEncrypted,
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.EditNoteCommand" />: the note dialog of a group is headed by its name.
	/// </summary>
	[Test]
	public async Task EditNoteCommand_Heads_The_Dialog_Of_A_Group_With_Its_Name()
	{
		// Arrange
		ITextInputPrompt textInputPrompt = Substitute.For<ITextInputPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			textInputPrompt
				.RequestMultilineTextAsync(
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<bool>(),
					Arg.Any<CancellationToken>())
				.Returns(new TextInputResult());

			builder.RegisterInstance(textInputPrompt);
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		RecordsGroup record = new()
		{
			Name = RandomString.Create(10),
			Note = RandomString.Create(10)
		};

		// Act
		await sut
			.EditNoteCommand
			.ExecuteAsync(record);

		// Assert
		await textInputPrompt.Received(1).RequestMultilineTextAsync(
			record.Note,
			record.Name,
			Arg.Any<bool>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.EditNoteCommand" />: the note dialog of a key-value record is headed by its key.
	/// </summary>
	[Test]
	public async Task EditNoteCommand_Heads_The_Dialog_Of_A_Key_Value_Record_With_Its_Key()
	{
		// Arrange
		ITextInputPrompt textInputPrompt = Substitute.For<ITextInputPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			textInputPrompt
				.RequestMultilineTextAsync(
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<bool>(),
					Arg.Any<CancellationToken>())
				.Returns(new TextInputResult());

			builder.RegisterInstance(textInputPrompt);
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		KeyValueRecord record = new()
		{
			Key = RandomString.Create(10),
			Note = RandomString.Create(10),
			Value = RandomString.Create(10)
		};

		// Act
		await sut
			.EditNoteCommand
			.ExecuteAsync(record);

		// Assert
		await textInputPrompt.Received(1).RequestMultilineTextAsync(
			record.Note,
			record.Key,
			Arg.Any<bool>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.EditNoteCommand" />: a record without a name is left without a header,
	/// its value never becomes one.
	/// </summary>
	[Test]
	public async Task EditNoteCommand_Leaves_A_Value_Record_Without_A_Header()
	{
		// Arrange
		ITextInputPrompt textInputPrompt = Substitute.For<ITextInputPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			textInputPrompt
				.RequestMultilineTextAsync(
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<bool>(),
					Arg.Any<CancellationToken>())
				.Returns(new TextInputResult());

			builder.RegisterInstance(textInputPrompt);
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		ValueRecord record = new()
		{
			Note = RandomString.Create(10),
			Value = RandomString.Create(10)
		};

		// Act
		await sut
			.EditNoteCommand
			.ExecuteAsync(record);

		// Assert
		await textInputPrompt.Received(1).RequestMultilineTextAsync(
			record.Note,
			null,
			Arg.Any<bool>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.EditValueAsync" />: updates the record's value, persisting only when the value actually changed.
	/// </summary>
	[Test]
	public async Task EditValueAsync_Edits_Record([Values] bool isSameValue)
	{
		// Arrange
		string value = RandomString.Create(10);

		ValueRecord target = new();

		if (isSameValue)
		{
			target.Value = value;
		}

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		// Act
		await sut.EditValueAsync(target, value);

		// Assert
		target.Value
			.Should()
			.Be(value);

		await dbAccess.Received(isSameValue ? 0 : 1).UpdateFileContentsAsync(
			Arg.Any<Guid>(),
			Arg.Any<byte[]>(),
			Arg.Any<DateTime?>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.EditValueCommand" />: the value input dialog masks the input only when the record is hidden.
	/// </summary>
	[Test]
	public async Task EditValueCommand_Masks_Input_When_Hidden([Values] bool isHidden)
	{
		// Arrange
		ITextInputPrompt textInputPrompt = Substitute.For<ITextInputPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(textInputPrompt));

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		ValueRecord record = new()
		{
			IsHidden = isHidden,
			Value = RandomString.Create(10)
		};

		// Act
		await sut
			.EditValueCommand
			.ExecuteAsync(record);

		// Assert
		await textInputPrompt.Received(1).RequestKeyValueInputAsync(
			Arg.Is<KeyValueInputParameters>(x => x!.MaskKeyInput == isHidden),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ExpandCollapseAsync" />: an edit whose save failed goes with the next save, which
	/// then writes the date of change of the edit.
	/// </summary>
	[Test]
	public async Task ExpandCollapseAsync_Dates_An_Edit_Whose_Save_Failed()
	{
		// Arrange
		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(_ => RandomValues.CreateBytes(10));

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			// The save of the edit fails, the next one succeeds.
			dbAccess
				.UpdateFileContentsAsync(default, default!, default, default)
				.ReturnsForAnyArgs(
					Task.FromException<bool>(new InvalidOperationException()),
					Task.FromResult(true));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder.RegisterInstance<TimeProvider>(time);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		DateTime? reported = null;

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setUpdatedAtCallback: x => reported = x));

		// The new group starts expanded.
		await sut.AddGroupAsync(RandomString.Create(10), null);

		// Act
		await sut.ExpandCollapseAsync(null, false);

		// Assert
		reported
			.Should()
			.Be(time.GetLocalNow().LocalDateTime);
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ExpandCollapseAsync" />: expanded groups are saved as a change of view, which keeps
	/// the date of change of the dataset.
	/// </summary>
	[Test]
	public async Task ExpandCollapseAsync_Keeps_The_Date_Of_Change()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			dbAccess
				.UpdateFileContentsAsync(default, default!, default, default)
				.ReturnsForAnyArgs(true);

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			// A save as an edit would take its date from this clock.
			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut
			.Records
			.Add(new RecordsGroup
			{
				IsExpanded = false
			});

		DateTime? reported = null;

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setUpdatedAtCallback: x => reported = x));

		// Act
		await sut.ExpandCollapseAsync(null, true);

		// Assert
		reported
			.Should()
			.BeNull();

		await dbAccess
			.ReceivedWithAnyArgs(1)
			.UpdateFileContentsAsync(default, default!, default, default);
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ExpandCollapseAsync" />: sets the expanded state of all groups, in a group or at the root, and persists it.
	/// </summary>
	[Test]
	public async Task ExpandCollapseAsync_Sets_The_Expanded_State(
		[Values] bool expand,
		[Values] bool inGroup)
	{
		// Arrange
		RecordsGroup[] groups = [.. new SampleRecords()
			.CreateGroups(50)
			.ToArray()
			.ForEach(x => x.IsExpanded = !expand)];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		RecordsGroup? group = inGroup ? new() : null;

		// Whichever collection holds the records, the rest of the test reads the same.
		ObservableCollection<DatasetRecordBase> target = group is not null
			? group.Children
			: sut.Records;

		target.AddRange(groups);

		// Act
		await sut.ExpandCollapseAsync(group, expand);

		// Assert
		target
			.Should()
			.Contain(groups);

		groups
			.Should()
			.OnlyContain(x => x.IsExpanded == expand);

		await dbAccess.Received(1).UpdateFileContentsAsync(
			Arg.Any<Guid>(),
			Arg.Any<byte[]>(),
			Arg.Any<DateTime?>());
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.IsEncrypted" />: reflects whether a password keeper is known.
	/// </summary>
	[Test]
	public void IsEncrypted_Reflects_KeeperId()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		// Assert
		sut.IsEncrypted
			.Should()
			.BeFalse();

		// Act
		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: Guid.NewGuid()));

		// Assert
		sut.IsEncrypted
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.IsHiddenChanged" />: a value shown or hidden is saved as a change of view, which
	/// keeps the date of change of the dataset.
	/// </summary>
	[Test]
	public async Task IsHiddenChanged_Keeps_The_Date_Of_Change()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			dbAccess
				.UpdateFileContentsAsync(default, default!, default, default)
				.ReturnsForAnyArgs(true);

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			// A save as an edit would take its date from this clock.
			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		DateTime? reported = null;

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setUpdatedAtCallback: x => reported = x));

		// Act
		await sut.IsHiddenChanged();

		// Assert
		reported
			.Should()
			.BeNull();

		await dbAccess
			.ReceivedWithAnyArgs(1)
			.UpdateFileContentsAsync(default, default!, default, default);
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.IsHiddenChanged" />: persists the content only when not read-only.
	/// </summary>
	[Test]
	public async Task IsHiddenChanged_Persists_Only_When_Editable([Values] bool isReadOnly)
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut.IsReadOnly = isReadOnly;

		// Act
		await sut.IsHiddenChanged();

		// Assert
		await dbAccess.Received(isReadOnly ? 0 : 1).UpdateFileContentsAsync(
			Arg.Any<Guid>(),
			Arg.Any<byte[]>(),
			Arg.Any<DateTime?>());
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.IsInitialized" />: the commands that change the records wait until the
	/// contents are loaded.
	/// </summary>
	[TestCaseSource(nameof(EditCommands))]
	public void IsInitialized_Gates_The_Edit_Commands(Func<DatasetEditorViewModel, ICommand> command)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		// Act
		bool canExecute = command(sut).CanExecute(null);

		// Assert
		sut.IsInitialized
			.Should()
			.BeFalse();

		canExecute
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.IsReadOnly" />: no command that changes the records runs in the read-only
	/// mode.
	/// </summary>
	[TestCaseSource(nameof(EditCommands))]
	public void IsReadOnly_Disables_The_Edit_Commands(Func<DatasetEditorViewModel, ICommand> command)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		// Act
		sut.IsReadOnly = true;

		// Assert
		command(sut).CanExecute(null)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.MoveRecord" />: a record dropped before the contents are loaded stays where it is.
	/// </summary>
	[Test]
	public async Task MoveRecord_Does_Nothing_Before_The_Contents_Are_Loaded()
	{
		// Arrange
		ValueRecord first = new();

		ValueRecord second = new();

		bool highlighted = false;

		// A move that got through would pulse the record at once.
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<InlineDispatcherAccessor>()
			.As<IDispatcherAccessor>());

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut
			.Records
			.AddRange([first, second]);

		using IDisposable subscription = first.HighlightSignal.Subscribe(_ => highlighted = true);

		DatasetRecordMove move = new()
		{
			Index = 2,
			Record = first,
			Target = sut.Records
		};

		// Act
		await sut.MoveRecord(move);

		// Assert
		sut.Records
			.Should()
			.Equal(first, second);

		highlighted
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.MoveRecord" />: a record dropped into records that could not be read stays where it
	/// is.
	/// </summary>
	[Test]
	public async Task MoveRecord_Does_Nothing_When_The_Contents_Are_Unavailable()
	{
		// Arrange
		ValueRecord first = new();

		ValueRecord second = new();

		bool highlighted = false;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			// A move that got through would pulse the record at once.
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		await sut.ContainerLoaded();

		sut
			.Records
			.AddRange([first, second]);

		using IDisposable subscription = first.HighlightSignal.Subscribe(_ => highlighted = true);

		DatasetRecordMove move = new()
		{
			Index = 2,
			Record = first,
			Target = sut.Records
		};

		// Act
		await sut.MoveRecord(move);

		// Assert
		sut.Records
			.Should()
			.Equal(first, second);

		highlighted
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.MoveRecord" />: a record dropped onto its own edge stays where it is, so nothing is
	/// saved or pulsed.
	/// </summary>
	[Test]
	public async Task MoveRecord_Leaves_A_Record_At_Its_Own_Edge()
	{
		// Arrange
		ValueRecord first = new();

		ValueRecord second = new();

		ValueRecord third = new();

		int changes = 0;

		bool highlighted = false;

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents contents = new()
			{
				Contents = [],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		await sut.ContainerLoaded();

		sut
			.Records
			.AddRange([first, second, third]);

		sut.Records.CollectionChanged += (_, _) => changes++;

		using IDisposable subscription = second.HighlightSignal.Subscribe(_ => highlighted = true);

		// The slot right after the first record is where the second one already is.
		DatasetRecordMove move = new()
		{
			Index = 1,
			Record = second,
			Target = sut.Records
		};

		// Act
		await sut.MoveRecord(move);

		// Assert
		changes
			.Should()
			.Be(0);

		highlighted
			.Should()
			.BeFalse();

		await dbAccess
			.DidNotReceiveWithAnyArgs()
			.UpdateFileContentsAsync(default, default!, default, default);
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.MoveRecord" />: moves the dropped record, saves the records and pulses the record,
	/// doing none of it when the dataset is read-only.
	/// </summary>
	[Test]
	public async Task MoveRecord_Moves_Saves_And_Highlights_Only_When_Editable([Values] bool isReadOnly)
	{
		// Arrange
		ValueRecord first = new();

		ValueRecord second = new();

		ValueRecord third = new();

		bool highlighted = false;

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents contents = new()
			{
				Contents = [],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		await sut.ContainerLoaded();

		sut.IsReadOnly = isReadOnly;

		sut
			.Records
			.AddRange([first, second, third]);

		using IDisposable subscription = first.HighlightSignal.Subscribe(_ => highlighted = true);

		DatasetRecordMove move = new()
		{
			Index = 3,
			Record = first,
			Target = sut.Records
		};

		DatasetRecordBase[] expected = isReadOnly
			? [first, second, third]
			: [second, third, first];

		// Act
		await sut
			.MoveRecordCommand
			.ExecuteAsync(move);

		// Assert
		sut.Records
			.Should()
			.Equal(expected);

		await dbAccess.Received(isReadOnly ? 0 : 1).UpdateFileContentsAsync(
			Arg.Any<Guid>(),
			Arg.Any<byte[]>(),
			Arg.Any<DateTime?>());

		highlighted
			.Should()
			.Be(!isReadOnly);
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.MoveRecord" />: moved records are an edit, which reports the date of change it is
	/// written with.
	/// </summary>
	[Test]
	public async Task MoveRecord_Reports_The_Date_Of_Change()
	{
		// Arrange
		ValueRecord first = new();

		ValueRecord second = new();

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents contents = new()
			{
				Contents = [],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			dbAccess
				.UpdateFileContentsAsync(default, default!, default, default)
				.ReturnsForAnyArgs(true);

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder.RegisterInstance<TimeProvider>(time);
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		await sut.ContainerLoaded();

		sut
			.Records
			.AddRange([first, second]);

		DateTime? reported = null;

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setUpdatedAtCallback: x => reported = x));

		DatasetRecordMove move = new()
		{
			Index = 2,
			Record = first,
			Target = sut.Records
		};

		// Act
		await sut.MoveRecord(move);

		// Assert
		reported
			.Should()
			.Be(time.GetLocalNow().LocalDateTime);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.Receive(FlushEditorsMessage)" />: in read-only mode a flush writes nothing,
	/// not even records whose save failed.
	/// </summary>
	[Test]
	public async Task Receive_Flush_In_Read_Only_Mode_Writes_Nothing()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(_ => RandomValues.CreateBytes(10));

			dbAccess
				.UpdateFileContentsAsync(default, default!, default, default)
				.ReturnsForAnyArgs(Task.FromException<bool>(new InvalidOperationException()));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		await sut.AddGroupAsync(RandomString.Create(10), null);

		sut.IsReadOnly = true;

		FlushEditorsMessage flush = new();

		// Act
		sut.Receive(flush);

		IReadOnlyCollection<bool> responses = await flush.GetResponsesAsync();

		// Assert
		responses
			.Should()
			.Equal(true);

		// Only the failed save of the new group
		await dbAccess
			.ReceivedWithAnyArgs(1)
			.UpdateFileContentsAsync(default, default!, default, default);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.Receive(FlushEditorsMessage)" />: a flush writes again a change of view whose
	/// save failed, still without a date of change.
	/// </summary>
	[Test]
	public async Task Receive_Flush_Keeps_The_Date_Of_A_View_Change_Whose_Save_Failed()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(_ => RandomValues.CreateBytes(10));

			// The save of the change fails, the next one succeeds.
			dbAccess
				.UpdateFileContentsAsync(default, default!, default, default)
				.ReturnsForAnyArgs(
					Task.FromException<bool>(new InvalidOperationException()),
					Task.FromResult(true));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			// A save as an edit would take its date from this clock.
			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		DateTime? reported = null;

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setUpdatedAtCallback: x => reported = x));

		await sut.IsHiddenChanged();

		FlushEditorsMessage flush = new();

		// Act
		sut.Receive(flush);

		await flush.GetResponsesAsync();

		// Assert
		reported
			.Should()
			.BeNull();

		await dbAccess
			.ReceivedWithAnyArgs(2)
			.UpdateFileContentsAsync(default, default!, default, default);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.Receive(FlushEditorsMessage)" />: a flush writes nothing while every change is
	/// saved, so the date of change stays as it is.
	/// </summary>
	[Test]
	public async Task Receive_Flush_Writes_Nothing_Without_A_Failed_Save()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(_ => RandomValues.CreateBytes(10));

			dbAccess
				.UpdateFileContentsAsync(default, default!, default, default)
				.ReturnsForAnyArgs(true);

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		await sut.AddGroupAsync(RandomString.Create(10), null);

		FlushEditorsMessage flush = new();

		// Act
		sut.Receive(flush);

		IReadOnlyCollection<bool> responses = await flush.GetResponsesAsync();

		// Assert
		responses
			.Should()
			.Equal(true);

		// Only the save of the new group
		await dbAccess
			.ReceivedWithAnyArgs(1)
			.UpdateFileContentsAsync(default, default!, default, default);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.Receive(FlushEditorsMessage)" />: a flush writes the records again after their
	/// save failed.
	/// </summary>
	[Test]
	public async Task Receive_Flush_Writes_The_Records_Whose_Save_Failed()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(_ => RandomValues.CreateBytes(10));

			dbAccess
				.UpdateFileContentsAsync(default, default!, default, default)
				.ReturnsForAnyArgs(
					Task.FromException<bool>(new InvalidOperationException()),
					Task.FromResult(true));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		await sut.AddGroupAsync(RandomString.Create(10), null);

		FlushEditorsMessage flush = new();

		// Act
		sut.Receive(flush);

		IReadOnlyCollection<bool> responses = await flush.GetResponsesAsync();

		// Assert
		responses
			.Should()
			.Equal(true);

		await dbAccess
			.ReceivedWithAnyArgs(2)
			.UpdateFileContentsAsync(default, default!, default, default);
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.RenameGroupAsync" />: updates the group's name, persisting only when the name actually changed.
	/// </summary>
	[Test]
	public async Task RenameGroupAsync_Renames_Group([Values] bool isSameValue)
	{
		// Arrange
		string name = RandomString.Create(10);

		RecordsGroup group = new();

		if (isSameValue)
		{
			group.Name = name;
		}

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		// Act
		await sut.RenameGroupAsync(group, name);

		// Assert
		group.Name
			.Should()
			.Be(name);

		await dbAccess.Received(isSameValue ? 0 : 1).UpdateFileContentsAsync(
			Arg.Any<Guid>(),
			Arg.Any<byte[]>(),
			Arg.Any<DateTime?>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ShowHideAsync" />: hidden values are saved as a change of view, which keeps the date
	/// of change of the dataset.
	/// </summary>
	[Test]
	public async Task ShowHideAsync_Keeps_The_Date_Of_Change()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			dbAccess
				.UpdateFileContentsAsync(default, default!, default, default)
				.ReturnsForAnyArgs(true);

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			// A save as an edit would take its date from this clock.
			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut
			.Records
			.Add(new ValueRecord());

		DateTime? reported = null;

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setUpdatedAtCallback: x => reported = x));

		// Act
		await sut.ShowHideAsync(null, true);

		// Assert
		reported
			.Should()
			.BeNull();

		await dbAccess
			.ReceivedWithAnyArgs(1)
			.UpdateFileContentsAsync(default, default!, default, default);
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ShowHideAsync" />: sets the hidden state of all records, in a group or at the root, and persists it.
	/// </summary>
	[Test]
	public async Task ShowHideAsync_Sets_The_Hidden_State(
		[Values] bool hide,
		[Values] bool inGroup)
	{
		// Arrange
		const int count = 50;

		SampleRecords samples = new();

		ValueRecordBase[] records = [.. samples
			.CreateValueRecords(count)
			.Concat<ValueRecordBase>(samples.CreateKeyValueRecords(count))
			.ToArray()
			.ForEach(x => x.IsHidden = !hide)];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		RecordsGroup? group = inGroup ? new() : null;

		// Whichever collection holds the records, the rest of the test reads the same.
		ObservableCollection<DatasetRecordBase> target = group is not null
			? group.Children
			: sut.Records;

		target.AddRange(records);

		// Act
		await sut.ShowHideAsync(group, hide);

		// Assert
		target
			.Should()
			.Contain(records);

		records
			.Should()
			.OnlyContain(x => x.IsHidden == hide);

		await dbAccess.Received(1).UpdateFileContentsAsync(
			Arg.Any<Guid>(),
			Arg.Any<byte[]>(),
			Arg.Any<DateTime?>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.SortAsync" />: a read-only dataset is never written back. The check
	/// lives in the save path, so it holds for every change that persists the records.
	/// </summary>
	[Test]
	public async Task SortAsync_Does_Not_Persist_A_Read_Only_Dataset()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut.IsReadOnly = true;

		sut
			.Records
			.AddRange(new SampleRecords().CreateRandomRecords(eachTypes: 5));

		// Act
		await sut.SortAsync(group: null, ListSortDirection.Ascending);

		// Assert
		await dbAccess.DidNotReceive().UpdateFileContentsAsync(
			Arg.Any<Guid>(),
			Arg.Any<byte[]>(),
			Arg.Any<DateTime?>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.SortAsync" />: records of a kind the sort does not know would be lost, so the
	/// records keep their order and nothing is saved.
	/// </summary>
	[Test]
	public async Task SortAsync_Keeps_Records_It_Cannot_Sort()
	{
		// Arrange
		ForeignRecord foreign = new();

		ValueRecord value = new()
		{
			Value = RandomString.Create(10)
		};

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut
			.Records
			.AddRange([foreign, value]);

		// Act
		await sut.SortAsync(null, ListSortDirection.Ascending);

		// Assert
		sut.Records
			.Should()
			.Equal(foreign, value);

		await dbAccess
			.DidNotReceiveWithAnyArgs()
			.UpdateFileContentsAsync(default, default!, default, default);
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.SortAsync" />: sorted records are an edit, which reports the date of change it is
	/// written with.
	/// </summary>
	[Test]
	public async Task SortAsync_Reports_The_Date_Of_Change()
	{
		// Arrange
		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.UpdateFileContentsAsync(default, default!, default, default)
				.ReturnsForAnyArgs(true);

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder.RegisterInstance<TimeProvider>(time);
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		DateTime? reported = null;

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setUpdatedAtCallback: x => reported = x));

		// Act
		await sut.SortAsync(null, ListSortDirection.Ascending);

		// Assert
		reported
			.Should()
			.Be(time.GetLocalNow().LocalDateTime);
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.SortAsync" />: sorts the records of a group or of the root in the given direction and persists them.
	/// </summary>
	[Test]
	public async Task SortAsync_Sorts_The_Records(
		[Values] ListSortDirection direction,
		[Values] bool inGroup)
	{
		// Arrange
		DatasetRecordBase[] records = [.. new SampleRecords().CreateRandomRecords(eachTypes: 5)];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.SerializeToUtf8Bytes(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		RecordsGroup? group = inGroup ? new() : null;

		// Whichever collection holds the records, the rest of the test reads the same.
		ObservableCollection<DatasetRecordBase> target = group is not null
			? group.Children
			: sut.Records;

		target.AddRange(records);

		// Act
		await sut.SortAsync(group, direction);

		// Assert
		target
			.Should()
			.HaveCount(records.Length);

		// The records follow the order of the current culture, as the assertions would compare by code point otherwise.
		if (direction == ListSortDirection.Ascending)
		{
			target.OfType<ValueRecord>()
				.Should()
				.BeInAscendingOrder(x => x.Value, StringComparer.CurrentCulture);
		}
		else
		{
			target.OfType<ValueRecord>()
				.Should()
				.BeInDescendingOrder(x => x.Value, StringComparer.CurrentCulture);
		}

		await dbAccess.Received(1).UpdateFileContentsAsync(
			Arg.Any<Guid>(),
			Arg.Any<byte[]>(),
			Arg.Any<DateTime?>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ViewState" />: a place that comes back to the one saved last, after a change that
	/// the read-only mode kept unsaved, is not saved again.
	/// </summary>
	[Test]
	public async Task ViewState_Does_Not_Save_The_State_Saved_Last()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents contents = new()
			{
				Contents = [],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		await sut.ContainerLoaded();

		DatasetEditorState saved = new()
		{
			TopRecordIndex = TopRecordIndex,
			WithinRecordOffset = WithinRecordOffset
		};

		sut.ViewState = saved;

		sut.IsReadOnly = true;

		sut.ViewState = new DatasetEditorState
		{
			TopRecordIndex = TopRecordIndex + 1,
			WithinRecordOffset = WithinRecordOffset
		};

		sut.IsReadOnly = false;

		// Act
		sut.ViewState = saved;

		// Assert
		await dbAccess
			.Received(1)
			.UpdateFileEditorStateAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ViewState" />: once the editor is closed, a new place of the list is neither
	/// reported to the file nor saved.
	/// </summary>
	[Test]
	public async Task ViewState_Is_Not_Saved_After_The_Editor_Is_Closed()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents contents = new()
			{
				Contents = [],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		string? reported = null;

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setEditorStateCallback: x => reported = x));

		await sut.ContainerLoaded();

		sut.Dispose();

		// Act
		sut.ViewState = new DatasetEditorState
		{
			TopRecordIndex = TopRecordIndex,
			WithinRecordOffset = WithinRecordOffset
		};

		// Assert
		reported
			.Should()
			.BeNull();

		await dbAccess
			.DidNotReceive()
			.UpdateFileEditorStateAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ViewState" />: before the records are read, a place of the list is neither reported
	/// to the file nor saved.
	/// </summary>
	[Test]
	public async Task ViewState_Is_Not_Saved_Before_The_Records_Are_Read()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		string? reported = null;

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setEditorStateCallback: x => reported = x));

		// Act
		sut.ViewState = new DatasetEditorState
		{
			TopRecordIndex = TopRecordIndex,
			WithinRecordOffset = WithinRecordOffset
		};

		// Assert
		reported
			.Should()
			.BeNull();

		await dbAccess
			.DidNotReceive()
			.UpdateFileEditorStateAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ViewState" />: when the contents could not be read, a place of the list is neither
	/// reported to the file nor saved.
	/// </summary>
	[Test]
	public async Task ViewState_Is_Not_Saved_When_The_Contents_Are_Unavailable()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		string? reported = null;

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setEditorStateCallback: x => reported = x));

		await sut.ContainerLoaded();

		// Act
		sut.ViewState = new DatasetEditorState
		{
			TopRecordIndex = TopRecordIndex,
			WithinRecordOffset = WithinRecordOffset
		};

		// Assert
		reported
			.Should()
			.BeNull();

		await dbAccess
			.DidNotReceive()
			.UpdateFileEditorStateAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ViewState" />: in the read-only mode a new place of the list reaches the file but is
	/// not saved.
	/// </summary>
	[Test]
	public async Task ViewState_Reports_The_State_Without_Saving_It_In_The_ReadOnly_Mode()
	{
		// Arrange
		DatasetEditorState state = new()
		{
			TopRecordIndex = TopRecordIndex,
			WithinRecordOffset = WithinRecordOffset
		};

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents contents = new()
			{
				Contents = [],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		string? reported = null;

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setEditorStateCallback: x => reported = x));

		await sut.ContainerLoaded();

		sut.IsReadOnly = true;

		// Act
		sut.ViewState = state;

		// Assert
		reported
			.Should()
			.NotBeNull();

		new SystemTextJsonSerializer().Deserialize<DatasetEditorState>(reported!)
			.Should()
			.Be(state);

		await dbAccess
			.DidNotReceive()
			.UpdateFileEditorStateAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ViewState" />: a new place of the list is saved, and the file gets the same state.
	/// </summary>
	[Test]
	public async Task ViewState_Saves_The_State_With_The_File()
	{
		// Arrange
		DatasetEditorState state = new()
		{
			TopRecordIndex = TopRecordIndex,
			WithinRecordOffset = WithinRecordOffset
		};

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents contents = new()
			{
				Contents = [],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		string? reported = null;

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(
			fileId: Guid.NewGuid(),
			setEditorStateCallback: x => reported = x));

		await sut.ContainerLoaded();

		// Act
		sut.ViewState = state;

		// Assert
		reported
			.Should()
			.NotBeNull();

		new SystemTextJsonSerializer().Deserialize<DatasetEditorState>(reported!)
			.Should()
			.Be(state);

		await dbAccess
			.Received(1)
			.UpdateFileEditorStateAsync(sut.FileId, reported!, Arg.Any<CancellationToken>());
	}
	#endregion

	#region Helpers
	/// <summary>
	/// The commands that change the records, each case named after its command.
	/// </summary>
	private static IEnumerable<TestCaseData> EditCommands()
	{
		yield return Case(static x => x.AddGroupCommand, nameof(DatasetEditorViewModel.AddGroupCommand));

		yield return Case(static x => x.AddKeyValueCommand, nameof(DatasetEditorViewModel.AddKeyValueCommand));

		yield return Case(static x => x.AddValueCommand, nameof(DatasetEditorViewModel.AddValueCommand));

		yield return Case(static x => x.DeleteGroupCommand, nameof(DatasetEditorViewModel.DeleteGroupCommand));

		yield return Case(static x => x.DeleteKeyValueRecordCommand, nameof(DatasetEditorViewModel.DeleteKeyValueRecordCommand));

		yield return Case(static x => x.DeleteValueRecordCommand, nameof(DatasetEditorViewModel.DeleteValueRecordCommand));

		yield return Case(static x => x.EditKeyValueCommand, nameof(DatasetEditorViewModel.EditKeyValueCommand));

		yield return Case(static x => x.EditNoteCommand, nameof(DatasetEditorViewModel.EditNoteCommand));

		yield return Case(static x => x.EditValueCommand, nameof(DatasetEditorViewModel.EditValueCommand));

		yield return Case(
			static x => x.GroupExpandedCollapsedByUserCommand,
			nameof(DatasetEditorViewModel.GroupExpandedCollapsedByUserCommand));

		yield return Case(static x => x.RenameGroupCommand, nameof(DatasetEditorViewModel.RenameGroupCommand));

		static TestCaseData Case(Func<DatasetEditorViewModel, ICommand> command, string name)
		{
			return new TestCaseData(command).SetArgDisplayNames(name);
		}
	}
	#endregion

	#region Nested Types
	/// <summary>
	/// Record of a kind that the sort does not know.
	/// </summary>
	private sealed class ForeignRecord : DatasetRecordBase;
	#endregion
}
