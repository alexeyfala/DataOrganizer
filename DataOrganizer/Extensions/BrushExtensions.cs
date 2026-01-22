using Avalonia.Animation;
using Avalonia.Media;
using Avalonia.Styling;
using Shared.Extensions;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Extensions;

internal static class BrushExtensions
{
	#region Methods
	/// <summary>
	/// Applies a color animation to a property.
	/// </summary>
	public static Task ApplyColorAnimation(
		Expression<Func<Animatable?>> expression,
		Color fromColor,
		Color toColor,
		double duration,
		CancellationToken token = default)
	{
		// The expression may be null, so it is initialized at the beginning.
		expression.SetValue(new SolidColorBrush());

		if (expression.Compile()() is not Animatable target)
		{
			return Task.CompletedTask;
		}

		Animation animation = new()
		{
			Duration = TimeSpan.FromSeconds(duration)
		};

		// Start of the animation.
		animation.Children.Add(new()
		{
			Cue = new(0.0),
			Setters =
			{
				new Setter
				{
					Property = SolidColorBrush.ColorProperty,
					Value = fromColor
				}
			}
		});

		// End of the animation.
		animation.Children.Add(new()
		{
			Cue = new(1.0),
			Setters =
			{
				new Setter
				{
					Property = SolidColorBrush.ColorProperty,
					Value = toColor
				}
			}
		});

		return animation.RunAsync(target, token);
	}

	/// <summary>
	/// Applies a <see cref="Colors.LimeGreen" /> animation with 2.0 seconds duration to a property.
	/// </summary>
	public static Task ApplyLimeGreenColorAnimation(
		Expression<Func<Animatable?>> expression,
		CancellationToken token = default)
	{
		return ApplyColorAnimation(
			expression,
			Colors.LimeGreen,
			Colors.Transparent,
			2.0,
			token);
	}
	#endregion
}
