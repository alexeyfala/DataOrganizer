using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.Abstract;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.Models;
using DataOrganizer.UnitTests.Helpers;
using DataOrganizer.ViewModels;
using Entities.Models;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;
using Repository.DTO;
using Repository.Interfaces;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(DatasetEditorViewModel)}"" type")]
internal class DatasetEditorViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="DatasetEditorViewModel.AddGroupAsync" />: adds a group to the parent group (expanding it) or to the root records and persists the change.
	/// </summary>
	[Test]
	public async Task AddGroupAsync_Adds_Group([Values] bool addToGroup)
	{
		// Arrange
		string name = AppUtils.CreateRandomString(10);

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

			serializer
				.Serialize(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(AppUtils.CreateRandomString(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);
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

		await dbAccess.Received().UpdateFilePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.AddKeyValueAsync" />: adds a key-value record to the parent group (expanding it) or to the root records and persists the change.
	/// </summary>
	[Test]
	public async Task AddKeyValueAsync_Adds_Key_And_Value_Record([Values] bool addToGroup)
	{
		// Arrange
		string key = AppUtils.CreateRandomString(10);

		string value = key;

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

			serializer
				.Serialize(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(AppUtils.CreateRandomString(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);
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

		await dbAccess.Received().UpdateFilePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.AddValueAsync" />: adds a value record to the parent group (expanding it) or to the root records and persists the change.
	/// </summary>
	[Test]
	public async Task AddValueAsync_Adds_Value_Record([Values] bool addToGroup)
	{
		// Arrange
		string value = AppUtils.CreateRandomString(10);

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

			serializer
				.Serialize(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(AppUtils.CreateRandomString(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);
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

		await dbAccess.Received().UpdateFilePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ContainerLoaded" />: deserializes the file contents and populates the records, marking the view model initialized.
	/// </summary>
	[Test]
	public async Task ContainerLoaded_Adds_Records()
	{
		// Arrange
		DatasetRecordBase[] records = [.. DbAccessExtensions.CreateRandomRecords()];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ContentsIsValidPair pair = new()
			{
				Contents = TestUtils.CreateRandomBytes(10),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(pair);

			IJsonSerializerWrapper jsonSerializer = Substitute.For<IJsonSerializerWrapper>();

			jsonSerializer
				.Deserialize<DatasetRecordBase[]>(Arg.Any<string>())
				.Returns(records);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(jsonSerializer);
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		// Act
		await sut.ContainerLoaded(null);

		// Assert
		sut.IsInitialized
			.Should()
			.BeTrue();

		sut.Records
			.Should()
			.Contain(records);
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ContainerLoaded" />: does not attempt deserialization when the file contents are empty, but still marks the view model initialized.
	/// </summary>
	[Test]
	public async Task ContainerLoaded_Shoud_Not_Tries_Add_Records_If_File_Content_Is_Empty()
	{
		// Arrange
		IJsonSerializerWrapper jsonSerializer = Substitute.For<IJsonSerializerWrapper>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ContentsIsValidPair pair = new()
			{
				Contents = [],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(pair);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(jsonSerializer);
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		// Act
		await sut.ContainerLoaded(null);

		// Assert
		sut.IsInitialized
			.Should()
			.BeTrue();

		jsonSerializer
			.DidNotReceive()
			.Deserialize<DatasetRecordBase[]>(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.DeleteRecordAsync" />: removes the given record from the collection and persists the change.
	/// </summary>
	[Test]
	public async Task DeleteRecordAsync_Deletes_Record()
	{
		// Arrange
		DatasetRecordBase[] records = [.. DbAccessExtensions.CreateRandomRecords()];

		DatasetRecordBase toBeDeleted = records[0];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

			serializer
				.Serialize(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(AppUtils.CreateRandomString(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);
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

		await dbAccess.Received().UpdateFilePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.EditKeyValueAsync" />: updates the record's key and value, persisting only when the values actually changed.
	/// </summary>
	[Test]
	public async Task EditKeyValueAsync_Edits_Record([Values] bool isSameValue)
	{
		// Arrange
		string key = AppUtils.CreateRandomString(10);

		string value = AppUtils.CreateRandomString(10);

		KeyValueRecord target = new();

		if (isSameValue)
		{
			target.Key = key;

			target.Value = value;
		}

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

			serializer
				.Serialize(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(AppUtils.CreateRandomString(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);
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

		await dbAccess.Received(isSameValue ? 0 : 1).UpdateFilePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.EditNoteAsync" />: updates the record's note and persists the change.
	/// </summary>
	[Test]
	public async Task EditNoteAsync_Edits_Note_Of_Record()
	{
		// Arrange
		string note = AppUtils.CreateRandomString(10);

		ValueRecord target = new();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

			serializer
				.Serialize(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(AppUtils.CreateRandomString(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		// Act
		await sut.EditNoteAsync(target, note);

		// Assert
		target.Note
			.Should()
			.Be(note);

		await dbAccess.Received().UpdateFilePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.EditValueAsync" />: updates the record's value, persisting only when the value actually changed.
	/// </summary>
	[Test]
	public async Task EditValueAsync_Edits_Record([Values] bool isSameValue)
	{
		// Arrange
		string value = AppUtils.CreateRandomString(10);

		ValueRecord target = new();

		if (isSameValue)
		{
			target.Value = value;
		}

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

			serializer
				.Serialize(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(AppUtils.CreateRandomString(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		// Act
		await sut.EditValueAsync(target, value);

		// Assert
		target.Value
			.Should()
			.Be(value);

		await dbAccess.Received(isSameValue ? 0 : 1).UpdateFilePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ExpandCollapseAsync" />: sets the expanded state of all groups (in a group or at the root) and persists only when not read-only.
	/// </summary>
	[Test]
	public async Task ExpandCollapseAsync_Tests(
		[Values] bool expand,
		[Values] bool isReadOnly,
		[Values] bool inGroup)
	{
		// Arrange
		RecordsGroup[] groups = [.. DbAccessExtensions
			.CreateGroups(50)
			.ToArray()
			.ForEach(x => x.IsExpanded = !expand)];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

			serializer
				.Serialize(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(AppUtils.CreateRandomString(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IDispatcherAccessor>(new InlineDispatcherAccessor());
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut.IsReadOnly = isReadOnly;

		RecordsGroup? group = null;

		if (inGroup)
		{
			group = new();

			group
				.Children
				.AddRange(groups);
		}
		else
		{
			sut
				.Records
				.AddRange(groups);
		}

		// Act
		await sut.ExpandCollapseAsync(group, expand);

		// Assert
		if (group is not null)
		{
			group.Children
				.Should()
				.Contain(groups);
		}
		else
		{
			sut.Records
				.Should()
				.Contain(groups);
		}

		groups
			.Should()
			.OnlyContain(x => x.IsExpanded == expand);

		await dbAccess.Received(isReadOnly ? 0 : 1).UpdateFilePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.IsHiddenChanged" />: persists the content only when not read-only.
	/// </summary>
	[Test]
	public async Task IsHiddenChanged_Saves_Content_Or_Not([Values] bool isReadOnly)
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

			serializer
				.Serialize(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(AppUtils.CreateRandomString(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut.IsReadOnly = isReadOnly;

		// Act
		await sut.IsHiddenChanged();

		// Assert
		await dbAccess.Received(isReadOnly ? 0 : 1).UpdateFilePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.RenameGroupAsync" />: updates the group's name, persisting only when the name actually changed.
	/// </summary>
	[Test]
	public async Task RenameGroupAsync_Renames_Group([Values] bool isSameValue)
	{
		// Arrange
		string name = AppUtils.CreateRandomString(10);

		RecordsGroup group = new();

		if (isSameValue)
		{
			group.Name = name;
		}

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

			serializer
				.Serialize(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(AppUtils.CreateRandomString(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		// Act
		await sut.RenameGroupAsync(group, name);

		// Assert
		group.Name
			.Should()
			.Be(name);

		await dbAccess.Received(isSameValue ? 0 : 1).UpdateFilePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.ShowHideAsync" />: sets the hidden state of all records (in a group or at the root) and persists only when not read-only.
	/// </summary>
	[Test]
	public async Task ShowHideAsync_Tests(
		[Values] bool hide,
		[Values] bool isReadOnly,
		[Values] bool inGroup)
	{
		// Arrange
		const int count = 50;

		ValueRecord[] records = [.. DbAccessExtensions
			.CreateValueRecords(count)
			.Concat(DbAccessExtensions.CreateKeyValueRecords(count))
			.ToArray()
			.ForEach(x => x.IsHidden = !hide)];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

			serializer
				.Serialize(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(AppUtils.CreateRandomString(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut.IsReadOnly = isReadOnly;

		RecordsGroup? group = null;

		if (inGroup)
		{
			group = new();

			group
				.Children
				.AddRange(records);
		}
		else
		{
			sut
				.Records
				.AddRange(records);
		}

		// Act
		await sut.ShowHideAsync(group, hide);

		// Assert
		if (group is not null)
		{
			group.Children
				.Should()
				.Contain(records);
		}
		else
		{
			sut.Records
				.Should()
				.Contain(records);
		}

		records
			.Should()
			.OnlyContain(x => x.IsHidden == hide);

		await dbAccess.Received(isReadOnly ? 0 : 1).UpdateFilePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>());
	}

	/// <summary>
	/// <see cref="DatasetEditorViewModel.SortAsync" />: sorts values, key-values and groups in the given direction (in a group or at the root) and persists only when not read-only.
	/// </summary>
	[Test]
	public async Task SortAsync(
		[Values] ListSortDirection direction,
		[Values] bool isReadOnly,
		[Values] bool inGroup)
	{
		// Arrange
		DatasetRecordBase[] records = [.. DbAccessExtensions.CreateRandomRecords(eachTypes: 5)];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

			serializer
				.Serialize(Arg.Any<ObservableCollection<DatasetRecordBase>>())
				.Returns(AppUtils.CreateRandomString(10));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);
		});

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>();

		sut.IsReadOnly = isReadOnly;

		RecordsGroup? group = null;

		if (inGroup)
		{
			group = new();

			group
				.Children
				.AddRange(records);
		}
		else
		{
			sut
				.Records
				.AddRange(records);
		}

		// Act
		await sut.SortAsync(group, direction);

		// Assert
		ObservableCollection<DatasetRecordBase> collection = group is not null
			? group.Children
			: sut.Records;

		if (direction == ListSortDirection.Ascending)
		{
			collection.OfSpecificType<DatasetRecordBase, ValueRecord>()
				.Should()
				.BeInAscendingOrder(x => x.Value);

			collection.OfSpecificType<DatasetRecordBase, KeyValueRecord>()
				.Should()
				.BeInAscendingOrder(x => x.Key);

			collection.OfType<RecordsGroup>()
				.Should()
				.BeInAscendingOrder(x => x.Name);
		}
		else if (direction == ListSortDirection.Descending)
		{
			collection.OfSpecificType<DatasetRecordBase, ValueRecord>()
				.Should()
				.BeInDescendingOrder(x => x.Value);

			collection.OfSpecificType<DatasetRecordBase, KeyValueRecord>()
				.Should()
				.BeInDescendingOrder(x => x.Key);

			collection.OfType<RecordsGroup>()
				.Should()
				.BeInDescendingOrder(x => x.Name);
		}

		await dbAccess.Received(isReadOnly ? 0 : 1).UpdateFilePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>());
	}
	#endregion
}
