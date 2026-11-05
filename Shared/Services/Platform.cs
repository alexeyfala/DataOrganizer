using Shared.Enums;
using Shared.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Shared.Services;

/// <inheritdoc cref="IPlatform" />
[ExcludeFromCodeCoverage(Justification = "Depends on the operating system the tests run on")]
public sealed class Platform : IPlatform
{
	#region Properties
	/// <inheritdoc />
	public OperatingSystemKind CurrentOS
	{
		get
		{
			if (IsWindows)
			{
				return OperatingSystemKind.Windows;
			}

			if (IsLinux)
			{
				return OperatingSystemKind.Linux;
			}

			return IsMacOS
				? OperatingSystemKind.MacOS
				: OperatingSystemKind.Unknown;
		}
	}

	/// <inheritdoc />
	public bool IsLinux => OperatingSystem.IsLinux();

	/// <inheritdoc />
	public bool IsMacOS => OperatingSystem.IsMacOS();

	/// <inheritdoc />
	public bool IsWindows => OperatingSystem.IsWindows();
	#endregion
}
