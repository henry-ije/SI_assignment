/**
 * Author: Henry Ije
 * Project: Personal Task & Schedule Management System
 * Purpose: Console user interface to create, edit, view and manage `TaskDetails` instances.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ConsoleApp.Models;
using ConsoleApp.Storage;
using TaskStatus = ConsoleApp.Models.TaskStatus;

namespace ConsoleApp
{
    /**
     * Console UI for the Personal Task & Schedule Management System.
     *
     * Provides a simple interactive menu to add, edit, delete, view, search,
     * sort and filter tasks kept in an in-memory list.
     *
     * @return void (application entry).
     */
    internal static class Program
    {
        /**
         * In-memory storage for tasks. Index displayed to the user corresponds to list index.
         *
         * @return List of TaskDetails stored in memory.
         */
        private static readonly List<TaskDetails> Tasks = new();

        /**
         * Repository responsible for task data persistence.
         */
        private static readonly ITaskRepository Repository = new CsvTaskRepository();


        /**
         * Entry point. Runs the main interactive loop until the user exits.
         *
         * @param args Command line arguments.
         * @return void.
         */
        private static void Main(string[] args)
        {
            Console.WriteLine("Personal Task & Schedule Management System");

            while (true)
            {
                // Alert user if any tasks due within next 24 hours.
                CheckDueSoonAlerts();

                ShowMainMenu();
                Console.Write("Select option: ");
                var choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1": AddTask(); break;
                    case "2": EditTask(); break;
                    case "3": DeleteTask(); break;
                    case "4": MarkTaskCompleted(); break;
                    case "5": ViewTasksMenu(); break;
                    case "6": SearchTasks(); break;
                    case "7": ViewTasksDueRange(); break;
                    case "8": ShowDayWeekView(); break;
                    case "9": SaveTasksInteractive(); break;
                    case "10": LoadTasksInteractive(); break;
                    case "0": return;
                    default:
                        Console.WriteLine("Invalid choice. Try again.");
                        break;
                }

                Console.WriteLine("------------");
                Console.WriteLine();
            }
        }

        /**
         * Prints the main menu options to the console.
         *
         * @return void
         */
        private static void ShowMainMenu()
        {
            Console.WriteLine();
            Console.WriteLine("------------");
            Console.WriteLine("1) Add new task");
            Console.WriteLine("2) Edit existing task");
            Console.WriteLine("3) Delete task");
            Console.WriteLine("4) Mark task as completed");
            Console.WriteLine("5) View / Filter / Sort tasks");
            Console.WriteLine("6) Search tasks by keyword");
            Console.WriteLine("7) View tasks due within date range");
            Console.WriteLine("8) Day / Week view (grouped by date)");
            Console.WriteLine("9) Save tasks to file");
            Console.WriteLine("10) Load tasks from file");
            Console.WriteLine("0) Exit");
        }

        /**
         * Interactive flow to add a new task.
         *
         * Prompts for required and optional fields, constructs a TaskDetails instance and appends it to the in-memory list.
         *
         * @return void
         */
        private static void AddTask()
        {
            PrintSubMenuHeader("Add New Task");

            var title = PromptRequiredString("Title: ");
            var description = PromptOptionalString("Description (optional): ");
            var category = PromptRequiredString("Category: ");
            var dueDate = PromptOptionalDate("Due date (yyyy-MM-dd) (optional): ");
            var priority = PromptEnum<PriorityLevel>("Priority (Low, Medium, High) [Defaults to Medium]: ", PriorityLevel.Medium);
            var status = PromptEnum<TaskStatus>("Status (NotStarted, InProgress, Completed) [Defaults to NotStarted]: ", TaskStatus.NotStarted);

            try
            {
                var task = new TaskDetails(title, category, priority, status, description, dueDate);
                Tasks.Add(task);

                Console.WriteLine("Task added:");
                Console.WriteLine(FormatTaskDisplay(task, Tasks.IndexOf(task)));
            }
            catch (ArgumentException aex)
            {
                Console.WriteLine($"Invalid task data: {aex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to add task: {ex.Message}");
            }
        }

        /**
         * Interactive flow to edit an existing task.
         *
         * User selects a task index then may update title, description, category, due date, priority and status.
         * For fields that are privately set on TaskDetails (Category, DueDate) a new TaskDetails is constructed
         * and replaces the existing item ensuring validation rules are applied.
         *
         * @return void
         */
        private static void EditTask()
        {
            if (!EnsureTasksExist()) return;

            PrintSubMenuHeader("Edit Task");

            var index = PromptForTaskIndex();
            if (index < 0) return;

            try
            {
                var task = Tasks[index];
                Console.WriteLine("Current:");
                Console.WriteLine(FormatTaskDisplay(task, index));

                // Edit fields. Empty input keeps existing.
                var newTitle = PromptOptionalString($"Title [{task.Title}]: ");
                if (!string.IsNullOrWhiteSpace(newTitle)) task.Title = newTitle;

                var newDescription = PromptOptionalString($"Description [{task.Description ?? "<none>"}]: ");
                if (newDescription is not null) task.Description = string.IsNullOrWhiteSpace(newDescription) ? null : newDescription;

                var newCategory = PromptOptionalString($"Category [{task.Category}]: ");
                if (!string.IsNullOrWhiteSpace(newCategory)) task = ReplaceCategory(task, newCategory, index);

                var newDue = PromptOptionalDate($"Due date (yyyy-MM-dd) [{(task.DueDate.HasValue ? task.DueDate.Value.ToString("yyyy-MM-dd") : "none")}]: ");
                // DueDate is nullable so allow clearing via explicit input.
                if (newDue.HasValue || AskYesNo("Clear due date? (y/N): ")) task = ReplaceDueDate(task, newDue, index);

                var newPriorityInput = PromptOptionalString($"Priority [{task.Priority}] (Low, Medium, High): ");
                if (!string.IsNullOrWhiteSpace(newPriorityInput) && Enum.TryParse<PriorityLevel>(newPriorityInput, true, out var parsedPriority))
                {
                    task.UpdatePriority(parsedPriority);
                }

                var newStatusInput = PromptOptionalString($"Status [{task.Status}] (NotStarted, InProgress, Completed): ");
                if (!string.IsNullOrWhiteSpace(newStatusInput) && Enum.TryParse<TaskStatus>(newStatusInput, true, out var parsedStatus))
                {
                    task.UpdateStatus(parsedStatus);
                }

                Console.WriteLine("Task updated:");
                Console.WriteLine(FormatTaskDisplay(task, index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to replace category: {ex.Message}. Change aborted.");
                return;
            }
        }

        /**
         * Rebuilds a TaskDetails with a new category and replaces the item at the provided index.
         *
         * @param original    The original task to replace.
         * @param newCategory New category value.
         * @param index       Index in the Tasks list to replace.
         *
         * @return The new TaskDetails instance now stored in the list.
         *
         * @throws ArgumentException if the new category is null/empty (propagated from TaskDetails ctor).
         * @throws ArgumentOutOfRangeException if TaskDetails ctor validation fails for enums.
         */
        private static TaskDetails ReplaceCategory(TaskDetails original, string newCategory, int index)
        {
            try
            {
                var rebuilt = new TaskDetails(
                    title: original.Title,
                    category: newCategory,
                    priority: original.Priority,
                    status: original.Status,
                    description: original.Description,
                    dueDate: original.DueDate);
                Tasks[index] = rebuilt;
                return rebuilt;
            }
            catch (ArgumentException aex)
            {
                Console.WriteLine($"Invalid category: {aex.Message}. Category not changed.");
                return original;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to replace category: {ex.Message}. Change aborted.");
                return original;
            }
        }

        /**
         * Rebuilds a TaskDetails with a new due date (nullable) and replaces the item at the provided index.
         *
         * @param original The original task to replace.
         * @param newDue   New due date (nullable).
         * @param index    Index in the Tasks list to replace.
         *
         * @return The new TaskDetails instance now stored in the list.
         *
         * @throws ArgumentException if TaskDetails ctor validation fails for title/category.
         * @throws ArgumentOutOfRangeException if TaskDetails ctor validation fails for enums.
         */
        private static TaskDetails ReplaceDueDate(TaskDetails original, DateTime? newDue, int index)
        {
            try
            {
                var rebuilt = new TaskDetails(
                    title: original.Title,
                    category: original.Category,
                    priority: original.Priority,
                    status: original.Status,
                    description: original.Description,
                    dueDate: newDue);
                Tasks[index] = rebuilt;
                return rebuilt;
            }
            catch (ArgumentException aex)
            {
                Console.WriteLine($"Invalid data: {aex.Message}. Due date not changed.");
                return original;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to replace due date: {ex.Message}. Change aborted.");
                return original;
            }
        }

        /**
         * Deletes a task selected by index after asking the user for confirmation.
         *
         * @return void
         */
        private static void DeleteTask()
        {
            if (!EnsureTasksExist()) return;

            PrintSubMenuHeader("Delete Task");

            var index = PromptForTaskIndex();
            if (index < 0) return;

            Console.WriteLine("Selected:");
            Console.WriteLine(FormatTaskDisplay(Tasks[index], index));
            if (AskYesNo("Are you sure you want to delete this task? (y/N): "))
            {
                Tasks.RemoveAt(index);
                Console.WriteLine("Task deleted.");
            }
            else
            {
                Console.WriteLine("Delete cancelled.");
            }
        }

        /**
         * Marks the selected task as completed.
         *
         * @return void
         */
        private static void MarkTaskCompleted()
        {
            if (!EnsureTasksExist()) return;

            PrintSubMenuHeader("Mark Task Completed");

            var index = PromptForTaskIndex();
            if (index < 0) return;

            Tasks[index].MarkCompleted();
            Console.WriteLine("Task marked completed:");
            Console.WriteLine(FormatTaskDisplay(Tasks[index], index));
        }

        /**
         * View menu to allow listing, filtering and sorting tasks.
         *
         * Presents filter options and applies the selected query before displaying results.
         *
         * @return void
         */
        private static void ViewTasksMenu()
        {
            if (!EnsureTasksExist()) return;

            PrintSubMenuHeader("View Tasks - options:");

            Console.WriteLine("1) View all");
            Console.WriteLine("2) Filter by category");
            Console.WriteLine("3) Filter by status");
            Console.WriteLine("4) Filter by due date range");
            Console.WriteLine("5) Sort tasks");
            Console.WriteLine("0) Back");

            Console.Write("Choice: ");
            var choice = Console.ReadLine()?.Trim();

            IEnumerable<TaskDetails> query = Tasks;

            switch (choice)
            {
                case "1":
                    break;
                case "2":
                    var category = PromptRequiredString("Category to filter by: ");
                    query = query.Where(t => string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase));
                    break;
                case "3":
                    var status = PromptEnum<TaskStatus>("Status to filter by (NotStarted, InProgress, Completed): ", TaskStatus.NotStarted);
                    query = query.Where(t => t.Status == status);
                    break;
                case "4":
                    var start = PromptRequiredDate("Start date (yyyy-MM-dd): ");
                    var end = PromptRequiredDate("End date (yyyy-MM-dd): ");
                    query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date >= start.Date && t.DueDate.Value.Date <= end.Date);
                    break;
                case "5":
                    query = SortMenu(query);
                    break;
                default:
                    return;
            }

            DisplayTaskList(query.ToList());
        }

        /**
         * Presents sorting options and returns the ordered enumerable.
         *
         * @param source The source task collection to sort.
         * @return IEnumerable<TaskDetails> sorted according to user choice.
         */
        private static IEnumerable<TaskDetails> SortMenu(IEnumerable<TaskDetails> source)
        {
            PrintSubMenuHeader("Sort by:");

            Console.WriteLine("1) Due date (earliest first)");
            Console.WriteLine("2) Priority (High -> Low)");
            Console.WriteLine("3) Title (A -> Z)");
            Console.Write("Choice: ");
            var choice = Console.ReadLine()?.Trim();

            return choice switch
            {
                "1" => source.OrderBy(t => t.DueDate ?? DateTime.MaxValue),
                "2" => source.OrderByDescending(t => t.Priority),
                "3" => source.OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase),
                _ => source
            };
        }

        /**
         * Prompts for a keyword and searches title and description for matches.
         *
         * Displays matching tasks.
         *
         * @return void
         */
        private static void SearchTasks()
        {
            if (!EnsureTasksExist()) return;

            Console.WriteLine();
            Console.WriteLine("------------");
            // Trim and keep the user's keyword as entered (but trimmed).
            var keyword = PromptRequiredString("Enter search keyword (searches title & description): ").Trim();

            // Use IndexOf with OrdinalIgnoreCase to reliably handle nulls and casing.
            var results = Tasks.Where(t =>
                (!string.IsNullOrEmpty(t.Title) && t.Title.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                || (!string.IsNullOrEmpty(t.Description) && t.Description.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
            ).ToList();

            DisplayTaskList(results);
        }

        /**
         * Displays tasks due within a user-selected range (next 7 days, 30 days, or custom).
         *
         * @return void
         */
        private static void ViewTasksDueRange()
        {
            if (!EnsureTasksExist()) return;

            PrintSubMenuHeader("View tasks due within a specific time range.");

            Console.WriteLine("Quick choices:");
            Console.WriteLine("1) Next 7 days");
            Console.WriteLine("2) Next 30 days");
            Console.WriteLine("3) Custom range");
            Console.Write("Choice: ");
            var choice = Console.ReadLine()?.Trim();

            DateTime start;
            DateTime end;

            if (choice == "1")
            {
                start = DateTime.Today;
                end = DateTime.Today.AddDays(7);
            }
            else if (choice == "2")
            {
                start = DateTime.Today;
                end = DateTime.Today.AddDays(30);
            }
            else
            {
                start = PromptRequiredDate("Start date (yyyy-MM-dd): ");
                end = PromptRequiredDate("End date (yyyy-MM-dd): ");
            }

            var results = Tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date >= start.Date && t.DueDate.Value.Date <= end.Date).ToList();
            DisplayTaskList(results);
        }

        /**
         * Displays a list of tasks to the console. If empty prints a message.
         *
         * @param list The list of tasks to display.
         * @return void
         */
        private static void DisplayTaskList(IList<TaskDetails> list)
        {
            Console.WriteLine();
            if (list.Count == 0)
            {
                Console.WriteLine("No tasks found.");
                return;
            }

            for (var i = 0; i < list.Count; i++)
            {
                {
                    // Show index corresponding to the master Tasks list to keep indexes consistent across views.
                    Console.WriteLine(FormatTaskDisplay(list[i], Tasks.IndexOf(list[i])));
                }
            }
            Console.WriteLine();
        }

        /**
         * Shows tasks grouped by date for either a single day or a seven-day week.
         * Users choose Day or Week and an optional date (defaults to today).
         *
         * @return void
         */
        private static void ShowDayWeekView()
        {
            if (!EnsureTasksExist()) return;

            PrintSubMenuHeader("Day / Week View");

            Console.WriteLine("1) Day view");
            Console.WriteLine("2) Week view (7 days)");
            Console.WriteLine();
            Console.Write("Choice: ");
            var choice = Console.ReadLine()?.Trim();

            if (choice == "1")
            {
                var date = PromptOptionalDate("Date for day view (yyyy-MM-dd) [Defaults to Today]: ") ?? DateTime.Today;
                DisplayGroupedByDate(date, date);
            }
            else if (choice == "2")
            {
                var start = PromptOptionalDate("Start date for week view (yyyy-MM-dd) [Defaults to Today]: ") ?? DateTime.Today;
                var end = start.AddDays(6);
                DisplayGroupedByDate(start, end);
            }
            else
            {
                Console.WriteLine("Invalid choice.");
            }
        }

        /**
         * Displays tasks grouped by date in the inclusive range [start, end].
         * Tasks without due date are shown under "No due date".
         *
         * @param start Start date of range (inclusive).
         * @param end   End date of range (inclusive).
         * @return void
         */
        private static void DisplayGroupedByDate(DateTime start, DateTime end)
        {
            PrintSubMenuHeader($"Tasks {start:yyyy-MM-dd} to {end:yyyy-MM-dd}");

            // Build groups for each date in the range.
            var range = Enumerable.Range(0, (end.Date - start.Date).Days + 1)
                                  .Select(offset => start.Date.AddDays(offset))
                                  .ToList();

            foreach (var day in range)
            {
                Console.WriteLine();
                Console.WriteLine(day.ToString("yyyy-MM-dd (dddd)"));
                var forDay = Tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == day.Date)
                                  .OrderByDescending(t => t.Priority)
                                  .ThenBy(t => t.Title)
                                  .ToList();
                if (forDay.Count == 0)
                {
                    Console.WriteLine("  (no tasks)");
                }
                else
                {
                    foreach (var t in forDay)
                    {
                        Console.WriteLine("  " + FormatTaskDisplay(t, Tasks.IndexOf(t)));
                    }
                }
            }

            // Tasks with no due date.
            var noDue = Tasks.Where(t => !t.DueDate.HasValue).ToList();
            Console.WriteLine();
            Console.WriteLine("------------");
            Console.WriteLine("No due date:");
            if (noDue.Count == 0)
            {
                Console.WriteLine("  (none)");
            }
            else
            {
                foreach (var t in noDue)
                {
                    Console.WriteLine("  " + FormatTaskDisplay(t, Tasks.IndexOf(t)));
                }
            }

            Console.WriteLine();
        }

        /**
         * Check for tasks due within the next 24 hours (and not completed) and print an alert.
         * Called at the start of each main loop so user is notified frequently.
         *
         * @return void
         */
        private static void CheckDueSoonAlerts()
        {
            if (Tasks.Count == 0) return;

            var now = DateTime.Now;
            var cutoff = now.AddHours(24);

            var dueSoon = Tasks.Where(t =>
                t.DueDate.HasValue
                && t.Status != TaskStatus.Completed
                && t.DueDate.Value <= cutoff
            ).OrderBy(t => t.DueDate).ToList();

            if (dueSoon.Count == 0) return;

            Console.WriteLine();
            Console.WriteLine("!!! ALERT: Tasks due within next 24 hours or overdue !!!");
            Console.WriteLine("------------");
            foreach (var t in dueSoon)
            {
                var due = t.DueDate.HasValue ? t.DueDate.Value.ToString("yyyy-MM-dd HH:mm") : "none";
                var overdue = t.DueDate.HasValue && t.DueDate.Value < now ? " (OVERDUE)" : string.Empty;
                Console.WriteLine($" - {t.Title} | Due: {due}{overdue} | Priority: {t.Priority} | Status: {t.Status}");
            }

            Console.WriteLine();
        }

        /**
         * Formats a single task for console display.
         *
         * @param task  The task to format.
         * @param index The displayed index of the task in the list.
         * @return A human-readable single-line representation of the task.
         */
        private static string FormatTaskDisplay(TaskDetails task, int index)
        {
            var due = task.DueDate.HasValue ? task.DueDate.Value.ToString("yyyy-MM-dd") : "none";
            var desc = string.IsNullOrWhiteSpace(task.Description) ? "<none>" : task.Description;
            var created = task.CreatedAt.ToString("yyyy-MM-dd");
            var completed = task.CompletedAt.HasValue ? $" | Completed: {task.CompletedAt.Value:yyyy-MM-dd}" : string.Empty;

            return $"[{index}] {task.Title} | Category: {task.Category} | Priority: {task.Priority} | Status: {task.Status} | Due: {due} | Created: {created}{completed} | Desc: {desc}";
        }

        /**
         * Prompts the user to select a task index after showing the current list.
         *
         * @return The selected index, or -1 if the selection was invalid.
         */
        private static int PromptForTaskIndex()
        {
            DisplayTaskList(Tasks);
            Console.Write("Enter task index: ");

            var input = Console.ReadLine();
            if (int.TryParse(input, out var idx) && idx >= 0 && idx < Tasks.Count) return idx;
            Console.WriteLine("Invalid index.");

            return -1;
        }

        /**
         * Prints a formatted sub-menu header to the console.
         *
         * @param title The text to display as the sub-menu header.
         * @return void
         */
        private static void PrintSubMenuHeader(string title)
        {
            Console.WriteLine();
            Console.WriteLine("------------");
            Console.WriteLine(title);
            Console.WriteLine("------------");
        }

        /**
         * Prompts the user to save current tasks interactively to a CSV file.
         *
         * Behaviour:
         * - If no tasks exist, informs the user and returns immediately.
         * - Prompts for a target file path.
         * - If the file already exists, confirms whether to overwrite it.
         * - Attempts to save the tasks to the specified CSV file.
         * - Handles I/O and permission exceptions gracefully.
         *
         * @return void
         */
        private static void SaveTasksInteractive()
        {
            if (!EnsureTasksExist())
            {
                Console.WriteLine("No tasks to save.");
                return;
            }

            var input = PromptRequiredString("Enter file path to save tasks (e.g. tasks.csv): ").Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Invalid path.");
                return;
            }

            // If user provided a directory path by mistake, detect and reject.
            if (Directory.Exists(input))
            {
                Console.WriteLine("Specified path is a directory. Please provide a file name (for example: tasks.csv).");
                return;
            }

            var ext = Path.GetExtension(input);
            if (string.IsNullOrEmpty(ext))
            {
                // No extension -> append .csv
                input += ".csv";
                Console.WriteLine($"No extension provided. Using '{input}'.");
            }
            else if (!string.Equals(ext, ".csv", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Provided file has extension '{ext}'.");
                if (AskYesNo("Replace extension with '.csv'? (Y/n): "))
                {
                    input = Path.ChangeExtension(input, ".csv");
                    Console.WriteLine($"Saving as '{input}'.");
                }
                else
                {
                    Console.WriteLine("Keeping supplied extension.");
                }
            }

            var path = input;

            if (File.Exists(path))
            {
                Console.WriteLine($"File '{path}' already exists.");
                if (!AskYesNo("Overwrite existing file? (y/N): "))
                {
                    Console.WriteLine("Save cancelled.");
                    return;
                }
            }

            try
            {
                Repository.Save(Tasks, path);
                Console.WriteLine($"Tasks saved to '{path}'.");
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Console.WriteLine($"Failed to save file: {ex.Message}");
            }
        }

        /**
         * Prompts the user to load tasks interactively from a CSV file.
         *
         * Behaviour:
         * - Requests the file path to load from.
         * - Validates that the file exists.
         * - Reads and parses the file using {@link LoadTasksFromCsv(string, out int)}.
         * - Displays how many tasks were loaded and how many lines were skipped.
         * - Allows the user to choose between replacing or appending the current task list.
         * - Handles format, I/O, and permission errors gracefully.
         *
         * @return void
        */
        private static void LoadTasksInteractive()
        {
            var path = PromptRequiredString("Enter file path to load tasks from (e.g. tasks.csv): ").Trim();

            if (!File.Exists(path))
            {
                Console.WriteLine("File not found.");
                return;
            }

            try
            {
                var loaded = Repository.Load(path, out var skipped);
                if (loaded.Count == 0)
                {
                    Console.WriteLine("No valid tasks were read from the file.");
                    return;
                }

                Console.WriteLine($"Read {loaded.Count} tasks from file. {skipped} invalid line(s) were skipped.");
                Console.WriteLine("1) Replace and backup current tasks");
                Console.WriteLine("2) Replace current tasks");
                Console.WriteLine("3) Append to current tasks");
                Console.WriteLine();
                Console.Write("Choice: ");
                var choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        if (!CreateBackup(path))
                        {
                            if (!AskYesNo("Continue with replace without backup? (y/N): "))
                            {
                                Console.WriteLine("Load cancelled.");
                                return;
                            }
                        }
                        ReplaceTasks(loaded, "Current tasks replaced with file contents (backup created).");
                        break;

                    case "2":
                        ReplaceTasks(loaded, "Current tasks replaced with file contents.");
                        break;

                    default:
                        Tasks.AddRange(loaded);
                        Console.WriteLine("Loaded tasks appended to current list.");
                        break;
                }
            }
            catch (FormatException fex)
            {
                Console.WriteLine($"File format error: {fex.Message}");
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Console.WriteLine($"Failed to load file: {ex.Message}");
            }
        }

        /**
         * Creates a timestamped backup of the current tasks in the same directory as the target file.
         *
         * @param path The base file path used to derive the backup file name.
         * @return true if backup was created successfully, false otherwise.
         */
        private static bool CreateBackup(string path)
        {
            try
            {
                // Determine directory to place backup (fallback to current dir)
                var dir = Path.GetDirectoryName(path);
                if (string.IsNullOrWhiteSpace(dir))
                    dir = Directory.GetCurrentDirectory();

                // Preserve original extension if present, otherwise use .csv
                var ext = Path.GetExtension(path);
                if (string.IsNullOrEmpty(ext))
                    ext = ".csv";

                // Build canonical backup name: tasks-list-backup.<timestamp>.<ext>
                var backupFileName = $"tasks-list-backup.{DateTime.Now:yyyyMMddHHmmss}{ext}";
                var backupPath = Path.Combine(dir, backupFileName);

                Repository.Save(Tasks, backupPath);
                Console.WriteLine($"Backup of current tasks created at '{backupPath}'.");
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Console.WriteLine($"Failed to create backup: {ex.Message}");
                return false;
            }
        }

        /**
         * Replaces the current in-memory task list with the given tasks and displays a confirmation message.
         *
         * @param newTasks The new list of tasks to set.
         * @param message A confirmation message to display after replacement.
         * @return void
         */
        private static void ReplaceTasks(List<TaskDetails> newTasks, string message)
        {
            Tasks.Clear();
            Tasks.AddRange(newTasks);
            Console.WriteLine(message);
        }

        #region Input helpers

        /**
         * Prompts until the user supplies a non-empty string.
         *
         * @param prompt Prompt text presented to the user.
         * @return Trimmed, non-empty user input.
         */
        private static string PromptRequiredString(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                var input = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(input)) return input.Trim();
                Console.WriteLine("Value is required.");
            }
        }

        /**
         * Reads a single-line optional string. Returns null if the input stream returned null.
         *
         * @param prompt Prompt text presented to the user.
         * @return The raw user input (may be empty) or null if Console.ReadLine() returned null.
         */
        private static string? PromptOptionalString(string prompt)
        {
            Console.Write(prompt);
            var input = Console.ReadLine();
            if (input is null) return null;

            return input;
        }

        /**
         * Prompts for an optional date. Accepts yyyy-MM-dd or other recognizable date formats.
         *
         * @param prompt The prompt text presented to the user.
         *
         * @return Parsed DateTime.Date or null if no valid value was provided.
         */
        private static DateTime? PromptOptionalDate(string prompt)
        {
            Console.Write(prompt);
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) return null;

            if (DateTime.TryParseExact(input.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt.Date;

            if (DateTime.TryParse(input, out dt))
                return dt.Date;

            Console.WriteLine("Invalid date format. Ignoring value.");
            return null;
        }

        /**
         * Prompts repeatedly until a valid date is provided.
         *
         * @param prompt Prompt text presented to the user.
         * @return Parsed DateTime.Date value.
         */
        private static DateTime PromptRequiredDate(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                var input = Console.ReadLine();
                if (DateTime.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    return dt.Date;
                if (DateTime.TryParse(input, out dt))
                    return dt.Date;
                Console.WriteLine("Invalid date. Please use yyyy-MM-dd.");
            }
        }

        /**
         * Prompts for an enum value and returns the parsed value or the provided default.
         *
         * @param prompt       Prompt text presented to the user.
         * @param defaultValue Value returned when the user provides no input or parsing fails.
         *
         * @return Parsed enum value or defaultValue.
         */
        private static T PromptEnum<T>(string prompt, T defaultValue) where T : struct, Enum
        {
            Console.Write(prompt);
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) return defaultValue;
            if (Enum.TryParse<T>(input.Trim(), true, out var value)) return value;
            Console.WriteLine($"Invalid value. Using default {defaultValue}.");

            return defaultValue;
        }

        /**
         * Simple yes/no helper. Returns true if the user enters a value starting with 'y' or 'Y'.
         *
         * @param prompt Prompt text presented to the user.
         * @return True for yes, false otherwise.
         */
        private static bool AskYesNo(string prompt)
        {
            Console.Write(prompt);
            var input = Console.ReadLine();

            return !string.IsNullOrWhiteSpace(input) && input.StartsWith("y", StringComparison.OrdinalIgnoreCase);
        }

        /**
         * Ensures there is at least one task in the list and prints a message if none exist.
         *
         * @return True if tasks exist; otherwise false.
         */
        private static bool EnsureTasksExist()
        {
            if (Tasks.Count == 0)
            {
                Console.WriteLine("No tasks available. Add tasks first.");
                return false;
            }

            return true;
        }

        #endregion
    }
}
