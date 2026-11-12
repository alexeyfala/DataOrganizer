using DataOrganizer.Shared.Extensions;
using DataOrganizer.Shared.Interfaces;
using DataOrganizer.SystemIntegration.FileExecution.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DataOrganizer.SystemIntegration.FileExecution.Services;

/// <summary>
/// Finds the programs that hold a file open through the Restart Manager of Windows. Services, Explorer, critical
/// processes and this application are never among them.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed partial class WindowsExecutingFileHolderFinder : IExecutingFileHolderFinder
{
	#region Data
	/// <summary>
	/// Answer of <c>RmGetList</c> when the buffer is too small, <c>ERROR_MORE_DATA</c> in the Windows headers.
	/// </summary>
	private const int ErrorMoreData = 234;

	/// <summary>
	/// Success code of a Restart Manager call, <c>ERROR_SUCCESS</c> in the Windows headers.
	/// </summary>
	private const int ErrorSuccess = 0;

	/// <summary>
	/// Rounds of the list query, for a list that grows between the call that sizes it and the call that fills it.
	/// </summary>
	private const int MaxListAttempts = 3;

	/// <summary>
	/// Kind of a critical process, <c>RmCritical</c>.
	/// </summary>
	private const int RmCritical = 1000;

	/// <summary>
	/// Kind of the Explorer process, <c>RmExplorer</c>.
	/// </summary>
	private const int RmExplorer = 4;

	/// <summary>
	/// Kind of a Windows service, <c>RmService</c>.
	/// </summary>
	private const int RmService = 3;

	/// <summary>
	/// Length of the session key buffer, <c>CCH_RM_SESSION_KEY</c> + 1.
	/// </summary>
	private const int SessionKeyLength = 33;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IProcessFinder" />
	private readonly IProcessFinder _processFinder;
	#endregion

	#region Constructors
	[ExcludeFromCodeCoverage(Justification = "Asks the Windows Restart Manager, runs only on Windows")]
	public WindowsExecutingFileHolderFinder(ILogger logger, IProcessFinder processFinder)
	{
		_logger = logger;

		_processFinder = processFinder;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	[ExcludeFromCodeCoverage(Justification = "Asks the Windows Restart Manager, runs only on Windows")]
	public unsafe IExecutingFileProcess[] FindHolders(string filePath)
	{
		List<IExecutingFileProcess> holders = [];

		foreach (RM_PROCESS_INFO entry in QueryHolders(filePath))
		{
			int processId = entry.Process.dwProcessId;

			string appName = new(entry.strAppName);

			if (!IsHolderToKill(entry.ApplicationType, processId, Environment.ProcessId))
			{
				_logger.LogDebug($@"Process {processId} ""{appName}"" of kind {entry.ApplicationType} holds the file and is left alone.");

				continue;
			}

			// A process that has ended since the query leaves nothing to kill.
			if (_processFinder.FindById(processId) is not { } holder)
			{
				continue;
			}

			holders.Add(holder);

			_logger.LogDebug($@"Process {processId} ""{appName}"" holds the file.");
		}

		return [.. holders];
	}

	/// <summary>
	/// <c>True</c> when the process <paramref name="processId" /> of the Restart Manager kind
	/// <paramref name="applicationType" /> may be killed: no service, Explorer, critical process or this application.
	/// </summary>
	internal static bool IsHolderToKill(int applicationType, int processId, int ownProcessId)
	{
		return processId != ownProcessId
			&& applicationType is not (RmService or RmExplorer or RmCritical);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Asks the Restart Manager which processes use <paramref name="filePath" />; empty when it cannot tell.
	/// </summary>
	[ExcludeFromCodeCoverage(Justification = "Asks the Windows Restart Manager, runs only on Windows")]
	private unsafe RM_PROCESS_INFO[] QueryHolders(string filePath)
	{
		char* sessionKey = stackalloc char[SessionKeyLength];

		int result = RmStartSession(out uint session, 0, sessionKey);

		if (result != ErrorSuccess)
		{
			_logger.LogWarning($"Restart Manager session failed to start with error {result}.");

			return [];
		}

		try
		{
			result = RmRegisterResources(session, 1, [filePath], 0, 0, 0, 0);

			if (result != ErrorSuccess)
			{
				_logger.LogWarning($"Restart Manager failed to register the file with error {result}.");

				return [];
			}

			RM_PROCESS_INFO[] entries = [];

			for (int attempt = 0; attempt < MaxListAttempts; attempt++)
			{
				uint count = (uint)entries.Length;

				fixed (RM_PROCESS_INFO* buffer = entries)
				{
					result = RmGetList(session, out uint needed, ref count, buffer, out _);

					if (result == ErrorSuccess)
					{
						return entries[..(int)count];
					}

					if (result != ErrorMoreData)
					{
						_logger.LogWarning($"Restart Manager failed to list the processes with error {result}.");

						return [];
					}

					entries = new RM_PROCESS_INFO[needed];
				}
			}

			_logger.LogWarning($"Restart Manager kept finding new processes after {MaxListAttempts} attempts.");

			return [];
		}
		finally
		{
			_ = RmEndSession(session);
		}
	}
	#endregion

	#region Native
	[LibraryImport("rstrtmgr.dll")]
	private static partial int RmEndSession(uint dwSessionHandle);

	[LibraryImport("rstrtmgr.dll")]
	private static unsafe partial int RmGetList(
		uint dwSessionHandle,
		out uint pnProcInfoNeeded,
		ref uint pnProcInfo,
		RM_PROCESS_INFO* rgAffectedApps,
		out uint lpdwRebootReasons);

	[LibraryImport("rstrtmgr.dll", StringMarshalling = StringMarshalling.Utf16)]
	private static partial int RmRegisterResources(
		uint dwSessionHandle,
		uint nFiles,
		[In] string[] rgsFileNames,
		uint nApplications,
		nint rgApplications,
		uint nServices,
		nint rgsServiceNames);

	[LibraryImport("rstrtmgr.dll")]
	private static unsafe partial int RmStartSession(
		out uint pSessionHandle,
		int dwSessionFlags,
		char* strSessionKey);
	#endregion

	#region Nested Types
	/// <summary>
	/// Blittable mirror of the native <c>RM_PROCESS_INFO</c> struct; string fields use <c>fixed char[N]</c> buffers.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	private unsafe struct RM_PROCESS_INFO
	{
		public RM_UNIQUE_PROCESS Process;

		public fixed char strAppName[256];

		public fixed char strServiceShortName[64];

		public int ApplicationType;

		public int AppStatus;

		public int TSSessionId;

		public int bRestartable;
	}

	/// <summary>
	/// Blittable mirror of the native <c>RM_UNIQUE_PROCESS</c> struct; the start time is a <c>FILETIME</c> in two halves.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	private struct RM_UNIQUE_PROCESS
	{
		public int dwProcessId;

		public uint ProcessStartTimeLow;

		public uint ProcessStartTimeHigh;
	}
	#endregion
}
