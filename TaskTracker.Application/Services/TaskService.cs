using System.Reflection;
using System.Text.Json;
using TaskTracker.Application.Contracts;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Services;

public class TaskService : ITaskService
{
    private static string FileName = "task_data.json";
    private static string FilePath = Path.Combine(Directory.GetCurrentDirectory(), FileName);
    private readonly ITaskRepository _taskRepository;

    public TaskService(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<int> AddTaskAsync(string description)
    {
        try
        {
            var task = new Domain.Entities.Task
            {
                Description = description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TaskStatus = Domain.Enums.Status.TODO
            };
            
            var response = await _taskRepository.AddAsync(task);

            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Task addition failed. Error - {ex.Message}");
            return 0;
        }
        throw new NotImplementedException();
    }

    public async Task<bool> DeleteTaskAsync(int id)
    {
        try
        {
            var response = await _taskRepository.DeleteAsync(id);

            if (response.IsSuccess)
            {
                return true;
            }

            Console.WriteLine($"Unable to fetch tasks. Error - {response.Message}");
            return false;
        }
        catch(Exception ex)
        {
            return false;
        }
    }

    public List<string> GetAllHelpCommands()
    {
        return new List<string>
            {
                "add \"Task Description\" - To add a new task, type add with task description",
                "update \"Task Id\" \"Task Description\" - To update a task, type update with task id and task description",
                "delete \"Task Id\" - To delete a task, type delete with task id",
                "mark-in-progress \"Task Id\" - To mark a task to in progress, type mark-in-progress with task id",
                "mark-done \"Task Id\" - To mark a task to done, type mark-done with task id",
                "list - To list all task with its current status",
                "list done - To list all task with done status",
                "list todo  - To list all task with todo status",
                "list in-progress  - To list all task with in-progress status",
                "exit - To exit from app",
                "clear - To clear console window"
            };
    }

    public async Task<List<Domain.Entities.Task>> GetAllTasksAsync()
    {
        try
        {
            var tasks = await _taskRepository.GetAllAsync();

            return tasks.ToList() ?? new List<Domain.Entities.Task>();
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Something went wrong. Error: {ex.Message}");
            return new List<Domain.Entities.Task>();
        }
    }

    public async Task<List<Domain.Entities.Task>> GetTaskByStatusAsync(string status)
    {
        try
        {
            var statusToCheck = GetStatusToDisplay(status);
            var tasks = await _taskRepository.FindAsync(t => t.TaskStatus == statusToCheck);
            
            return tasks.ToList() ?? new List<Domain.Entities.Task>();
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Something went wrong. Error: {ex.Message}");
            return new List<Domain.Entities.Task>();
        }
    }

    public async Task<bool> SetStatusAsync(string status, int id)
    {
        var statusToUpdate = GetStatusToSet(status);

        var task = await _taskRepository.GetByIdAsync(id);
        if(task != null)
        {
            task.TaskStatus = statusToUpdate;
            await _taskRepository.UpdateAsync(task);

            return true;
        }
        return false;
    }

    public async Task<bool> UpdateTaskAsync(int id, string description)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if(task != null)
        {
            task.Description = description;
            await _taskRepository.UpdateAsync(task);
            return true;
        }

        return false;
    }


    private Status GetStatusToDisplay(string status)
    {
        switch (status)
        {
            case "in-progress":
                return Status.IN_PROGRESS;
            case "done":
                return Status.DONE;
            case "todo":
                return Status.TODO;
            default:
                return Status.TODO;
        }
    }

    private Status GetStatusToSet(string status)
    {
        switch (status)
        {
            case "mark-in-progress":
                return Status.IN_PROGRESS;
            case "mark-done":
                return Status.DONE;
            case "mark-todo":
                return Status.TODO;
            default:
                return Status.TODO;
        }
    }
}
