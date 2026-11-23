using NSubstitute;
using NSubstitute.Core;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.SystemIntegration.UnitTests.TestHelpers;

/// <summary>
/// Configures the stubbed calls that return a value task.
/// </summary>
internal static class ValueTaskExtensions
{
	#region Methods
	/// <summary>
	/// Makes a stubbed call answer with the value task a function builds, which may still be running.
	/// </summary>
	public static ConfiguredCall ReturnsValueTask<T>(this ValueTask<T> call, Func<CallInfo, ValueTask<T>> answer) => call.Returns(answer);
	#endregion
}
