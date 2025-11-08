using System.Collections.Generic;

namespace ConsoleApp.Storage
{
    public interface ITaskRepository
    {
        void Save(IEnumerable<ConsoleApp.Models.TaskDetails> tasks, string path);
        List<ConsoleApp.Models.TaskDetails> Load(string path, out int skipped);
    }
}
