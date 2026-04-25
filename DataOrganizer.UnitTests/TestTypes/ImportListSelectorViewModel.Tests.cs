using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Enums;
using DataOrganizer.ViewModels;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ImportListSelectorViewModel)}"" type")]
internal class ImportListSelectorViewModelTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="ImportListSelectorViewModel.ApplyCommand" />.
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
	/// Test of <see cref="ImportListSelectorViewModel.ApplyCommand" />.
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
	/// Test of <see cref="ImportListSelectorViewModel.ApplyCommand" />.
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
	/// Test of <see cref="ImportListSelectorViewModel.CancelCommand" />.
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
