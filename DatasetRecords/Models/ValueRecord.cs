using System.Diagnostics;

namespace DatasetRecords.Models;

/// <summary>
/// Record of a dataset that holds a value without a key.
/// </summary>
[DebuggerDisplay($"{nameof(Type)} = {{{nameof(Type)}}}, {nameof(Value)} = {{{nameof(Value)}}}")]
public sealed class ValueRecord : ValueRecordBase;
