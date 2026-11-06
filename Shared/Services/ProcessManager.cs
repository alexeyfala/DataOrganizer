using Shared.Common;
using Shared.Enums;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Shared.Services;

// The one wrapper over Process: every other start, run and lookup of a process goes through its roles.
#pragma warning disable RS0030
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
	public ProcessManager(IPlatform platform) => _platform = platform;
	#endregion

	#region Methods
	/// <inheritdoc />
	public IExecutingFileProcess? FindById(int processId)
	{
		try
		{
			return new ExecutingFileProcess(Process.GetProcessById(processId));
		}
		catch (ArgumentException)
		{
			// No process of that id runs.
			return null;
		}
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
		using Process process = new()
		{
			StartInfo = new(fileName, arguments)
			{
				CreateNoWindow = true,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				UseShellExecute = false
			}
		};

		process.Start();

		// The output is read and dropped, so that a full pipe cannot stall the process.
		process.BeginErrorReadLine();

		process.BeginOutputReadLine();

		if (process.WaitForExit(timeout))
		{
			return process.ExitCode;
		}

		try
		{
			process.Kill(entireProcessTree: true);
		}
		catch
		{
			// The process has ended meanwhile.
		}

		return null;
	}

	/// <inheritdoc />
	public void StartProcess(string fileName, IReadOnlyList<string> arguments)
	{
		using Process process = Process.Start(fileName, arguments);
	}
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
#pragma warning restore RS0030
