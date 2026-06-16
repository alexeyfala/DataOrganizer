using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Enums;
using DataOrganizer.ViewModels;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(ImportListSelectorViewModel)}"" type")]
internal class ImportListSelectorViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="ImportListSelectorViewModel.ApplyCommand" />: yields the Append result when AddToList is selected.
	/// </summary>
	[Test]
	public async Task ApplyCommand_Sets_Append_Result_When_AddToList_Is_Selected()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ImportListSelectorViewModel sut = mock.Create<ImportListSelectorViewModel>();

		sut.Replace = false;

		sut.AddToList = true;

		// Act
		_ = Task.Run(() => sut.ApplyCommand.Execute(null));

		ImportListVariant result = await sut.GetResultAsync();

		// Assert
		result
			.Should()
			.Be(ImportListVariant.Append);
	}

	/// <summary>
	/// <see cref="ImportListSelectorViewModel.ApplyCommand" />: yields the None result when no option is selected.
	/// </summary>
	[Test]
	public async Task ApplyCommand_Sets_None_Result_When_Nothing_Is_Selected()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ImportListSelectorViewModel sut = mock.Create<ImportListSelectorViewModel>();

		sut.Replace = false;

		sut.AddToList = false;

		// Act
		_ = Task.Run(() => sut.ApplyCommand.Execute(null));

		ImportListVariant result = await sut.GetResultAsync();

		// Assert
		result
			.Should()
			.Be(ImportListVariant.None);
	}

	/// <summary>
	/// <see cref="ImportListSelectorViewModel.ApplyCommand" />: yields the Replace result when Replace is selected.
	/// </summary>
	[Test]
	public async Task ApplyCommand_Sets_Replace_Result_When_Replace_Is_Selected()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ImportListSelectorViewModel sut = mock.Create<ImportListSelectorViewModel>();

		sut.Replace = true;

		// Act
		_ = Task.Run(() => sut.ApplyCommand.Execute(null));

		ImportListVariant result = await sut.GetResultAsync();

		// Assert
		result
			.Should()
			.Be(ImportListVariant.Replace);
	}

	/// <summary>
	/// <see cref="ImportListSelectorViewModel.CancelCommand" />: yields the None result.
	/// </summary>
	[Test]
	public async Task CancelCommand_Sets_None_Result()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ImportListSelectorViewModel sut = mock.Create<ImportListSelectorViewModel>();

		// Act
		_ = Task.Run(() => sut.CancelCommand.Execute(null));

		ImportListVariant result = await sut.GetResultAsync();

		// Assert
		result
			.Should()
			.Be(ImportListVariant.None);
	}
	#endregion
}
