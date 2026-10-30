using AppLifecycle.Services;
using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.ViewModels.Hierarchy;
using Hierarchy.UnitTests.Factories;

namespace DataOrganizer.UnitTests.ViewModels.Hierarchy;

[TestFixture(Description = $@"Tests of ""{nameof(FileNodeViewModel)}"" type")]
internal class FileNodeViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="FileNodeViewModel.Children" />: a file holds no rows.
	/// </summary>
	[Test]
	public void Children_Is_Empty()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(ItemDtoFactory.CreateFileDto());

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		// Act
		FileNodeViewModel sut = mock.Create<FileNodeViewModel>();

		// Assert
		sut.Children
			.Should()
			.BeEmpty();
	}
	#endregion
}
