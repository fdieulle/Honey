using Domain.Dtos.Workflows;
using Domain.Dtos;
using Domain.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Application.Beehive;

namespace Application.Honey
{
    public class WorkflowRepository : IDisposable
    {
        private readonly Colony _colony;
        private readonly ITimer _timer;
        private readonly Repository<Guid, WorkflowViewModel, WorkflowDto> _workflows = new Repository<Guid, WorkflowViewModel, WorkflowDto>(
            p => p.Id, p => p.ToViewModel(), (dto, vm) => vm.Update(dto));
        private readonly Repository<Guid, JobViewModel, JobDto> _jobs = new Repository<Guid, JobViewModel, JobDto>(
            p => p.Id, p => p.ToViewModel(), (dto, vm) => vm.Update(dto));
        private readonly Repository<Guid, RemoteTaskDto> _tasks = new Repository<Guid, RemoteTaskDto>(p => p.Id);

        private readonly Dictionary<Guid, Guid> _wfTojobs = new Dictionary<Guid, Guid>();

        public IRepository<WorkflowViewModel> Workflows => _workflows;

        public WorkflowRepository(Colony colony, ITimer timer)
        {
            _colony = colony;
            _timer = timer;

            Refresh();
            _timer.Updated += Refresh;
        }

        public List<WorkflowViewModel> GetWorkflows() 
            => _workflows.Values.ToList();

        public WorkflowViewModel GetWorkflow(Guid id)
            => _workflows.TryGetValue(id, out var workflow) ? workflow : null;

        public JobViewModel GetWorkflowJobs(Guid id)
            => _wfTojobs.TryGetValue(id, out var jobId) && _jobs.TryGetValue(jobId, out var job)
                ? job : JobViewModel.Empty;

        public void Refresh()
        {
            var workflows = _colony.GetWorkflows();
            var jobs = _colony.GetJobs();
            var tasks = _colony.GetTasks();

            _tasks.Reload(tasks);
            _jobs.Reload(jobs);
            _workflows.Reload(workflows);


            // Build job trees
            foreach (var dto in jobs)
            {
                if (_jobs.TryGetValue(dto.Id, out var vm))
                    vm.Update(dto, _jobs, _tasks);
            }

            // Track all job trees and post process
            _wfTojobs.Clear();
            foreach (var workflow in workflows)
            {
                _wfTojobs[workflow.Id] = workflow.RootJobId;
                if (_jobs.TryGetValue(workflow.RootJobId, out var job))
                {
                    job.UpdateTree();
                    if (_workflows.TryGetValue(workflow.Id, out var vm))
                        vm.Update(job);
                }
            }
        }

        public void Dispose()
        {
            _timer.Updated -= Refresh;
        }
    }
}
