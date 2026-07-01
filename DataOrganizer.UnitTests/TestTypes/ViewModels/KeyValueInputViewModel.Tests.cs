using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.ViewModels;
using Shared.Common;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(KeyValueInputViewModel)}"" type")]
internal class KeyValueInputViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="KeyValueInputViewModel.CancelCommand" />: executing the command yields a false result.
	/// </summary>
	[Test]
	public async Task CancelCommand_Sets_False_Result()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		KeyValueInputViewModel sut = mock.Create<KeyValueInputViewModel>();

		// Act
		_ = Task.Run(() => sut.CancelCommand.Execute(null));

		bool result = await sut.GetResultAsync();

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="KeyValueInputViewModel.DefaultPressedCommand" />: executing the command with a key set yields a true result.
	/// </summary>
	[Test]
	public async Task DefaultPressedCommand_Sets_True_Result_When_Key_Is_Provided()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		KeyValueInputViewModel sut = mock.Create<KeyValueInputViewModel>();

		sut.Key = "some-key";

		// Act
		_ = Task.Run(() => sut.DefaultPressedCommand.Execute(null));

		bool result = await sut.GetResultAsync();

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="KeyValueInputViewModel.Initialize" />: all view-model properties are set from the provided data.
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
		sut.Initialize(new()
		{
			DefaultButtonText = defaultButtonText,
			Key = key,
			KeyHint = keyHint,
			Value = value,
			ValueHint = valueHint
		});

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

	/// <summary>
	/// <see cref="KeyValueInputViewModel.Initialize" />: maps the key-input mask flag from the parameters.
	/// </summary>
	[Test]
	public void Initialize_Sets_IsKeyMasked([Values] bool maskKeyInput)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		KeyValueInputViewModel sut = mock.Create<KeyValueInputViewModel>();

		// Act
		sut.Initialize(new()
		{
			DefaultButtonText = AppUtils.CreateRandomString(10),
			MaskKeyInput = maskKeyInput
		});

		// Assert
		sut.IsKeyMasked
			.Should()
			.Be(maskKeyInput);
	}
	#endregion
}
