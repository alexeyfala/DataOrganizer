using DataOrganizer.Shared.Common;
using DataOrganizer.Shared.Enums;
using DataOrganizer.Shared.Extensions;
using DataOrganizer.Shared.Interfaces;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace DataOrganizer.Shared.Services;

/// <summary>
/// Starts, runs and finds the processes of the operating system.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Starts and stops real processes")]
public sealed class ProcessManager :
	IProcessFinder,
	IProcessLauncher,
	IProcessRunner
{
	#region Data
	/// <inheritdoc cref="IPlatform" />
	private readonly IPlatform _platform;
	#endregion

	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="ProcessManager" /> class.
	/// </summary>
	public ProcessManager(IPlatform platform) => _platform = platform;
	#endregion

	#region Methods
	/// <inheritdoc />
	public IExecutingFileProcess? FindById(int processId)
	{
		return Process.TryGetProcessById(processId, out Process? process)
			? new ExecutingFileProcess(process)
			: null;
	}

	/// <inheritdoc />
	public IExecutingFileProcess? OpenFile(string filePath)
	{
		return StartExecutingFileProcess(_platform.IsWindows
			? CreateWindowsProcess(filePath)
			: CreateNonWindowsProcess(filePath));
	}

	/// <inheritdoc />
	public IExecutingFileProcess? OpenFile(string appPath, string filePath)
	{
		return StartExecutingFileProcess(new()
		{
			StartInfo = new(appPath.SurroundWithQuotesIfNeeded())
			{
				Arguments = filePath.SurroundWithQuotesIfNeeded(),
				UseShellExecute = false
			}
		});
	}

	/// <inheritdoc />
	public void OpenWithShell(string target)
	{
		using Process? process = Process.Start(new ProcessStartInfo
		{
			FileName = target,
			UseShellExecute = true
		});
	}

	/// <inheritdoc />
	public int? Run(string fileName, IReadOnlyList<string> arguments, TimeSpan timeout)
	{
		// The output goes to the null device, so no pipe can fill up and stall the process.
		using SafeFileHandle nullDevice = File.OpenNullHandle();

		ProcessExitStatus status = Process.Run(
			new ProcessStartInfo(fileName, arguments)
			{
				CreateNoWindow = true,
				StandardErrorHandle = nullDevice,
				StandardInputHandle = nullDevice,
				StandardOutputHandle = nullDevice
			},
			timeout);

		return status.Canceled ? null : status.ExitCode;
	}

	/// <inheritdoc />
	public void StartProcess(string fileName, IReadOnlyList<string> arguments) => Process.StartAndForget(fileName, arguments);
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a process to open a file on an operating system other than <see cref="OperatingSystemKind.Windows" />.
	/// </summary>
	private static Process CreateNonWindowsProcess(string filePath) => new()
	{
		StartInfo = new()
		{
			Arguments = filePath.SurroundWithQuotesIfNeeded(),
			CreateNoWindow = true,
			FileName = PlatformInfo.FileOpener,
			UseShellExecute = false
		}
	};

	/// <summary>
	/// Creates a process to open a file in the <see cref="OperatingSystemKind.Windows" /> operating system.
	/// </summary>
	private static Process CreateWindowsProcess(string filePath) => new()
	{
		StartInfo = new(filePath.SurroundWithQuotesIfNeeded())
		{
			UseShellExecute = true
		}
	};

	/// <summary>
	/// Starts <paramref name="process" /> that opens a file and hands it over; <c>null</c>, with the process released, when
	/// it did not start.
	/// </summary>
	private static ExecutingFileProcess? StartExecutingFileProcess(Process process)
	{
		try
		{
			if (process.Start())
			{
				return new(process);
			}
		}
		catch
		{
			process.Dispose();

			throw;
		}

		process.Dispose();

		return null;
	}
	#endregion
}
