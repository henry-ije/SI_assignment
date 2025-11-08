/**
 * Author: Henry Ije
 * Project: Personal Task & Schedule Management System
 */

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
         * Category name (required).
         */
        public string Category { get; private set; }

        /**
         * Priority level (required).
         */
        public PriorityLevel Priority { get; private set; }

        /**
         * Current status (required).
         */
        public TaskStatus Status { get; private set; }

        /**
         * Timestamp when the task was created (set automatically).
         */
        public DateTime CreatedAt { get; }

        /**
         * Timestamp when the task was completed (set when MarkCompleted is called).
         * Null until the task is completed.
         */
        public DateTime? CompletedAt { get; private set; }

        /**
         * Creates a new TaskDetails instance.
         *
         * Parameters:
         *   title       - The title of the task. Cannot be null or empty.
         *   category    - The category of the task (e.g., Work, Personal).
         *   priority    - The priority level of the task (Low, Medium, High).
         *   status      - The current status of the task (NotStarted, InProgress, Completed).
         *   description - Optional. A detailed description of the task.
         *   dueDate     - Optional. The due date of the task.
         */
        public TaskDetails(
            string title,
            string category,
            PriorityLevel priority,
            TaskStatus status,
            string? description = null,
            DateTime? dueDate = null
        )
        {
            // Title setter validates for null/whitespace.
            Title = title;

            // Validate required category.
            if (string.IsNullOrWhiteSpace(category))
            {
                throw new ArgumentException("Category cannot be null or empty.", nameof(category));
            }

            // Validate enums are defined values.
            if (!Enum.IsDefined(typeof(PriorityLevel), priority))
            {
                throw new ArgumentOutOfRangeException(nameof(priority), "Priority must be a defined PriorityLevel value.");
            }

            if (!Enum.IsDefined(typeof(TaskStatus), status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), "Status must be a defined TaskStatus value.");
            }

            Description = description;
            DueDate = dueDate;
            Category = category;
            Priority = priority;
            Status = status;

            // Track creation and completion time.
            CreatedAt = DateTime.Now;
            CompletedAt = status == TaskStatus.Completed ? DateTime.Now : null;
        }

        /**
         * Update the task's status.
         */
        public void UpdateStatus(TaskStatus status)
        {
            if (!Enum.IsDefined(typeof(TaskStatus), status))
                throw new ArgumentOutOfRangeException(nameof(status), "Status must be a defined TaskStatus value.");

            Status = status;

            // If status set to completed, stamp completion time.
            if (Status == TaskStatus.Completed && CompletedAt == null)
            {
                CompletedAt = DateTime.Now;
            }

            // If status moves away from completed, clear completion time.
            if (Status != TaskStatus.Completed)
            {
                CompletedAt = null;
            }
        }

        /**
         * Mark the task as completed.
         */
        public void MarkCompleted()
        {
            Status = TaskStatus.Completed;
            CompletedAt = DateTime.Now;
        }

        /**
         * Update the task's priority.
         */
        public void UpdatePriority(PriorityLevel priority)
        {
            if (!Enum.IsDefined(typeof(PriorityLevel), priority))
                throw new ArgumentOutOfRangeException(nameof(priority), "Priority must be a defined PriorityLevel value.");

            Priority = priority;
        }

        public override string ToString()
        {
            var due = DueDate.HasValue ? $" Due: {DueDate:yyyy-MM-dd}" : string.Empty;
            var category = string.IsNullOrWhiteSpace(Category) ? string.Empty : $" Category: {Category}";
            var created = $" Created: {CreatedAt:yyyy-MM-dd HH:mm}";
            var completed = CompletedAt.HasValue ? $" Completed: {CompletedAt:yyyy-MM-dd HH:mm}" : string.Empty;

            return $"{Title} [{Priority}] - {Status}{due}{category}{created}{completed}";
        }
    }
}
