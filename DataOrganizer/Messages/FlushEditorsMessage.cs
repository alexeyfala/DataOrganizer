using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DataOrganizer.Messages;

/// <summary>
/// Request asking every open editor to persist its pending changes; a reply of <c>False</c> means the
/// contents could not be saved.
/// </summary>
public sealed class FlushEditorsMessage : AsyncCollectionRequestMessage<bool>;
