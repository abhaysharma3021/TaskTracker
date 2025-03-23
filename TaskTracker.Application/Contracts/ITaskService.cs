namespace TaskTracker.Application.Contracts;

public interface ITaskService
{
    Task<List<Domain.Entities.Task>> GetAllTasksAsync();
    Task<int> AddTaskAsync(string description);
    Task<bool> UpdateTaskAsync(int id, string description);
    Task<bool> DeleteTaskAsync(int id);
    Task<bool> SetStatusAsync(string status, int id);
    Task<List<Domain.Entities.Task>> GetTaskByStatusAsync(string status);
    List<string> GetAllHelpCommands();
}
