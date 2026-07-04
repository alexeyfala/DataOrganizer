using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace DataOrganizer.Services;

public sealed class InstanceRegistry : IInstanceRegistry
{
	#region Properties
	/// <inheritdoc />
	public int InstanceNumber { get; }
	#endregion

	#region Data
	/// <summary>
	/// Name of the directory holding the per-instance lock files.
	/// </summary>
	private const string LocksDirectoryName = "InstanceLocks";

	/// <summary>
	/// Upper bound on the number of slots probed before giving up.
	/// </summary>
	private const int MaxInstances = 1024;

	/// <summary>
	/// The exclusive lock held for the whole session; releasing it frees the slot.
	/// A field keeps the stream alive so the finalizer cannot close the handle early.
	/// </summary>
	private readonly FileStream? _lock;
	#endregion

	#region Constructors
	public InstanceRegistry() => InstanceNumber = ClaimSlot(out _lock);
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Dispose() => _lock?.Dispose();
	#endregion

	#region Helpers
	/// <summary>
	/// Claims the lowest free instance slot by holding an exclusive lock on its file.
	/// Falls back to slot 1 without a lock when the registry directory is unusable.
	/// </summary>
	private static int ClaimSlot(out FileStream? lockStream)
	{
		lockStream = null;

		try
		{
			string directory = Path.Combine(
				IAppEnvironment.GetAppDataDirectoryPath(),
				LocksDirectoryName);

			Directory.CreateDirectory(directory);

			for (int number = 1; number <= MaxInstances; number++)
			{
				string path = Path.Combine(directory, $"{number}.lock");

				try
				{
					// FileShare.None fails while a live instance holds the slot
					// (native share mode on Windows, flock on Unix).
					FileStream stream = new(
						path,
						FileMode.OpenOrCreate,
						FileAccess.ReadWrite,
						FileShare.None);

					WritePidMarker(stream);

					lockStream = stream;

					return number;
				}
				catch (IOException)
				{
					// Slot held by another running instance; try the next one.
				}
			}
		}
		catch (Exception ex)
		{
			Trace.WriteLine(ex.ToStringDemystified());
		}

		return 1;
	}

	/// <summary>
	/// Overwrites the lock file with the current process id for diagnostics.
	/// </summary>
	private static void WritePidMarker(FileStream stream)
	{
		try
		{
			stream.SetLength(0);

			byte[] pid = TextHelper
				.Utf8Encoding
				.GetBytes(Environment.ProcessId.ToString(CultureInfo.InvariantCulture));

			stream.Write(pid);

			stream.Flush();
		}
		catch (Exception ex)
		{
			Trace.WriteLine(ex.ToStringDemystified());
		}
	}
	#endregion
}
