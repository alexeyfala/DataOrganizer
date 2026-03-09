using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Platform.Storage;
using CommonTestHelpers.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
using DataOrganizer.Windows;
using Entities.Abstract;
using NSubstitute;
using Shared.Interfaces;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(DataExchangeService)}"" type")]
internal class DataExchangeServiceTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="DataExchangeService.ExportDataAsync" />.
	/// </summary>
	[Test]
	public async Task ExportDataAsync_Exports_To_Json()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IJsonSerializerWrapper jsonSerializer = Substitute.For<IJsonSerializerWrapper>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SaveFileAsync<EditorWindow>(Arg.Any<FilePickerSaveOptions>())
				.Returns(TestUtils.CreateRandomFileName(10, IFileSystemPicker.JsonExt));

			builder.RegisterInstance(picker);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(jsonSerializer);
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		await sut.ExportDataAsync();

		// Assert
		fileSystem
			.Received()
			.WriteAllText(Arg.Any<string>(), Arg.Any<string>());

		jsonSerializer
			.Received()
			.Serialize(Arg.Any<ExplorerModelBase[]>(), Arg.Any<JsonSerializerOptions>());
	}
	#endregion
}
