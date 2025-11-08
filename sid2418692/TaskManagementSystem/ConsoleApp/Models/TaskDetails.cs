/**
 * Author: Henry Ije
 * Project: Personal Task & Schedule Management System
 * Purpose: Model representing a task's data (title, description, category, priority, status, dates).
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
         *
         * @return the task title.
         * @throws ArgumentException if an attempt is made to set an empty or whitespace title.
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
         *
         * @return the description or null when not provided.
         */
        public string? Description { get; set; }

        /**
         * Optional due date. Null means no due date.
         *
         * @return the due date or null when not set.
         */
        public DateTime? DueDate { get; set; }

        /**
         * Category name (required).
         *
         * @return the task category.
         */
        public string Category { get; private set; }

        /**
         * Priority level (required).
         *
         * @return the task priority.
         */
        public PriorityLevel Priority { get; private set; }

        /**
         * Current status (required).
         *
         * @return the task status.
         */
        public TaskStatus Status { get; private set; }

        /**
         * Timestamp when the task was created (set automatically).
         *
         * @return creation timestamp.
         */
        public DateTime CreatedAt { get; }

        /**
         * Timestamp when the task was completed (set when MarkCompleted is called).
         * Null until the task is completed.
         *
         * @return completion timestamp or null when not completed.
         */
        public DateTime? CompletedAt { get; private set; }

        /**
         * Creates a new TaskDetails instance.
         *
         * @param title       The title of the task. Cannot be null or empty.
         * @param category    The category of the task (e.g., Work, Personal).
         * @param priority    The priority level of the task (Low, Medium, High).
         * @param status      The current status of the task (NotStarted, InProgress, Completed).
         * @param description Optional. A detailed description of the task.
         * @param dueDate     Optional. The due date of the task.
         *
         * @throws ArgumentException if title or category is null/empty/whitespace.
         * @throws ArgumentOutOfRangeException if priority or status is not a defined enum value.
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
         *
         * @param status The new status to set.
         * @throws ArgumentOutOfRangeException if the provided status is not a defined TaskStatus value.
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
         *
         * @return void
         */
        public void MarkCompleted()
        {
            Status = TaskStatus.Completed;
            CompletedAt = DateTime.Now;
        }

        /**
         * Update the task's priority.
         *
         * @param priority The new priority to set.
         * @throws ArgumentOutOfRangeException if the provided priority is not a defined PriorityLevel value.
         */
        public void UpdatePriority(PriorityLevel priority)
        {
            if (!Enum.IsDefined(typeof(PriorityLevel), priority))
                throw new ArgumentOutOfRangeException(nameof(priority), "Priority must be a defined PriorityLevel value.");

            Priority = priority;
        }

        /**
         * Convert the task to a human-readable string.
         *
         * @return A string representation of the task including title, priority, status and timestamps.
         */
        public override string ToString()
        {
            var due = DueDate.HasValue ? $" Due: {DueDate.Value:yyyy-MM-dd}" : string.Empty;
            var category = string.IsNullOrWhiteSpace(Category) ? string.Empty : $" Category: {Category}";
            var created = $" Created: {CreatedAt:yyyy-MM-dd HH:mm}";
            var completed = CompletedAt.HasValue ? $" Completed: {CompletedAt:yyyy-MM-dd HH:mm}" : string.Empty;

            return $"{Title} [{Priority}] - {Status}{due}{category}{created}{completed}";
        }
    }
}
