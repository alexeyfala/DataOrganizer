using AwesomeAssertions;
using DialogContracts.Constants;
using DialogContracts.Dto;

namespace DialogContracts.UnitTests.Dto;

[TestFixture(Description = $@"Tests of ""{nameof(KeyValueInputParameters)}"" type")]
internal class KeyValueInputParametersTests
{
	#region Methods
	/// <summary>
	/// <see cref="KeyValueInputParameters.DialogHostIdentifier" />: a dialog that sets no host of its own is shown in the
	/// host of the main window.
	/// </summary>
	[Test]
	public void DialogHostIdentifier_Defaults_To_The_Main_Host()
	{
		// Act
		KeyValueInputParameters sut = new()
		{
			DefaultButtonText = "OK"
		};

		// Assert
		sut.DialogHostIdentifier
			.Should()
			.Be(DialogHostIdentifiers.Main);
	}
	#endregion
}
