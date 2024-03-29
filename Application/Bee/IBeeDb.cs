﻿using System;
using System.Collections.Generic;

namespace Application.Bee
{
    public interface IBeeDb
    {
        void CreateTask(RunningTask task);

        void UpdateTask(RunningTask task);

        void DeleteTask(Guid taskId);

        IEnumerable<RunningTask> FetchTasks();
    }
}
