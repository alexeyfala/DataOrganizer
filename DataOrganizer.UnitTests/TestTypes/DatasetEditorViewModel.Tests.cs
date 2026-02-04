using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.Abstract;
using DataOrganizer.Models;
using DataOrganizer.ViewModels;
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
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(DatasetEditorViewModel)}"" type")]
internal class DatasetEditorViewModelTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="DatasetEditorViewModel.AddGroupAsync(string, CancellationToken)" />.
	/// </summary>
	[TestCase(true)]
	[TestCase(false)]
	public async Task AddGroupAsync_Adds_Group(bool addToGroup)
	{
		// Arrange
		string name = AppUtils.CreateRandomString(10);

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose();

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>(
			TypedParameter.From(dbAccess),
			TypedParameter.From(CreateSerializerMock()));

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

		await dbAccess.Received().UpdatePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<CancellationToken>(),
			Arg.Any<PropertyNameValuePair[]>());
	}

	/// <summary>
	/// Test of <see cref="DatasetEditorViewModel.AddKeyValueAsync(string, string?, RecordsGroup?, CancellationToken)" />.
	/// </summary>
	[TestCase(true)]
	[TestCase(false)]
	public async Task AddKeyValueAsync_Adds_Key_And_Value_Record(bool addToGroup)
	{
		// Arrange
		string key = AppUtils.CreateRandomString(10);

		string value = key;

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose();

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>(
			TypedParameter.From(dbAccess),
			TypedParameter.From(CreateSerializerMock()));

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

		await dbAccess.Received().UpdatePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<CancellationToken>(),
			Arg.Any<PropertyNameValuePair[]>());
	}

	/// <summary>
	/// Test of <see cref="DatasetEditorViewModel.AddValueAsync(string, RecordsGroup?, CancellationToken)" />.
	/// </summary>
	[TestCase(true)]
	[TestCase(false)]
	public async Task AddValueAsync_Adds_Value_Record(bool addToGroup)
	{
		// Arrange
		string value = AppUtils.CreateRandomString(10);

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose();

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>(
			TypedParameter.From(dbAccess),
			TypedParameter.From(CreateSerializerMock()));

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

		await dbAccess.Received().UpdatePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<CancellationToken>(),
			Arg.Any<PropertyNameValuePair[]>());
	}

	/// <summary>
	/// Test of <see cref="DatasetEditorViewModel.DeleteRecordAsync(DatasetRecordBase, CancellationToken)" />.
	/// </summary>
	[Test]
	public async Task DeleteRecordAsync_Deletes_Record()
	{
		// Arrange
		DatasetRecordBase[] records = [.. DatasetEditorViewModel.CreateRandomRecords()];

		DatasetRecordBase toBeDeleted = records[0];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose();

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>(
			TypedParameter.From(dbAccess),
			TypedParameter.From(CreateSerializerMock()));

		sut
			.Records
			.AddRange(records);

		// Act
		await sut.DeleteRecordAsync(toBeDeleted);

		// Assert
		sut.Records
			.Should()
			.NotContain(toBeDeleted);

		await dbAccess.Received().UpdatePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<CancellationToken>(),
			Arg.Any<PropertyNameValuePair[]>());
	}

	/// <summary>
	/// Test of <see cref="DatasetEditorViewModel.EditKeyValueAsync(KeyValueRecord, string, string?, CancellationToken)" />.
	/// </summary>
	[TestCase(true)]
	[TestCase(false)]
	public async Task EditKeyValueAsync_Edits_Record(bool isSameValue)
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

		using AutoMock mock = AutoMock.GetLoose();

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>(
			TypedParameter.From(dbAccess),
			TypedParameter.From(CreateSerializerMock()));

		// Act
		await sut.EditKeyValueAsync(target, key, value);

		// Assert
		target.Key
			.Should()
			.Be(key);

		target.Value
			.Should()
			.Be(value);

		await dbAccess.Received(isSameValue ? 0 : 1).UpdatePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<CancellationToken>(),
			Arg.Any<PropertyNameValuePair[]>());
	}

	/// <summary>
	/// Test of <see cref="DatasetEditorViewModel.EditNoteAsync(DatasetRecordBase, string?, CancellationToken)" />.
	/// </summary>
	[Test]
	public async Task EditNoteAsync_Edits_Note_Of_Record()
	{
		// Arrange
		string note = AppUtils.CreateRandomString(10);

		ValueRecord target = new();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose();

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>(
			TypedParameter.From(dbAccess),
			TypedParameter.From(CreateSerializerMock()));

		// Act
		await sut.EditNoteAsync(target, note);

		// Assert
		target.Note
			.Should()
			.Be(note);

		await dbAccess.Received().UpdatePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<CancellationToken>(),
			Arg.Any<PropertyNameValuePair[]>());
	}

	/// <summary>
	/// Test of <see cref="DatasetEditorViewModel.EditValueAsync(ValueRecord, string, CancellationToken)" />.
	/// </summary>
	[TestCase(true)]
	[TestCase(false)]
	public async Task EditValueAsync_Edits_Record(bool isSameValue)
	{
		// Arrange
		string value = AppUtils.CreateRandomString(10);

		ValueRecord target = new();

		if (isSameValue)
		{
			target.Value = value;
		}

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose();

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>(
			TypedParameter.From(dbAccess),
			TypedParameter.From(CreateSerializerMock()));

		// Act
		await sut.EditValueAsync(target, value);

		// Assert
		target.Value
			.Should()
			.Be(value);

		await dbAccess.Received(isSameValue ? 0 : 1).UpdatePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<CancellationToken>(),
			Arg.Any<PropertyNameValuePair[]>());
	}

	/// <summary>
	/// Test of <see cref="DatasetEditorViewModel.ExpandCollapseAsync(RecordsGroup?, bool, CancellationToken)" />.
	/// </summary>
	[TestCase(true, true, true)]
	[TestCase(false, false, false)]
	public async Task ExpandCollapseAsync_Tests(
		bool expand,
		bool isReadOnly,
		bool inGroup)
	{
		// Arrange
		RecordsGroup[] groups = [.. DatasetEditorViewModel
			.CreateGroups(50)
			.ToArray()
			.ForEach(x => x.IsExpanded = !expand)];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose();

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>(
			TypedParameter.From(dbAccess),
			TypedParameter.From(CreateSerializerMock()));

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

		await dbAccess.Received(isReadOnly ? 0 : 1).UpdatePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<CancellationToken>(),
			Arg.Any<PropertyNameValuePair[]>());
	}

	/// <summary>
	/// Test of <see cref="DatasetEditorViewModel.IsHiddenChanged" />.
	/// </summary>
	[TestCase(true)]
	[TestCase(false)]
	public async Task IsHiddenChanged_Saves_Content_Or_Not(bool isReadOnly)
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose();

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>(
			TypedParameter.From(dbAccess),
			TypedParameter.From(CreateSerializerMock()));

		sut.IsReadOnly = isReadOnly;

		// Act
		await sut.IsHiddenChanged();

		// Assert
		await dbAccess.Received(isReadOnly ? 0 : 1).UpdatePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<CancellationToken>(),
			Arg.Any<PropertyNameValuePair[]>());
	}

	/// <summary>
	/// Test of <see cref="DatasetEditorViewModel.LoadDataAsync" />.
	/// </summary>
	[Test]
	public async Task LoadDataAsync_Adds_Records()
	{
		// Arrange
		DatasetRecordBase[] records = [.. DatasetEditorViewModel.CreateRandomRecords()];

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
		await sut.LoadDataAsync();

		// Assert
		sut.IsInitialized
			.Should()
			.BeTrue();

		sut.Records
			.Should()
			.Contain(records);
	}

	/// <summary>
	/// Test of <see cref="DatasetEditorViewModel.LoadDataAsync" />.
	/// </summary>
	[Test]
	public async Task LoadDataAsync_Shoud_Not_Tries_Add_Records_If_File_Content_Is_Empty()
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
		await sut.LoadDataAsync();

		// Assert
		sut.IsInitialized
			.Should()
			.BeTrue();

		jsonSerializer
			.Received(0)
			.Deserialize<DatasetRecordBase[]>(Arg.Any<string>());
	}

	/// <summary>
	/// Test of <see cref="DatasetEditorViewModel.RenameGroupAsync(RecordsGroup, string, CancellationToken)" />.
	/// </summary>
	[TestCase(true)]
	[TestCase(false)]
	public async Task RenameGroupAsync_Renames_Group(bool isSameValue)
	{
		// Arrange
		string name = AppUtils.CreateRandomString(10);

		RecordsGroup group = new();

		if (isSameValue)
		{
			group.Name = name;
		}

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose();

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>(
			TypedParameter.From(dbAccess),
			TypedParameter.From(CreateSerializerMock()));

		// Act
		await sut.RenameGroupAsync(group, name);

		// Assert
		group.Name
			.Should()
			.Be(name);

		await dbAccess.Received(isSameValue ? 0 : 1).UpdatePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<CancellationToken>(),
			Arg.Any<PropertyNameValuePair[]>());
	}

	/// <summary>
	/// Test of <see cref="DatasetEditorViewModel.ShowHideAsync(RecordsGroup?, bool, CancellationToken)" />.
	/// </summary>
	[TestCase(true, true, true)]
	[TestCase(false, false, false)]
	public async Task ShowHideAsync_Tests(
		bool hide,
		bool isReadOnly,
		bool inGroup)
	{
		// Arrange
		const int count = 50;

		ValueRecord[] records = [.. DatasetEditorViewModel
			.CreateValueRecords(count)
			.Concat(DatasetEditorViewModel.CreateKeyValueRecords(count))
			.ToArray()
			.ForEach(x => x.IsHidden = !hide)];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose();

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>(
			TypedParameter.From(dbAccess),
			TypedParameter.From(CreateSerializerMock()));

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

		await dbAccess.Received(isReadOnly ? 0 : 1).UpdatePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<CancellationToken>(),
			Arg.Any<PropertyNameValuePair[]>());
	}

	/// <summary>
	/// Test of <see cref="DatasetEditorViewModel.SortAsync(RecordsGroup?, ListSortDirection, CancellationToken)" />.
	/// </summary>
	[TestCase(ListSortDirection.Ascending, true, true)]
	[TestCase(ListSortDirection.Descending, false, false)]
	public async Task SortAsync(
		ListSortDirection direction,
		bool isReadOnly,
		bool inGroup)
	{
		// Arrange
		DatasetRecordBase[] records = [.. DatasetEditorViewModel.CreateRandomRecords(eachTypes: 5)];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose();

		using DatasetEditorViewModel sut = mock.Create<DatasetEditorViewModel>(
			TypedParameter.From(dbAccess),
			TypedParameter.From(CreateSerializerMock()));

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

		await dbAccess.Received(isReadOnly ? 0 : 1).UpdatePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<CancellationToken>(),
			Arg.Any<PropertyNameValuePair[]>());
	}
	#endregion

	#region Service
	/// <summary>
	/// Creates <see cref="IJsonSerializerWrapper" /> that returns a random string.
	/// </summary>
	private static IJsonSerializerWrapper CreateSerializerMock()
	{
		IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

		serializer
			.Serialize(Arg.Any<ObservableCollection<DatasetRecordBase>>())
			.Returns(AppUtils.CreateRandomString(10));

		return serializer;
	}
	#endregion
}
