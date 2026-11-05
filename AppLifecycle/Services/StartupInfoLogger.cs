using AppInstance.Interfaces;
using AppLifecycle.Interfaces;
using Cysharp.Text;
using Serilog;
using Settings.Interfaces;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using OSVersion = OSVersionExtension.OSVersion;

namespace AppLifecycle.Services;

/// <inheritdoc cref="IStartupInfoLogger" />
[ExcludeFromCodeCoverage(Justification = "Only writes to the log")]
public sealed class StartupInfoLogger : IStartupInfoLogger
{
	#region Data
	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="ICommandLineOptions" />
	private readonly ICommandLineOptions _options;

	/// <inheritdoc cref="IPlatform" />
	private readonly IPlatform _platform;

	/// <inheritdoc cref="IAppSettingsStore" />
	private readonly IAppSettingsStore _settingsStore;
	#endregion

	#region Constructors
	public StartupInfoLogger(
		IAppSettingsStore settingsStore,
		ICommandLineOptions options,
		ILogger logger,
		IPlatform platform)
	{
		_logger = logger;

		_options = options;

		_platform = platform;

		_settingsStore = settingsStore;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Log()
	{
		if (_options.PrintHelp)
		{
			_logger.LogInformationWithTemplate(_options.GetHelp());
		}

		_logger.LogInformationWithTemplate(
			$"{AppInfo.AppDisplayName} ({Assembly.GetEntryAssembly().GetVersionWithSuffix()})");

		using Utf16ValueStringBuilder builder = ZString.CreateStringBuilder();

		builder.AppendLine("System specifications:");

		const string os = "OS";

		builder.AppendLine($"{os} platform - {Environment.OSVersion.Platform}");

		if (_platform.IsMacOS)
		{
			builder.AppendLine($"{os} type - macOS {Environment.OSVersion.Version}");
		}

		if (_platform.IsLinux)
		{
			builder.AppendLine($"{os} type - Linux {Environment.OSVersion.Version}");
		}

		if (_platform.IsWindows)
		{
			builder.AppendLine($"{os} type - {OSVersion.GetOperatingSystem()} {OSVersion.GetOSVersion().Version}");
		}

		builder.AppendLine($"{os} architecture - {RuntimeInformation.OSArchitecture}");

		builder.AppendLine($"Process architecture - {RuntimeInformation.ProcessArchitecture}");

		builder.AppendLine($"Runtime identifier - {RuntimeInformation.RuntimeIdentifier}");

		builder.Append($".NET version - {RuntimeInformation.FrameworkDescription}");

		_logger.LogInformationWithTemplate(builder.ToString());

		_logger.LogInformationWithTemplate($"Application settings:{_settingsStore
			.Settings
			.GetPropertyValues(true)}");
	}
	#endregion
}
