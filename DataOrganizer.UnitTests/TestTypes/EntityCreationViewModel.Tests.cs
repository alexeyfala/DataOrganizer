using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.DTO.Settings;
using DataOrganizer.ViewModels;
using NSubstitute;
using Shared.Common;
using Shared.Interfaces;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(EntityCreationViewModel)}"" type")]
internal class EntityCreationViewModelTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="EntityCreationViewModel" />.
	/// </summary>
	[Test]
	public void Initializes_Properties_In_Constructor()
	{
		// Arrange
		EntityCreationViewSettings settings = new()
		{
			IsDatasetSelected = false,
			IsFileSelected = false,
			IsFolderSelected = true,
			Name = AppUtils.CreateRandomString(10)
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

			serializer
				.FromFile<EntityCreationViewSettings>(Arg.Any<string>())
				.Returns(settings);

			builder.RegisterInstance(serializer);
		});

		// Act
		EntityCreationViewModel sut = mock.Create<EntityCreationViewModel>();

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
	/// Test of <see cref="EntityCreationViewModel.SaveSettingsInFile" />.
	/// </summary>
	[Test]
	public void SaveSettingsInFile_Saves_Settings()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose();

		EntityCreationViewModel sut = mock.Create<EntityCreationViewModel>(TypedParameter.From(fileSystem));

		// Act
		sut.SaveSettingsInFile();

		// Assert
		fileSystem.Received().SerializeToJsonFile(
			Arg.Any<EntityCreationViewSettings>(),
			Arg.Any<string>(),
			Arg.Any<bool>());
	}

	/// <summary>
	/// Test of <see cref="EntityCreationViewModel" />.
	/// </summary>
	[Test]
	public void Selects_Folder_If_Nothing_Selected_In_Constructor()
	{
		// Arrange
		EntityCreationViewSettings settings = new()
		{
			IsDatasetSelected = false,
			IsFileSelected = false,
			IsFolderSelected = false,
			Name = AppUtils.CreateRandomString(10)
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

			serializer
				.FromFile<EntityCreationViewSettings>(Arg.Any<string>())
				.Returns(settings);

			builder.RegisterInstance(serializer);
		});

		// Act
		EntityCreationViewModel sut = mock.Create<EntityCreationViewModel>();

		// Assert
		sut.IsFolderSelected
			.Should()
			.BeTrue();
	}
	#endregion
}
