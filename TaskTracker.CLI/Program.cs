using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using TaskTracker.Application.Contracts;
using TaskTracker.Application.Services;
using TaskTracker.CLI.Utilities;
using TaskTracker.Infrastructure;
using TaskTracker.Infrastructure.Data;

// Define user config path
string userConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tasktracker", "config.json");

// Load configuration
var configuration = LoadConfiguration(userConfigPath);

// Configure & Add Services
var serviceCollection = new ServiceCollection();
ConfigureServices(serviceCollection, configuration);

// Build Service Provider
var serviceProvider = serviceCollection.BuildServiceProvider();

using (var scope = serviceProvider.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate(); // Applies any pending migrations
}

var _taskService = serviceProvider.GetService<ITaskService>();

if (args.Length > 0 && args[0] == "set-db")
{
    if (args.Length < 2)
    {
        Console.WriteLine("❌ Please provide a database path.");
        return;
    }

    string newDbPath = args[1];
    UpdateConnectionString(newDbPath);
    return;
}

DisplayWelcomeMessage();
List<string> commands = [];
while (true)
{
    Utility.PrintCommandMessage("Enter command : ");
    string input = Console.ReadLine() ?? string.Empty;

    if (string.IsNullOrEmpty(input))
    {
        Utility.PrintInfoMessage("\n No input detected, Try again!");
        continue;
    }

    commands = Utility.ParseInput(input);
    string command = commands[0].ToLower();

    bool exit = false;
    switch (command)
    {
        case "help":
            PrintHelpCommands();
            break;

        case "add":
            AddNewTask();
            break;

        case "delete":
            DeleteTask();
            break;

        case "update":
            UpdateTask();
            break;

        case "list":
            DisplayAllTasks();
            break;

        case "clear":
            Utility.ClearConsole();
            DisplayWelcomeMessage();
            continue;

        case "mark-in-progress":
            SetStatusOfTask();
            break;

        case "mark-todo":
            SetStatusOfTask();
            break;

        case "mark-done":
            SetStatusOfTask();
            break;

        case "exit":
            exit = true;
            break;

        default:
            break;
    }
}

static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddDbContext(configuration);
    services.AddRepositories();

    // Register services here
    services.AddSingleton<ITaskService, TaskService>();
}

static IConfiguration LoadConfiguration(string userConfigPath)
{
    var builder = new ConfigurationBuilder();

    // Ensure user config directory exists
    string userConfigDirectory = Path.GetDirectoryName(userConfigPath)!;
    if (!Directory.Exists(userConfigDirectory))
    {
        Directory.CreateDirectory(userConfigDirectory);
    }

    // If user config exists, load it
    if (File.Exists(userConfigPath))
    {
        builder.AddJsonFile(userConfigPath, optional: false, reloadOnChange: true);
        return builder.Build();
    }

    // Otherwise, load default from embedded resource and copy to user directory
    var assembly = Assembly.GetExecutingAssembly();
    var resourceName = "TaskTracker.CLI.appsettings.json"; // Ensure correct namespace + filename

    using (var stream = assembly.GetManifestResourceStream(resourceName))
    {
        if (stream == null)
        {
            throw new FileNotFoundException("Could not find 'appsettings.json' in working directory or as an embedded resource.");
        }

        using (var reader = new StreamReader(stream))
        {
            var json = reader.ReadToEnd();
            File.WriteAllText(userConfigPath, json); // Save default config to user directory
            builder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)));
            return builder.Build();
        }
    }
}

static void UpdateConnectionString(string newDatabasePath)
{
    string userConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".tasktracker",
        "config.json"
    );

    if (!File.Exists(userConfigPath))
    {
        Console.WriteLine("❌ Configuration file not found. Run the tool once to generate it.");
        return;
    }

    // Read and parse JSON
    var json = File.ReadAllText(userConfigPath);
    var config = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

    if (config == null)
    {
        Console.WriteLine("❌ Invalid configuration format.");
        return;
    }

    // ✅ Ensure "ConnectionStrings" exists & Deserialize it properly
    if (!config.ContainsKey("ConnectionStrings"))
    {
        config["ConnectionStrings"] = new Dictionary<string, string>();
    }

    // 🔥 FIX: Deserialize the "ConnectionStrings" section correctly
    var connectionStrings = JsonSerializer.Deserialize<Dictionary<string, string>>(config["ConnectionStrings"].ToString()!);
    if (connectionStrings == null)
    {
        Console.WriteLine("❌ Error reading ConnectionStrings.");
        return;
    }

    // ✅ Update the connection string
    connectionStrings["DefaultConnection"] = $"Data Source={newDatabasePath}";
    config["ConnectionStrings"] = connectionStrings; // Reassign updated dictionary

    // ✅ Save updated JSON
    File.WriteAllText(userConfigPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));

    Console.WriteLine($"✅ Connection string updated! Database now stored at: {newDatabasePath}");
}



static void DisplayWelcomeMessage()
{
    Utility.PrintInfoMessage("Hello, Welcome to Task Tracker!");
    Utility.PrintInfoMessage("Type \"help\" to know the set of commands");
}

void PrintHelpCommands()
{
    var helpCommands = _taskService?.GetAllHelpCommands();
    int count = 1;
    if (helpCommands != null)
    {
        foreach (var item in helpCommands)
        {
            Utility.PrintHelpMessage(count + ". " + item);
            count++;
        }
    }
}

static bool IsUserInputValid(List<string> commands, int parameterRequired)
{
    bool validInput = true;

    if (parameterRequired == 1)
    {
        if (commands.Count != parameterRequired)
        {
            validInput = false;
        }
    }

    if (parameterRequired == 2)
    {
        if (commands.Count != parameterRequired || string.IsNullOrEmpty(commands[1]))
        {
            validInput = false;
        }
    }

    if (parameterRequired == 3)
    {
        if (commands.Count != parameterRequired || string.IsNullOrEmpty(commands[1]) || string.IsNullOrEmpty(commands[2]))
        {
            validInput = false;
        }
    }

    if (!validInput)
    {

        Utility.PrintErrorMessage("Wrong command! Try again.");
        Utility.PrintInfoMessage("Type \"help\" to know the set of commands");
        return false;
    }

    return true;
}

static Tuple<bool, int> IsValidIdProvided(List<string> commands, int id)
{
    Int32.TryParse(commands[1], out id);

    if (id == 0)
    {
        Utility.PrintErrorMessage("Wrong command! Try again.");
        Utility.PrintInfoMessage("Type \"help\" to know the set of commands");
        return new Tuple<bool, int>(false, id);
    }

    return new Tuple<bool, int>(true, id);
}

void AddNewTask()
{
    if (!IsUserInputValid(commands, 2))
    {
        return;
    }

    var taskAdded = _taskService?.AddTaskAsync(commands[1]);

    if (taskAdded != null && taskAdded.Result != 0)
        Utility.PrintInfoMessage($"Task added successfully with Id : {taskAdded.Result}");
    else
        Utility.PrintInfoMessage("Task not saved!");
}

void UpdateTask()
{
    if (!IsUserInputValid(commands, 3))
    {
        return;
    }

    int id = IsValidIdProvided(commands, 0).Item2;


    if (id == 0)
    {
        return;
    }

    var result = _taskService?.UpdateTaskAsync(id, commands[2]).Result;

    if (result != null && result.Value)
    {
        Utility.PrintInfoMessage($"Task updated successfully with Id : {id}");
    }
    else
    {
        Utility.PrintInfoMessage($"Task with Id : {id}, does not exist!");
    }
}

void DeleteTask()
{
    if (!IsUserInputValid(commands, 2))
    {
        return;
    }

    int id = IsValidIdProvided(commands, 0).Item2;

    if (id == 0)
    {
        return;
    }

    var result = _taskService?.DeleteTaskAsync(id).Result;

    if (result != null && result.Value)
    {
        Utility.PrintInfoMessage($"Task deleted successfully with Id : {id}");
    }
    else
    {
        Utility.PrintInfoMessage($"Task with Id : {id}, does not exist!");
    }
}

void DisplayAllTasks()
{
    if (commands.Count > 2)
    {
        Utility.PrintErrorMessage("Wrong command! Try again.");
        Utility.PrintInfoMessage("Type \"help\" to know the set of commands");
        return;
    }

    List<TaskTracker.Domain.Entities.Task> tasks = new List<TaskTracker.Domain.Entities.Task>();
    if (commands.Count == 1)
    {
        tasks = _taskService?.GetAllTasksAsync().Result.OrderBy(x => x.Id).ToList() ?? tasks;
    }
    else
    {
        if (!commands[1].ToLower().Equals("in-progress") && !commands[1].ToLower().Equals("done") && !commands[1].ToLower().Equals("todo"))
        {
            Utility.PrintErrorMessage("Wrong command! Try again.");
            Utility.PrintInfoMessage("Type \"help\" to know the set of commands");
            return;
        }
        tasks = _taskService?.GetTaskByStatusAsync(commands[1]).Result.OrderBy(x => x.Id).ToList() ?? tasks;
    }

    CreateTaskTable(tasks);
}

static void CreateTaskTable(List<TaskTracker.Domain.Entities.Task> tasks)
{
    int colWidth1 = 15, colWidth2 = 35, colWidth3 = 15, colWidth4 = 15;
    if (tasks != null && tasks.Count > 0)
    {
        Console.WriteLine("\n{0,-" + colWidth1 + "} {1,-" + colWidth2 + "} {2,-" + colWidth3 + "} {3,-" + colWidth4 + "}",
            "Task Id", "Description", "Status", "Created Date" + "\n");

        foreach (var task in tasks)
        {
            SetConsoleTextColor(task);
            Console.WriteLine("{0,-" + colWidth1 + "} {1,-" + colWidth2 + "} {2,-" + colWidth3 + "} {3,-" + colWidth4 + "}"
                , task.Id, task.Description, task.TaskStatus, task.CreatedAt.Date.ToString("dd-MM-yyyy"));
            Console.ResetColor();
        }
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\n No Task exists! \n");
        Console.ResetColor();

        Console.WriteLine("{0,-" + colWidth1 + "} {1,-" + colWidth2 + "} {2,-" + colWidth3 + "} {3,-" + colWidth4 + "}",
           "Task Id", "Description", "Status", "CreatedDate");
    }
}

static void SetConsoleTextColor(TaskTracker.Domain.Entities.Task task)
{
    if (task.TaskStatus == TaskTracker.Domain.Enums.Status.TODO)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
    }
    else if (task.TaskStatus == TaskTracker.Domain.Enums.Status.DONE)
    {
        Console.ForegroundColor = ConsoleColor.Green;
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
    }
}

void SetStatusOfTask()
{
    if (!IsUserInputValid(commands, 2))
    {
        return;
    }

    int id = IsValidIdProvided(commands, 0).Item2;


    if (id == 0)
    {
        return;
    }

    var result = _taskService?.SetStatusAsync(commands[0], id).Result;

    if (result != null && result.Value)
    {
        Utility.PrintInfoMessage($"Task status set successfully with Id : {id}");
    }
    else
    {
        Utility.PrintInfoMessage($"Task with Id : {id}, does not exist!");
    }

}