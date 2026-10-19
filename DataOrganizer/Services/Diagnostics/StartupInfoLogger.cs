using Cysharp.Text;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Settings;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using OSVersion = OSVersionExtension.OSVersion;

namespace DataOrganizer.Services.Diagnostics;

/// <inheritdoc cref="IStartupInfoLogger" />
public sealed class StartupInfoLogger : IStartupInfoLogger
{
	#region Data
	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="ICommandLineOptions" />
	private readonly ICommandLineOptions _options;

	/// <inheritdoc cref="IAppSettingsStore" />
	private readonly IAppSettingsStore _settingsStore;
	#endregion

	#region Constructors
	public StartupInfoLogger(
		IAppSettingsStore settingsStore,
		ICommandLineOptions options,
		ILogger logger)
	{
		_logger = logger;

		_options = options;

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

		if (OperatingSystem.IsMacOS())
		{
			builder.AppendLine($"{os} type - macOS {Environment.OSVersion.Version}");
		}

		if (OperatingSystem.IsLinux())
		{
			builder.AppendLine($"{os} type - Linux {Environment.OSVersion.Version}");
		}

		if (OperatingSystem.IsWindows())
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
