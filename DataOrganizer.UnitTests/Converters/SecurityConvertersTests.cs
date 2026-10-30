using Avalonia;
using Avalonia.Data.Converters;
using AwesomeAssertions;
using DataOrganizer.Converters;
using DataProtection.Helpers;
using System;
using System.Globalization;

namespace DataOrganizer.UnitTests.Converters;

[TestFixture(Description = $@"Tests of ""{nameof(SecurityConverters)}"" type")]
internal class SecurityConvertersTests
{
	#region Methods
	/// <summary>
	/// <see cref="SecurityConverters.AutoLockDelay" />: a delay of zero minutes or less stands for no auto-lock and is named
	/// apart from a real delay.
	/// </summary>
	[Test]
	public void AutoLockDelay_Names_Every_Missing_Delay_Alike()
	{
		// Act
		object? result = Convert(SecurityConverters.AutoLockDelay, -5);

		// Assert
		result
			.Should()
			.Be(Convert(SecurityConverters.AutoLockDelay, 0))
			.And
			.NotBe(Convert(SecurityConverters.AutoLockDelay, 1));
	}

	/// <summary>
	/// <see cref="SecurityConverters.AutoLockDelay" />: a delay is shown with its minutes.
	/// </summary>
	[Test]
	public void AutoLockDelay_Shows_The_Minutes()
	{
		// Act
		object? result = Convert(SecurityConverters.AutoLockDelay, 15);

		// Assert
		result
			.Should()
			.BeOfType<string>()
			.Which
			.Should()
			.Contain("15");
	}

	/// <summary>
	/// <see cref="SecurityConverters.AutoLockIsExpiring" />: the warning starts within the last twenty seconds.
	/// </summary>
	[TestCase(21.0, false)]
	[TestCase(20.0, true)]
	[TestCase(1.0, true)]
	public void AutoLockIsExpiring_Warns_About_The_Last_Seconds(double seconds, bool expected)
	{
		// Act
		object? result = Convert(SecurityConverters.AutoLockIsExpiring, TimeSpan.FromSeconds(seconds));

		// Assert
		result
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="SecurityConverters.AutoLockIsExpiring" />: a countdown that is not running never warns.
	/// </summary>
	[Test]
	public void AutoLockIsExpiring_Without_A_Countdown_Is_False()
	{
		// Act
		object? result = Convert(SecurityConverters.AutoLockIsExpiring, null);

		// Assert
		result
			.Should()
			.Be(false);
	}

	/// <summary>
	/// <see cref="SecurityConverters.AutoLockRemaining" />: an hour or more keeps its hours in the caption.
	/// </summary>
	[Test]
	public void AutoLockRemaining_Keeps_The_Hours_Of_The_Longest_Delay()
	{
		// Act
		object? result = Convert(SecurityConverters.AutoLockRemaining, TimeSpan.FromMinutes(60.0));

		// Assert
		result
			.Should()
			.BeOfType<string>()
			.Which
			.Should()
			.Contain("1:00:00");
	}

	/// <summary>
	/// <see cref="SecurityConverters.AutoLockRemaining" />: the time left is shown as minutes and seconds.
	/// </summary>
	[Test]
	public void AutoLockRemaining_Shows_Minutes_And_Seconds()
	{
		// Act
		object? result = Convert(SecurityConverters.AutoLockRemaining, TimeSpan.FromSeconds(577.0));

		// Assert
		result
			.Should()
			.BeOfType<string>()
			.Which
			.Should()
			.Contain("09:37");
	}

	/// <summary>
	/// <see cref="SecurityConverters.AutoLockRemaining" />: a countdown that is not running has no caption.
	/// </summary>
	[Test]
	public void AutoLockRemaining_Without_A_Countdown_Is_Empty()
	{
		// Act
		object? result = Convert(SecurityConverters.AutoLockRemaining, null);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="SecurityConverters.MaskedValue" />: a hidden value shows the mask of the field, and an open one shows
	/// itself.
	/// </summary>
	[Test]
	public void MaskedValue_Shows_The_Mask_Of_A_Hidden_Value([Values] bool isHidden)
	{
		// Arrange
		const string value = "Secret";

		// Act
		object? result = SecurityConverters.MaskedValue.Convert(
			[value, isHidden],
			typeof(object),
			null,
			CultureInfo.CurrentCulture);

		// Assert
		result
			.Should()
			.Be(SecretMask.Apply(value, isHidden));

		(result as string == value)
			.Should()
			.Be(!isHidden);
	}

	/// <summary>
	/// <see cref="SecurityConverters.MaskedValue" />: nothing is shown until the value and its hidden state are known.
	/// </summary>
	[Test]
	public void MaskedValue_Without_The_Hidden_State_Is_Empty()
	{
		// Act
		object? result = SecurityConverters.MaskedValue.Convert(
			["Secret", AvaloniaProperty.UnsetValue],
			typeof(object),
			null,
			CultureInfo.CurrentCulture);

		// Assert
		result
			.Should()
			.BeNull();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Runs a converter over an auto-lock delay in minutes.
	/// </summary>
	private static object? Convert(FuncValueConverter<int, string> converter, int minutes)
	{
		return converter.Convert(
			minutes,
			typeof(object),
			null,
			CultureInfo.CurrentCulture);
	}

	/// <summary>
	/// Runs a converter over the time left of a countdown.
	/// </summary>
	private static object? Convert(IValueConverter converter, TimeSpan? remaining)
	{
		return converter.Convert(
			remaining,
			typeof(object),
			null,
			CultureInfo.CurrentCulture);
	}
	#endregion
}
