using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.ViewModels.Dialogs;
using NSubstitute;
using Settings.Dto;
using Settings.Interfaces;
using Shared.Common;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.ViewModels.Dialogs;

[TestFixture(Description = $@"Tests of ""{nameof(EntityCreationViewModel)}"" type")]
internal class EntityCreationViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="EntityCreationViewModel.CancelCommand" />: executing it completes the result with <c>false</c>.
	/// </summary>
	[Test]
	public async Task CancelCommand_Sets_False_Result()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EntityCreationViewModel sut = mock.Create<EntityCreationViewModel>();

		// Act
		_ = Task.Run(() => sut.CancelCommand.Execute(null));

		bool result = await sut.GetResultAsync(new TaskCompletionSource().Task);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="EntityCreationViewModel" />: the constructor initializes its properties from the loaded settings.
	/// </summary>
	[Test]
	public void Constructor_Initializes_Properties()
	{
		// Arrange
		EntityCreationViewSettings settings = new()
		{
			IsDatasetSelected = false,
			IsFileSelected = false,
			IsFolderSelected = true,
			Name = RandomString.Create(10)
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IWindowSettingsStore windowSettings = Substitute.For<IWindowSettingsStore>();

			windowSettings
				.Read<EntityCreationViewSettings>()
				.Returns(settings);

			builder.RegisterInstance(windowSettings);
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
	/// <see cref="EntityCreationViewModel" />: the constructor selects the folder option when the loaded settings have nothing selected.
	/// </summary>
	[Test]
	public void Constructor_Selects_Folder_If_Nothing_Selected()
	{
		// Arrange
		EntityCreationViewSettings settings = new()
		{
			IsDatasetSelected = false,
			IsFileSelected = false,
			IsFolderSelected = false,
			Name = RandomString.Create(10)
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IWindowSettingsStore windowSettings = Substitute.For<IWindowSettingsStore>();

			windowSettings
				.Read<EntityCreationViewSettings>()
				.Returns(settings);

			builder.RegisterInstance(windowSettings);
		});

		// Act
		EntityCreationViewModel sut = mock.Create<EntityCreationViewModel>();

		// Assert
		sut.IsFolderSelected
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="EntityCreationViewModel.SaveSettingsToFile" />: stores the choice and the name the dialog shows.
	/// </summary>
	[Test]
	public void SaveSettingsToFile_Saves_Settings()
	{
		// Arrange
		IWindowSettingsStore windowSettings = Substitute.For<IWindowSettingsStore>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(windowSettings));

		EntityCreationViewModel sut = mock.Create<EntityCreationViewModel>();

		sut.IsFileSelected = true;

		sut.Name = RandomString.Create(10);

		// Act
		sut.SaveSettingsToFile();

		// Assert
		windowSettings.Received(1).Write(new EntityCreationViewSettings
		{
			IsDatasetSelected = sut.IsDatasetSelected,
			IsFileSelected = true,
			IsFolderSelected = sut.IsFolderSelected,
			Name = sut.Name
		});
	}
	#endregion
}
