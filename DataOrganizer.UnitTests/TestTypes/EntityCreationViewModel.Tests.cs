using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.DTO.Settings;
using DataOrganizer.ViewModels;
using Shared.Common;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(EntityCreationViewModel)}"" type")]
internal class EntityCreationViewModelTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="EntityCreationViewModel.Initialize" />.
	/// </summary>
	[Test]
	public void Initialize_Initializes_Properties()
	{
		// Arrange
		EntityCreationViewSettings settings = new()
		{
			IsDatasetSelected = false,
			IsFileSelected = false,
			IsFolderSelected = true,
			Name = AppUtils.CreateRandomString(10)
		};

		using AutoMock mock = AutoMock.GetLoose();

		EntityCreationViewModel sut = mock.Create<EntityCreationViewModel>();

		// Act
		sut.Initialize(settings);

		// Assert
		sut.IsFolderSelected
			.Should()
			.Be(settings.IsFolderSelected);

		sut.IsFileSelected
			.Should()
			.Be(settings.IsFileSelected);

		sut.IsDatasetSelected
			.Should()
			.Be(settings.IsDatasetSelected);

		sut.Name
			.Should()
			.Be(settings.Name);
	}

	/// <summary>
	/// Test of <see cref="EntityCreationViewModel.Initialize" />.
	/// </summary>
	[Test]
	public void Initialize_Selects_Folder_If_Nothing_Selected()
	{
		// Arrange
		EntityCreationViewSettings settings = new()
		{
			IsDatasetSelected = false,
			IsFileSelected = false,
			IsFolderSelected = false,
			Name = AppUtils.CreateRandomString(10)
		};

		using AutoMock mock = AutoMock.GetLoose();

		EntityCreationViewModel sut = mock.Create<EntityCreationViewModel>();

		// Act
		sut.Initialize(settings);

		// Assert
		sut.IsFolderSelected
			.Should()
			.BeTrue();
	}
	#endregion
}
