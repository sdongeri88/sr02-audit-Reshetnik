namespace TaskTracker;

// Дані для створення демо-картки: вісім параметрів, які завжди ходили разом
public record CardSeed(
    int Id,
    string Title,
    Column Column,
    string Assignee,
    int Points,
    int Priority,
    string Tags,
    int AgeDays);
