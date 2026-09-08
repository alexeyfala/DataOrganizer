namespace DataOrganizer.Interfaces;

/// <summary>
/// Shows a message in a window of its own, which needs neither the main window nor its focus.
/// </summary>
public interface IToastPresenter : IMessagePresenter<string>;
