using FileExecution.Interfaces;
using Shared.Common;
using Shared.Enums;
using Shared.Extensions;
using System;
using System.Diagnostics;
using System.Linq;

namespace FileExecution.Services;

/// <inheritdoc cref="IProcessManager" />
public sealed class ProcessManager : IProcessManager
{
	#region Methods
	/// <inheritdoc />
	public void KillProcess(int processId)
	{
		Process
			.GetProcessById(processId)
			.Kill();
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
	public bool ProcessExists(int processId)
	{
		return Process
			.GetProcesses()
			.Any(x => x.Id == processId);
	}

	/// <inheritdoc />
	public bool StartProcess(string filePath, out int processId)
	{
		using Process process = OperatingSystem.IsWindows()
			? CreateWindowsProcess(filePath)
			: CreateNonWindowsProcess(filePath);

		if (process.Start())
		{
			processId = process.Id;

			return true;
		}

		processId = default;

		return false;
	}

	/// <inheritdoc />
	public bool StartProcess(
		string appPath,
		string fileArgument,
		out int processId)
	{
		using Process process = new()
		{
			StartInfo = new(appPath.SurroundWithQuotesIfNeeded())
			{
				Arguments = fileArgument.SurroundWithQuotesIfNeeded(),
				UseShellExecute = false
			}
		};

		if (process.Start())
		{
			processId = process.Id;

			return true;
		}

		processId = default;

		return false;
	}

	/// <inheritdoc />
	public Process StartProcess(string fileName) => Process.Start(fileName);
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
	#endregion
}
