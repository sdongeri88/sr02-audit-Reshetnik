namespace TaskTracker;

// Що потрібно звітам від сховища: лише перелік карток (без History, Add, nextId)
public interface ITaskSource
{
    IReadOnlyList<TaskCard> Cards { get; }
}
