# Personal Task & Schedule Management System

## Local Application Setup
Console app to create, edit, view, save and load tasks. Tasks are persisted as CSV files (columns: `Title,Category,Priority,Status,Description,DueDate,CreatedAt,CompletedAt`).

Prerequisites
- .NET 9 SDK installed (matching project target).
- Optionally: Visual Studio 2022/2023 or newer.

Build and run

Using command line:
1. From repository root:
   - Build:
     ```
     dotnet build
     ```
   - Run the console app (project folder is `ConsoleApp`):
     ```
     dotnet run --project ConsoleApp
     ```

Using Visual Studio:
1. Open the solution.
2. Build the solution with __Build Solution__.
3. Run the app with __Debug > Start Without Debugging__ (or __Debug > Start Debugging__).

Error handling & tips
- File I/O errors (permission, locked files) surface to the console. Ensure you have write/read permissions for the chosen directory.
- Malformed CSV lines are skipped; see the corresponding `.load.log` for details and line numbers.
- The app shows task indices that correspond to the master in-memory list (use those indices for edit/delete).

Development notes
- Persistence is implemented via `ConsoleApp.Storage.ITaskRepository` and `CsvTaskRepository`.
- `TaskDetails` supports round-trip of `CreatedAt` and `CompletedAt`.
- If you want JSON persistence or a different storage backend, implement `ITaskRepository` and swap the repository instantiation in `Program`.


## Manual test cases — Step-by-step

Use these manual tests to exercise every menu option and verify expected behavior, validation and file I/O.

**Prerequisites**
- Build project: `dotnet build` or in Visual Studio use __Build Solution__.
- Run app: `dotnet run --project ConsoleApp` or in Visual Studio use __Debug > Start Without Debugging__.

**General notes**
- The app shows indices that map to the master in-memory list; use those indices for edit/delete.
- Saved CSV fields: `Title,Category,Priority,Status,Description,DueDate,CreatedAt,CompletedAt`.
- Load creates a `<filename>.load.log` for skipped/invalid lines.
- Backups: `tasks-list-backup.<yyyyMMddHHmmss>.csv` placed in the same directory.

1) Add new task (`1`)
- Steps:
  1. Choose `1`.
  2. Enter `Title` (required), `Description` (optional), `Category` (required), `Due date` (optional `yyyy-MM-dd`), `Priority` (press Enter to default to `Medium` or type `High`), `Status` (Enter for default `NotStarted`).
- Expected:
  - Task is added and printed with index and timestamps.
  - Validation: empty title/category should display "Value is required" or constructor error caught and message shown.
- Edge cases:
  - Invalid date -> "Invalid date format. Ignoring value." Task still added without due date.
  - Title whitespace -> rejected.

2) Edit existing task (`2`)
- Precondition: at least one task exists.
- Steps:
  1. Choose `2`.
  2. Enter task index shown by list.
  3. Change fields or press Enter to keep existing.
  4. To clear due date: answer `y` to the "Clear due date?" prompt.
- Expected:
  - Updated task printed with new values.
  - Invalid category (empty) is rejected with message, original preserved.
  - Invalid enum input for priority/status leaves previous value and prints "Invalid value. Using default ..." (or no change).

3) Delete task (`3`)
- Steps:
  1. Choose `3`.
  2. Enter index.
  3. Confirm `y` to delete or `n` to cancel.
- Expected:
  - On `y`: task removed from master list.
  - On `n`: no change.

4) Mark task as completed (`4`)
- Steps:
  1. Choose `4`.
  2. Enter index.
- Expected:
  - `Status` becomes `Completed`.
  - `CompletedAt` is set (check display and CSV after saving).

5) View / Filter / Sort (`5`)
- Tests:
  - View all: `1`.
  - Filter by category: `2` -> enter a category (case-insensitive).
  - Filter by status: `3` -> pick one of `NotStarted`, `InProgress`, `Completed`.
  - Filter by due date range: `4` -> test inclusive boundaries.
  - Sort: `5` -> test options 1 (due earliest first), 2 (priority High->Low), 3 (title A->Z).
- Expected:
  - Correct subset/order printed.

6) Search tasks (`6`)
- Steps:
  1. Choose `6`.
  2. Enter keyword (case-insensitive).
- Expected:
  - Matches in `Title` or `Description` displayed.
  - Empty entry is rejected by prompt.

7) View tasks due within date range (`7`)
- Steps:
  - Use quick choices `1` (next 7 days), `2` (next 30 days), or `3` custom start/end `yyyy-MM-dd`.
- Expected:
  - Only tasks with `DueDate` in inclusive range shown.

8) Day / Week view (`8`)
- Steps:
  - Choose `1` for a single day (enter date or press Enter for today).
  - Choose `2` for 7-day week (enter start or default today).
- Expected:
  - Tasks grouped by date, ordered by priority then title.
  - "No due date" section shows tasks without due dates.

9) Save tasks to file (`9`)
- Steps:
  1. Choose `9`.
  2. Enter path (e.g., `tasks.csv` or `C:\temp\my-tasks`).
- Behaviors to verify:
  - No extension -> `.csv` appended.
  - Non-`.csv` extension -> prompts to replace with `.csv`.
  - Existing file -> prompts before overwrite.
  - After saving, open CSV and verify header and rows, date/time formats:
    - `DueDate`: `yyyy-MM-dd`
    - `CreatedAt`/`CompletedAt`: `yyyy-MM-dd HH:mm:ss`
- Edge cases:
  - Write to a protected directory -> expect permission error message.
  - Large lists should write without crashing.

10) Load tasks from file (`10`)
- Steps:
  1. Choose `10`.
  2. Enter CSV path (use a file saved earlier).
  3. After load, choose:
     - `1` Replace and backup (creates `tasks-list-backup.<timestamp>.csv`)
     - `2` Replace without backup
     - `3` Append
- Verify:
  - Valid lines are imported.
  - Invalid lines (bad enum, missing title/category, invalid date, unmatched quotes) are skipped and logged to `<filename>.load.log`.
  - `Replace and backup` creates `tasks-list-backup.<yyyyMMddHHmmss>.csv` in same directory.
  - `CreatedAt` and `CompletedAt` values are restored.
- Edge cases:
  - Malformed CSV (unmatched quotes) should be logged and skipped.
  - If backup creation fails (permission), user is prompted whether to continue.

0) Exit (`0`)
- Steps: choose `0`.
- Expected: application terminates.

