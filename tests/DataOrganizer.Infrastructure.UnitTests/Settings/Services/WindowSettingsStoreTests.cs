using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Infrastructure.AppInstance.Interfaces;
using DataOrganizer.Infrastructure.Settings.Dto;
using DataOrganizer.Infrastructure.Settings.Interfaces;
using DataOrganizer.Infrastructure.Settings.Services;
using DataOrganizer.TestSupport.Common;
using NSubstitute;

namespace DataOrganizer.Infrastructure.UnitTests.Settings.Services;

[TestFixture(Description = $@"Tests of ""{nameof(WindowSettingsStore)}"" type")]
internal class WindowSettingsStoreTests
{
	#region Data
	/// <summary>
	/// Path of the file the settings of the copy history are kept in.
	/// </summary>
	private static readonly string CopyHistorySettingsPath = TestPath.Combine(
		"Settings",
		$"{nameof(CopyHistoryViewSettings)}.json");
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="WindowSettingsStore.Read{T}" />: the settings come from the file named after their type.
	/// </summary>
	[Test]
	public void Read_Reads_The_File_Named_After_The_Settings()
	{
		// Arrange
		CopyHistoryViewSettings settings = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.GetSettingsFilePath(nameof(CopyHistoryViewSettings))
				.Returns(CopyHistorySettingsPath);

			IJsonFileStore jsonFileStore = Substitute.For<IJsonFileStore>();

			jsonFileStore
				.Read<CopyHistoryViewSettings>(CopyHistorySettingsPath)
				.Returns(settings);

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance(jsonFileStore);
		});

		WindowSettingsStore sut = mock.Create<WindowSettingsStore>();

		// Act
		CopyHistoryViewSettings? result = sut.Read<CopyHistoryViewSettings>();

		// Assert
		result
			.Should()
			.BeSameAs(settings);
	}

	/// <summary>
	/// <see cref="WindowSettingsStore.Write{T}" />: the settings go over the file named after their type.
	/// </summary>
	[Test]
	public void Write_Writes_The_File_Named_After_The_Settings()
	{
		// Arrange
		CopyHistoryViewSettings settings = new();

		IJsonFileStore jsonFileStore = Substitute.For<IJsonFileStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.GetSettingsFilePath(nameof(CopyHistoryViewSettings))
				.Returns(CopyHistorySettingsPath);

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance(jsonFileStore);
		});

		WindowSettingsStore sut = mock.Create<WindowSettingsStore>();

		// Act
		sut.Write(settings);

		// Assert
		jsonFileStore
			.Received(1)
			.Write(settings, CopyHistorySettingsPath);
	}
	#endregion
}
