using AwesomeAssertions;
using DataOrganizer.Abstract;
using NSubstitute;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(DefaultButtonViewModelBase)}"" type")]
internal class DefaultButtonViewModelBaseTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="DefaultButtonViewModelBase.DefaultPressedCallback" />.
	/// </summary>
	[Test]
	public void DefaultButtonViewModelBase_Resets_Callback_When_DefaultPressedCommand_Has_Executed()
	{
		// Arrange
		DefaultButtonViewModelBase sut = Substitute.For<DefaultButtonViewModelBase>();

		sut.DefaultPressedCallback = () => Task.CompletedTask;

		// Act
		sut
			.DefaultPressedCommand
			.Execute(null);

		// Assert
		sut.DefaultPressedCallback
			.Should()
			.BeNull();
	}
	#endregion
}
