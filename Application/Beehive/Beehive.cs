using Application.Beehive.Workflows;
using Domain.Dtos;
using Domain.Dtos.Workflows;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Beehive
{
    public class Beehive : IBeehive
    {
        private readonly ColonyProvider _colonyProvider;
        private readonly ITaskTracker _taskTracker;
        private readonly IBeehiveDb _db;
        private readonly Dictionary<string, IJobFactory> _factories = new Dictionary<string, IJobFactory>();
        private readonly Dictionary<Guid, Workflow> _workflows = new Dictionary<Guid, Workflow>();

        public Beehive(ColonyProvider colonyProvider, ITaskTracker taskTracker, IBeehiveDb db)
        {
            _colonyProvider = colonyProvider;
            _taskTracker = taskTracker;
            _db = db;

            foreach (var entity in _db.FetchWorkflows())
            {
                var workflow = new Workflow(entity, GetJobFactory(entity.Colony), db);
                _workflows[entity.Id] = workflow;
                workflow.Deleted += OnWorkflowDeleted;
            }
        }

        public Guid Execute(WorkflowParameters parameters)
        {
            var factory = GetJobFactory(parameters.Colony);
            var workflow = new Workflow(parameters, factory, _db);
            workflow.Deleted += OnWorkflowDeleted;

            lock (_workflows)
            {
                _workflows.Add(workflow.Id, workflow);

                workflow.Start();
            }

            return workflow.Id;
        }

        private void OnWorkflowDeleted(Workflow workflow)
        {
            workflow.Deleted -= OnWorkflowDeleted;
            lock (_workflows)
            {
                _workflows.Remove(workflow.Id);
            }
        }

        public Guid ExecuteTask(string name, string colony, TaskParameters task) 
            => Execute(new WorkflowParameters { Name = name, Colony = colony, RootJob = new SingleTaskJobParameters { Name = name, Task = task } });

        public bool Cancel(Guid id)
        {
            lock (_workflows)
            {
                if (!_workflows.TryGetValue(id, out var workflow))
                    return false;

                workflow.Cancel();
                return true;
            }
        }

        public bool Recover(Guid id)
        {
            lock (_workflows)
            {
                if (!_workflows.TryGetValue(id, out var workflow))
                    return false;

                workflow.Recover();

                return true;
            }
        }

        public bool Delete(Guid id)
        {
            lock (_workflows)
            {
                if (!_workflows.TryGetValue(id, out var workflow))
                    return false;

                workflow.Delete();

                return true;
            }
        }

        private IJobFactory GetJobFactory(string colonyName)
        {
            lock (_factories)
            {
                if (!_factories.TryGetValue(colonyName, out var factory))
                {
                    var colony = _colonyProvider.GetColony(colonyName);
                    _factories.Add(colonyName, factory = new JobFactory(colony, _taskTracker, _db));
                }

                return factory;
            }
        }

        public List<RemoteTaskDto> GetTasks()
        {
            return _colonyProvider.GetColonies()
                .Select(p => _colonyProvider.GetColony(p.Name))
                .Where(q => q != null)
                .SelectMany(p => p.GetAllTasks())
                .ToList();
        }

        public List<JobDto> GetJobs()
        {
            lock(_workflows)
            {
                return _workflows.Values
                    .SelectMany(p => p.GetJobs())
                    .ToList();
            }
        }

        public List<WorkflowDto> GetWorkflows()
        {
            lock (_workflows)
            {
                return _workflows.Values
                    .Select(p => p.Dto).ToList();
            }
        }
    } 
}
