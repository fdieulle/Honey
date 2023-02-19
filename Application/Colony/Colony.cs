using Application.Colony.Workflows;
using Domain.Dtos;
using Domain.Dtos.Workflows;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Application.Colony
{
    public class Colony : IColony
    {
        private readonly BeehiveProvider _beehiveProvider;
        private readonly IDispatcherFactory _dispatcherFactory;
        private readonly ITaskTracker _taskTracker;
        private readonly IColonyDb _db;
        private readonly ConcurrentDictionary<Guid, Workflow> _workflows = new ConcurrentDictionary<Guid, Workflow>();

        public Colony(BeehiveProvider beehiveProvider, IDispatcherFactory dispatcherFactory, ITaskTracker taskTracker, IColonyDb db)
        {
            _beehiveProvider = beehiveProvider;
            _dispatcherFactory = dispatcherFactory;
            _taskTracker = taskTracker;
            _db = db;

            foreach (var entity in _db.FetchWorkflows())
            {
                var beehive = _beehiveProvider.GetBeehive(entity.Beehive);
                var workflow = new Workflow(entity, beehive, taskTracker, dispatcherFactory, db);
                _workflows[entity.Id] = workflow;
                workflow.Deleted += OnWorkflowDeleted;
            }
        }

        public Guid Execute(WorkflowParameters parameters)
        {
            var beehive = _beehiveProvider.GetBeehive(parameters.Beehive);
            var workflow = new Workflow(parameters, beehive, _taskTracker, _dispatcherFactory, _db);
            workflow.Deleted += OnWorkflowDeleted;

            _workflows[workflow.Id] = workflow;
            workflow.Start();

            return workflow.Id;
        }

        private void OnWorkflowDeleted(Workflow workflow)
        {
            workflow.Deleted -= OnWorkflowDeleted;
            _workflows.TryRemove(workflow.Id, out _);
        }

        public Guid ExecuteTask(string name, string beehive, TaskParameters task) 
            => Execute(new WorkflowParameters { Name = name, Beehive = beehive, RootJob = new SingleTaskJobParameters { Name = name, Task = task } });

        public bool Cancel(Guid id)
        {
            if (!_workflows.TryGetValue(id, out var workflow))
                return false;

            workflow.Cancel();
            return true;
        }

        public bool Recover(Guid id)
        {
            if (!_workflows.TryGetValue(id, out var workflow))
                return false;

            workflow.Recover();
            return true;
        }

        public bool Delete(Guid id)
        {
            if (!_workflows.TryGetValue(id, out var workflow))
                return false;

            workflow.Delete();
            return true;
        }

        public List<RemoteTaskDto> GetTasks() 
            => _beehiveProvider.GetBeehives()
                .Select(p => _beehiveProvider.GetBeehive(p.Name))
                .Where(q => q != null)
                .SelectMany(p => p.GetAllTasks())
                .ToList();

        public List<JobDto> GetJobs() 
            => _workflows.Values
                .SelectMany(p => p.GetJobs())
                .ToList();

        public List<WorkflowDto> GetWorkflows() 
            => _workflows.Values
                .Select(p => p.Dto)
                .ToList();

        public List<TaskMessageDto> FetchTaskMessages(Guid workflowId, Guid taskId)
        {
            if (!_workflows.TryGetValue(workflowId, out var workflow))
                return new List<TaskMessageDto>();

            var beehive = _beehiveProvider.GetBeehive(workflow.Dto.Beehive);
            if (beehive == null)
                return new List<TaskMessageDto>();

            return beehive.FetchTaskMessages(taskId);
        }
    } 
}
