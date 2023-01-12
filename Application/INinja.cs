﻿using System;
using System.Collections.Generic;
using Domain.Dtos;

namespace Application
{
    public interface INinja
    {
        IEnumerable<TaskDto> GetTasks();

        IEnumerable<TaskMessageDto> FetchMessages(Guid id, int start, int length);

        Guid StartTask(string command, string arguments, int nbCores = 1);

        void CancelTask(Guid id);

        void DeleteTask(Guid id);

        NinjaResourcesDto GetResources();
    }

    public interface INinjaClient
    {
        void UpdateTask(TaskStateDto state);
    }

    public interface INinjaFactory
    {
        INinja Create(string address);
    }
}
