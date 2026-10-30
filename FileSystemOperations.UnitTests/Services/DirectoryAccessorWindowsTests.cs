using Autofac;
using Autofac.Extras.Moq;
using FileSystemOperations.Interfaces;
using FileSystemOperations.Services;
using NSubstitute;
using Shared.Interfaces;
using System;
using System.IO;
using System.Runtime.Versioning;

namespace FileSystemOperations.UnitTests.Services;

[SupportedOSPlatform("windows")]
[TestFixture(Description = $@"Tests of ""{nameof(DirectoryAccessor)}"" type on Windows")]
internal class DirectoryAccessorWindowsTests
{
	#region Methods
	/// <summary>
	/// <see cref="DirectoryAccessor.OpenAppDirectory" />: the running program is revealed in its folder.
	/// </summary>
	[Test]
	public void OpenAppDirectory_Reveals_The_Running_Program()
	{
		// Arrange
		string processPath = Environment.ProcessPath!;

		IExplorerManager explorerManager = Substitute.For<IExplorerManager>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.FileExists(processPath)
				.Returns(true);

			// No real file manager may start from a test.
			explorerManager
				.TryForegroundFolder(Arg.Any<string>(), Arg.Any<string?>())
				.Returns(true);

			builder.RegisterInstance(explorerManager);

			builder.RegisterInstance(fileSystem);
		});

		DirectoryAccessor sut = mock.Create<DirectoryAccessor>();

		// Act
		sut.OpenAppDirectory();

		// Assert
		explorerManager
			.Received(1)
			.TryForegroundFolder(Path.GetDirectoryName(processPath)!, processPath);
	}
	#endregion
}
