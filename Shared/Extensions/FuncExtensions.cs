using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Extensions;

public static class FuncExtensions
{
	#region Methods
	/// <summary>
	/// Performs an asynchronous delay some number of times until a condition is met.
	/// </summary>
	/// <param name="condition">Condition.</param>
	/// <param name="millisecondsDelay">Delay in milliseconds.</param>
	/// <param name="maxRepeat">Maximum number of repetitions.</param>
	/// <returns><c>True</c> if the condition is met, <c>False</c> if the maximum number of repetitions is reached and the condition is not met.</returns>
	public static async ValueTask<bool> WaitAsync(
		this Func<bool> condition,
		int millisecondsDelay,
		int maxRepeat,
		CancellationToken token = default)
	{
		for (int i = 0; !condition(); i++)
		{
			if (i == maxRepeat)
			{
				return false;
			}

			await Task
				.Delay(millisecondsDelay, token)
				.ConfigureAwait(true);
		}

		return true;
	}
	#endregion Methods
}
