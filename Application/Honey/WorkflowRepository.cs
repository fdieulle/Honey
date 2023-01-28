using Domain.Dtos.Workflows;
using Domain.Dtos;
using Domain.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Application.Dojo;

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
        {
            return _workflows.Values.ToList();
        }

        public WorkflowViewModel GetWorkflow(Guid id)
            => _workflows.TryGetValue(id, out var workflow) ? workflow : null;

        public JobViewModel GetWorkflowJobs(Guid id)
            => _wfTojobs.TryGetValue(id, out var jobId) && _jobs.TryGetValue(jobId, out var job)
                ? job : JobViewModel.Empty;

        //private List<WorkflowDto> _mockW;
        //private List<JobDto> _mockJ;
        //private List<RemoteTaskDto> _mockT;
        public void Refresh()
        {
            var workflows = _colony.GetWorkflows();
            var jobs = _colony.GetJobs();
            var tasks = _colony.GetTasks();

            //if (_mockW == null)
            //    (_mockW, _mockJ, _mockT) = CreateWorkflows();
            //else
            //{
            //    foreach (var t in _mockT)
            //        t.BeeState.ProgressPercent = Math.Min(1, t.BeeState.ProgressPercent + 0.1);
            //}
            //(workflows, jobs, tasks) = (_mockW, _mockJ, _mockT);

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

        private (List<WorkflowDto>, List<JobDto>, List<RemoteTaskDto>) CreateWorkflows()
        {
            var workflows = new List<WorkflowDto>();
            var jobs = new List<JobDto>();
            var tasks = new List<RemoteTaskDto>();

            var (wfJobs, wfTasks) = CreateMapReduce("Workflow 1", 3);
            workflows.Add(new WorkflowDto
            {
                Id = Guid.NewGuid(),
                Name = "Workflow 1",
                QueueName = "Queue 1",
                RootJobId = wfJobs.Last().Id
            });
            jobs.AddRange(wfJobs);
            tasks.AddRange(wfTasks);

            (wfJobs, wfTasks) = CreateMapReduce("Workflow 2", 3);
            workflows.Add(new WorkflowDto
            {
                Id = Guid.NewGuid(),
                Name = "Workflow 2",
                QueueName = "Queue 1",
                RootJobId = wfJobs.Last().Id
            });
            jobs.AddRange(wfJobs);
            tasks.AddRange(wfTasks);

            (wfJobs, wfTasks) = CreateMapReduce("Workflow 3", 3);
            workflows.Add(new WorkflowDto
            {
                Id = Guid.NewGuid(),
                Name = "Workflow 3",
                QueueName = "Queue 1",
                RootJobId = wfJobs.Last().Id
            });
            wfJobs.Last().Status = JobStatus.Cancel;
            jobs.AddRange(wfJobs);

            tasks.AddRange(wfTasks); (wfJobs, wfTasks) = CreateMapReduce("Workflow 4", 3);
            workflows.Add(new WorkflowDto
            {
                Id = Guid.NewGuid(),
                Name = "Workflow 4",
                QueueName = "Queue 1",
                RootJobId = wfJobs.Last().Id
            });
            wfJobs.Last().Status = JobStatus.Deleted;
            jobs.AddRange(wfJobs);
            tasks.AddRange(wfTasks);

            (wfJobs, wfTasks) = CreateMapReduce("Workflow 5", 3);
            workflows.Add(new WorkflowDto
            {
                Id = Guid.NewGuid(),
                Name = "Workflow 5",
                QueueName = "Queue 1",
                RootJobId = wfJobs.Last().Id
            });
            wfJobs.Last().Status = JobStatus.Completed;
            jobs.AddRange(wfJobs);
            tasks.AddRange(wfTasks);

            return (workflows, jobs, tasks);
        }

        private (List<JobDto>, List<RemoteTaskDto>) CreateMapReduce(string name, int nbParallels)
        {
            var jobs = new List<JobDto>();
            var tasks = new List<RemoteTaskDto>();

            for (var i = 0; i < nbParallels; i++)
            {
                var (job, task) = CreateTask($"Map {i + 1}", "python");
                jobs.Add(job);
                tasks.Add(task);
            }

            var mapperJob = new ManyJobsDto
            {
                Id = Guid.NewGuid(),
                Name = $"Map",
                Behavior = JobsBehavior.Parallel,
                JobIds = jobs.Select(p => p.Id).ToArray(),
                Status = JobStatus.Running,
            };
            jobs.Add(mapperJob);

            var (reducerJob, reducerTask) = CreateTask("Reduce", "python");
            reducerJob.Status = JobStatus.Pending;
            reducerTask.Status = RemoteTaskStatus.Pending;
            reducerTask.BeeState.StartTime = default;
            reducerTask.BeeState.ProgressPercent = 0;
            jobs.Add(reducerJob);
            tasks.Add(reducerTask);

            jobs.Add(new ManyJobsDto
            {
                Id = Guid.NewGuid(),
                Name = name,
                Behavior = JobsBehavior.Sequential,
                JobIds = new[] { mapperJob.Id, reducerJob.Id },
                Status = JobStatus.Running,
            });

            return (jobs, tasks);
        }

        private (SingleTaskJobDto, RemoteTaskDto) CreateTask(string name, string command)
        {
            var parameters = new TaskParameters
            {
                Command = command,
                NbCores = 1
            };
            var task = new RemoteTaskDto
            {
                Id = Guid.NewGuid(),
                Name = name,
                BeeAddress = "Bee 1",
                Status = RemoteTaskStatus.Running,
                BeeState = new TaskDto
                {
                    StartTime = DateTime.Now,
                    ProgressPercent = 0.2,
                    Status = TaskStatus.Running
                },
                QueueName = "Queue 1"
            };
            var job = new SingleTaskJobDto
            {
                Id = Guid.NewGuid(),
                Name = name,
                Parameters = parameters,
                Status = JobStatus.Running,
                TaskId = task.Id
            };
            return (job, task);
        }

        public void Dispose()
        {
            _timer.Updated -= Refresh;
        }
    }
}
