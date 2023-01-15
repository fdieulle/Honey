﻿using Application.Dojo.Workflows;
using Domain.Dtos;
using Domain.Dtos.Workflows;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Dojo
{
    public class Shogun : IShogun
    {
        private readonly QueueProvider _queueProvider;
        private readonly ITaskTracker _taskTracker;
        private readonly IDojoDb _db;
        private readonly Dictionary<string, IJobFactory> _factories = new Dictionary<string, IJobFactory>();
        private readonly Dictionary<Guid, Workflow> _workflows = new Dictionary<Guid, Workflow>();

        public Shogun(QueueProvider queueProvider, ITaskTracker taskTracker, IDojoDb db)
        {
            _queueProvider = queueProvider;
            _taskTracker = taskTracker;
            _db = db;

            foreach(var workflow in _db.FetchWorkflows())
                _workflows[workflow.Id] = new Workflow(workflow, GetJobFactory(workflow.QueueName), db);
        }

        public Guid Execute(WorkflowParameters parameters)
        {
            var factory = GetJobFactory(parameters.QueueName);
            var workflow = new Workflow(parameters, factory, _db);
            _workflows.Add(workflow.Id, workflow);

            workflow.Start();

            return workflow.Id;
        }

        public Guid ExecuteTask(string name, string queueName, TaskParameters task) 
            => Execute(new WorkflowParameters { Name = name, QueueName = queueName, RootJob = new SingleTaskJobParameters { Name = name, StartTask = task } });

        public void Cancel(Guid id)
        {
            if (!_workflows.TryGetValue(id, out var workflow))
                return;

            workflow.Cancel();
        }

        public void Delete(Guid id)
        {
            if (!_workflows.TryGetValue(id, out var workflow))
                return;

            workflow.Delete();
        }

        public IEnumerable<RemoteTaskDto> GetAllTasks()
        {
            var result = new List<RemoteTaskDto>();
            foreach(var queueName in _queueProvider.GetQueues().Select(p => p.Name))
            {
                var queue = _queueProvider.GetQueue(queueName);
                if (queue == null) continue;
                var allTasks = queue.GetAllTasks();
                if (allTasks == null) continue;

                result.AddRange(allTasks);
            }

            return result;
        }

        private IJobFactory GetJobFactory(string queueName)
        {
            if (!_factories.TryGetValue(queueName, out var factory))
            {
                var queue = _queueProvider.GetQueue(queueName);
                _factories.Add(queueName, factory = new JobFactory(queue, _taskTracker, _db));
            }

            return factory;
        }

    } 
}
