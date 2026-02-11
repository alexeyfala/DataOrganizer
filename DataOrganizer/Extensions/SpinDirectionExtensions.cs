using Avalonia.Controls;
using Shared.Extensions;
using System;
using System.Linq.Expressions;

namespace DataOrganizer.Extensions;

internal static class SpinDirectionExtensions
{
	#region Methods
	/// <summary>
	/// Increases/decreases value for <paramref name="property"/>.
	/// </summary>
	public static void IncreaseDecrease(
		this SpinDirection direction,
		in double currentValue,
		Expression<Func<double>> property)
	{
		const double step = 0.5;

		double value = direction switch
		{
			SpinDirection.Increase => currentValue + step,
			SpinDirection.Decrease => currentValue - step,
			_ => throw new NotImplementedException()
		};

		if (value < 6.0 || value > 64.0)
		{
			return;
		}

		property.SetValue(value);
	}
	#endregion
}
