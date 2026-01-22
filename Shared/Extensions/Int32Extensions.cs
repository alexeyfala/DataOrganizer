using System;

namespace Shared.Extensions;

public static class Int32Extensions
{
	#region Methods
	/// <summary>
	/// Returns the initial part of a number.
	/// </summary>
	/// <param name="value">Initial number.</param>
	/// <param name="count">The required number of digits.</param>
	public static int TakeDigits(this int value, int count)
	{
		value = Math.Abs(value);

		if (value == 0)
		{
			return value;
		}

		int digits = (int)Math.Floor(Math.Log10(value) + 1);

		return digits >= count
			? (int)Math.Truncate(value / Math.Pow(10, digits - count))
			: value;
	}
	#endregion
}
