using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.ViewModels;
using Shared.Common;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(KeyValueInputViewModel)}"" type")]
internal class KeyValueInputViewModelTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="KeyValueInputViewModel.Initialize" />.
	/// </summary>
	[Test]
	public void Initialize_Initializes_Properties()
	{
		// Arrange
		const int length = 10;

		string defaultButtonText = AppUtils.CreateRandomString(length);

		string key = AppUtils.CreateRandomString(length);

		string keyHint = AppUtils.CreateRandomString(length);

		string value = AppUtils.CreateRandomString(length);

		string valueHint = AppUtils.CreateRandomString(length);

		using AutoMock mock = AutoMock.GetLoose();

		KeyValueInputViewModel sut = mock.Create<KeyValueInputViewModel>();

		// Act
		sut.Initialize(
			defaultButtonText: defaultButtonText,
			key: key,
			keyHint: keyHint,
			value: value,
			valueHint: valueHint);

		// Assert
		sut.DefaultButtonText
			.Should()
			.Be(defaultButtonText);

		sut.Key
			.Should()
			.Be(key);

		sut.KeyHint
			.Should()
			.Be(keyHint);

		sut.Value
			.Should()
			.Be(value);

		sut.ValueHint
			.Should()
			.Be(valueHint);

		sut.IsValueInputVisible
			.Should()
			.BeTrue();
	}
	#endregion
}
