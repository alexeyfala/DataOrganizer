using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using System;
using System.Diagnostics;

namespace CommonTestHelpers.Attributes;

/// <summary>
/// Marks a test as ignored when a debugger is attached. Useful for tests that intentionally
/// trigger first-chance exceptions (e.g. asserting <see cref="Exception" /> is thrown), which
/// would otherwise interrupt batch debug runs (<c>Debug All Tests</c>) due to the
/// <c>Break When Thrown</c> setting in Visual Studio's Exception Settings.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class SkipUnderDebuggerAttribute : NUnitAttribute, IApplyToTest
{
	#region Properties
	/// <summary>
	/// Reason shown in Test Explorer when the test is skipped.
	/// </summary>
	public string Reason { get; init; } = "Skipped under debugger to avoid first-chance exception break.";
	#endregion

	#region Methods
	/// <inheritdoc />
	public void ApplyToTest(Test test)
	{
		if (!Debugger.IsAttached)
		{
			return;
		}

		test.RunState = RunState.Ignored;

		test.Properties.Set(PropertyNames.SkipReason, Reason);
	}
	#endregion
}
