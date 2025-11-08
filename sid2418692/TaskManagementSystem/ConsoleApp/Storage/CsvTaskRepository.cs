using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ConsoleApp.Models;
using TaskStatus = ConsoleApp.Models.TaskStatus;

namespace ConsoleApp.Storage
{
    public class CsvTaskRepository : ITaskRepository
    {
        public void Save(IEnumerable<TaskDetails> tasks, string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using var sw = new StreamWriter(path, false);
            sw.WriteLine("Title,Category,Priority,Status,Description,DueDate,CreatedAt,CompletedAt");
            foreach (var t in tasks)
            {
                var title = CsvEscape(t.Title);
                var category = CsvEscape(t.Category);
                var priority = CsvEscape(t.Priority.ToString());
                var status = CsvEscape(t.Status.ToString());
                var desc = CsvEscape(t.Description ?? string.Empty);
                var due = t.DueDate.HasValue ? CsvEscape(t.DueDate.Value.ToString("yyyy-MM-dd")) : CsvEscape(string.Empty);
                var createdAt = CsvEscape(t.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                var completedAt = t.CompletedAt.HasValue ? CsvEscape(t.CompletedAt.Value.ToString("yyyy-MM-dd HH:mm:ss")) : CsvEscape(string.Empty);

                sw.WriteLine(string.Join(",", new[] { title, category, priority, status, desc, due, createdAt, completedAt }));
            }
        }

        public List<TaskDetails> Load(string path, out int skipped)
        {
            var result = new List<TaskDetails>();
            skipped = 0;
            var logPath = Path.ChangeExtension(path, null) + ".load.log";

            using var sr = new StreamReader(path);
            string? line;
            var isFirst = true;
            var lineNo = 0;
            while ((line = sr.ReadLine()) != null)
            {
                lineNo++;
                if (isFirst)
                {
                    if (line.TrimStart().StartsWith("Title", StringComparison.OrdinalIgnoreCase))
                    {
                        isFirst = false;
                        continue;
                    }
                    isFirst = false;
                }

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] fields;
                try
                {
                    fields = ParseCsvLine(line);
                }
                catch (FormatException fex)
                {
                    skipped++;
                    File.AppendAllText(logPath, $"Line {lineNo}: malformed CSV - {fex.Message}{Environment.NewLine}");
                    continue;
                }
                catch (Exception ex)
                {
                    skipped++;
                    File.AppendAllText(logPath, $"Line {lineNo}: unexpected parse error - {ex.Message}{Environment.NewLine}");
                    continue;
                }

                if (fields.Length < 8)
                {
                    skipped++;
                    File.AppendAllText(logPath, $"Line {lineNo}: not enough columns ({fields.Length}).{Environment.NewLine}");
                    continue;
                }

                try
                {
                    var title = fields[0].Trim();
                    var category = fields[1].Trim();
                    var priorityText = fields[2].Trim();
                    var statusText = fields[3].Trim();
                    var description = string.IsNullOrWhiteSpace(fields[4]) ? null : fields[4];
                    var dueText = fields[5].Trim();
                    var createdAtText = fields[6].Trim();
                    var completedAtText = fields[7].Trim();

                    if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(category))
                    {
                        skipped++;
                        File.AppendAllText(logPath, $"Line {lineNo}: empty title or category.{Environment.NewLine}");
                        continue;
                    }

                    if (!Enum.TryParse<PriorityLevel>(priorityText, true, out var priority))
                    {
                        skipped++;
                        File.AppendAllText(logPath, $"Line {lineNo}: invalid priority '{priorityText}'.{Environment.NewLine}");
                        continue;
                    }

                    if (!Enum.TryParse<TaskStatus>(statusText, true, out var status))
                    {
                        skipped++;
                        File.AppendAllText(logPath, $"Line {lineNo}: invalid status '{statusText}'.{Environment.NewLine}");
                        continue;
                    }

                    DateTime? dueDate = null;
                    if (!string.IsNullOrWhiteSpace(dueText))
                    {
                        if (DateTime.TryParseExact(dueText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                            dueDate = dt.Date;
                        else if (DateTime.TryParse(dueText, out dt))
                            dueDate = dt.Date;
                        else
                        {
                            skipped++;
                            File.AppendAllText(logPath, $"Line {lineNo}: invalid due date '{dueText}'.{Environment.NewLine}");
                            continue;
                        }
                    }

                    if (!DateTime.TryParseExact(createdAtText, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var createdAt))
                    {
                        if (!DateTime.TryParse(createdAtText, out createdAt))
                        {
                            skipped++;
                            File.AppendAllText(logPath, $"Line {lineNo}: invalid CreatedAt '{createdAtText}'.{Environment.NewLine}");
                            continue;
                        }
                    }

                    DateTime? completedAt = null;
                    if (!string.IsNullOrWhiteSpace(completedAtText))
                    {
                        if (DateTime.TryParseExact(completedAtText, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var comp))
                            completedAt = comp;
                        else if (DateTime.TryParse(completedAtText, out comp))
                            completedAt = comp;
                        else
                        {
                            skipped++;
                            File.AppendAllText(logPath, $"Line {lineNo}: invalid CompletedAt '{completedAtText}'.{Environment.NewLine}");
                            continue;
                        }
                    }

                    var task = new TaskDetails(title, category, priority, status, description, dueDate, createdAt, completedAt);
                    result.Add(task);
                }
                catch (ArgumentException aex)
                {
                    skipped++;
                    File.AppendAllText(logPath, $"Line {lineNo}: invalid data - {aex.Message}{Environment.NewLine}");
                    continue;
                }
                catch (Exception ex)
                {
                    skipped++;
                    File.AppendAllText(logPath, $"Line {lineNo}: unexpected error - {ex.Message}{Environment.NewLine}");
                    continue;
                }
            }

            return result;
        }

        // --- CSV helper methods (private) ---
        private static string CsvEscape(string value)
        {
            if (value == null) return "\"\"";
            var escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        private static string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var cur = new StringBuilder();
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        cur.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(cur.ToString());
                    cur.Clear();
                }
                else
                {
                    cur.Append(c);
                }
            }

            if (inQuotes)
                throw new FormatException("Unmatched quote in CSV line.");

            fields.Add(cur.ToString());
            return fields.ToArray();
        }
    }
}