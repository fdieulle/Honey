using System;
using System.Collections.Generic;

namespace Application.Ninja
{
    public interface INinjaDb
    {
        void CreateTask(RunningTask task);

        void UpdateTask(RunningTask task);

        void DeleteTask(Guid taskId);

        IEnumerable<RunningTask> FetchTasks();
    }
}
