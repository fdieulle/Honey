using Application.Dojo.Pipelines;
using Domain.Dtos;
using Domain.Dtos.Pipelines;
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
        private readonly Dictionary<Guid, Pipeline> _pipelines = new Dictionary<Guid, Pipeline>();

        public Shogun(QueueProvider queueProvider, ITaskTracker taskTracker, IDojoDb db)
        {
            _queueProvider = queueProvider;
            _taskTracker = taskTracker;
            _db = db;

            foreach(var pipeline in _db.FetchPipelines())
                _pipelines[pipeline.Id] = new Pipeline(pipeline, GetJobFactory(pipeline.QueueName), db);
        }

        public Guid Execute(PipelineParameters parameters)
        {
            var factory = GetJobFactory(parameters.QueueName);
            var pipeline = new Pipeline(parameters, factory, _db);
            _pipelines.Add(pipeline.Id, pipeline);

            pipeline.Start();

            return pipeline.Id;
        }

        public Guid ExecuteTask(string name, string queueName, TaskParameters task) 
            => Execute(new PipelineParameters { Name = name, QueueName = queueName, RootJob = new SingleTaskJobParameters { Name = name, StartTask = task } });

        public void Cancel(Guid id)
        {
            if (!_pipelines.TryGetValue(id, out var pipeline))
                return;

            pipeline.Cancel();
        }

        public void Delete(Guid id)
        {
            if (!_pipelines.TryGetValue(id, out var pipeline))
                return;

            pipeline.Delete();
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
