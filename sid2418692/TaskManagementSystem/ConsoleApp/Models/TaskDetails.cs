using System;

namespace ConsoleApp.Models
{
    /**
     * Priority levels for a task.
     */
    public enum PriorityLevel
    {
        Low,
        Medium,
        High
    }

    /**
     * Status values for a task.
     */
    public enum TaskStatus
    {
        NotStarted,
        InProgress,
        Completed
    }

    /**
     * Represents the details of a task.
     */
    public sealed class TaskDetails
    {
        // Initialize variables.
        private string title = string.Empty;

        /**
         * Task title. Cannot be null/empty/whitespace.
         */
        public string Title
        {
            get => title;
            set => title = !string.IsNullOrWhiteSpace(value) 
                    ? value 
                    : throw new ArgumentException("Title cannot be empty.", nameof(Title));
        }

        /**
         * Optional longer description.
         */
        public string? Description { get; set; }

        /**
         * Optional due date. Null means no due date.
         */
        public DateTime? DueDate { get; set; }

        /**
         * Category name (free-form).
         */
        public string? Category { get; set; }

        /**
         * Priority level.
         */
        public PriorityLevel Priority { get; private set; }

        /**
         * Current status.
         */
        public TaskStatus Status { get; private set; }

        /**
         * Creates a new TaskDetails instance.
         */
        public TaskDetails(
            string title,
            string? description = null,
            DateTime? dueDate = null,
            string? category = null,
            PriorityLevel priority = PriorityLevel.Medium,
            TaskStatus status = TaskStatus.NotStarted)
        {
            Title       = title;
            Description = description;
            DueDate     = dueDate;
            Category    = category;
            Priority    = priority;
            Status      = status;
        }

        /**
         * Update the task's status.
         */
        public void UpdateStatus(TaskStatus status) => Status = status;

        /**
         * Mark the task as completed.
         */
        public void MarkCompleted() => Status = TaskStatus.Completed;

        /**
         * Update the task's priority.
         */
        public void UpdatePriority(PriorityLevel priority) => Priority = priority;

        public override string ToString()
        {
            var due      = DueDate.HasValue ? $" Due: {DueDate:yyyy-MM-dd}" : string.Empty;
            var category = string.IsNullOrWhiteSpace(Category) ? string.Empty : $" Category: {Category}";

            return $"{Title} [{Priority}] - {Status}{due}{category}";
        }
    }
}