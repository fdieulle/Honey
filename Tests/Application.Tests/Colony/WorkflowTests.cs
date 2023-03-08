using Domain.Dtos.Workflows;
using Domain.Dtos;
using Xunit;
using System.Text.Json;
using Application.Colony;
using Application.Colony.Workflows;
using NSubstitute;
using System;
using NSubstitute.ReceivedExtensions;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using Xunit.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Tests.Colony
{
    public class WorkflowTests
    {
        [Fact]
        public void MapReduceTest()
        {
            var mapper = new ManyJobsParameters
            {
                Name = "Mapper",
                Behavior = JobsBehavior.Parallel,
                Jobs = new JobParameters[]
                {
                    new SingleTaskJobParameters
                    {
                        Name = "Map 1",
                        Task = new TaskParameters
                        {
                            Command = "cmd map 1"
                        }
                    },
                    new SingleTaskJobParameters
                    {
                        Name = "Map 2",
                        Task = new TaskParameters
                        {
                            Command = "cmd map 2"
                        }
                    },
                    new SingleTaskJobParameters
                    {
                        Name = "Map 3",
                        Task = new TaskParameters
                        {
                            Command = "cmd map 3"
                        }
                    }
                },
            };

            var reducer = new SingleTaskJobParameters
            {
                Name = "Reducer",
                Task = new TaskParameters
                {
                    Command = "cmd reduce"
                }
            };

            var mapReduce = new ManyJobsParameters
            {
                Name = "MapReduce",
                Behavior = JobsBehavior.Sequential,
                Jobs = new JobParameters[] { mapper, reducer },
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = System.Text.Json.JsonSerializer.Serialize<JobParameters>(mapReduce, options);

            var parameters = System.Text.Json.JsonSerializer.Deserialize<JobParameters>(json);

            Assert.NotNull(parameters);
            Assert.IsAssignableFrom<ManyJobsParameters>(parameters);
        }

        [Fact]
        public void NominalCaseForSequentialJobsTests()
        {
            var db = new ColonyDbLogs();
            var factory = Substitute.For<IJobFactory>();

            var (jobParameters, children) = factory.SetupJobs("Child 1", "Child 2", "Child 3");

            var parameters = new ManyJobsParameters { Behavior = JobsBehavior.Sequential, Name = "Sequential", Jobs = jobParameters };
            var job = new SequentialJobs(parameters, factory, db);

            job.Is(JobStatus.Pending);

            job.Start();

            children.ReceivedStart(1, 0, 0);

            job.Is(JobStatus.Running);

            children[0].Update(JobStatus.Running);
            job.Is(JobStatus.Running);

            children[0].Update(JobStatus.Completed);
            job.Is(JobStatus.Running);

            children.ReceivedStart(1, 1, 0);

            children[1].Update(JobStatus.Running);
            job.Is(JobStatus.Running);

            children[1].Update(JobStatus.Completed);
            job.Is(JobStatus.Running);

            children.ReceivedStart(1, 1, 1);

            children[2].Update(JobStatus.Running);
            job.Is(JobStatus.Running);

            children[2].Update(JobStatus.Completed);
            job.Is(JobStatus.Completed);

            
            children.ReceivedDelete(0, 0, 0);
            job.Delete();

            children.ReceivedStart(1, 1, 1);
            children.ReceivedCancel(0, 0, 0);
            children.ReceivedRecover(0, 0, 0);
            children.ReceivedDelete(1, 1, 1);
        }

        [Fact]
        public void RecoverSequentialJobsAfterErrorTests()
        {
            var db = new ColonyDbLogs();
            var factory = Substitute.For<IJobFactory>();

            var (jobParameters, children) = factory.SetupJobs("Child 1", "Child 2", "Child 3");

            var parameters = new ManyJobsParameters { Behavior = JobsBehavior.Sequential, Name = "Sequential", Jobs = jobParameters };
            var job = new SequentialJobs(parameters, factory, db);

            job.Is(JobStatus.Pending);

            job.Start();

            children.ReceivedStart(1, 0, 0);

            job.Is(JobStatus.Running);

            children[0].Update(JobStatus.Running);
            job.Is(JobStatus.Running);

            children[0].Update(JobStatus.Error);
            job.Is(JobStatus.Error);

            
            // First recover
            job.Recover();

            children.ReceivedRecover(1, 0, 0);

            children[0].Update(JobStatus.Running);
            job.Is(JobStatus.Running);

            children[0].Update(JobStatus.Completed);
            job.Is(JobStatus.Running);

            children.ReceivedStart(1, 1, 0);

            children[1].Update(JobStatus.Running);
            job.Is(JobStatus.Running);

            children[1].Update(JobStatus.Error);
            job.Is(JobStatus.Error);

            // 2nd recover
            job.Recover();

            children.ReceivedRecover(1, 1, 0);

            children[1].Update(JobStatus.Running);
            job.Is(JobStatus.Running);

            children[1].Update(JobStatus.Completed);
            job.Is(JobStatus.Running);

            children.ReceivedStart(1, 1, 1);

            children[2].Update(JobStatus.Running);
            job.Is(JobStatus.Running);

            children[2].Update(JobStatus.Error);
            job.Is(JobStatus.Error);

            // 3rd recover
            job.Recover();

            children.ReceivedRecover(1, 1, 1);

            children[2].Update(JobStatus.Running);
            job.Is(JobStatus.Running);

            children[2].Update(JobStatus.Completed);
            job.Is(JobStatus.Completed);

            children.ReceivedDelete(0, 0, 0);
            job.Delete();

            children.ReceivedStart(1, 1, 1);
            children.ReceivedCancel(0, 0, 0);
            children.ReceivedRecover(1, 1, 1);
            children.ReceivedDelete(1, 1, 1);
        }

        [Fact]
        public void NominalCaseForParallelJobsTests()
        {
            var db = new ColonyDbLogs();
            var factory = Substitute.For<IJobFactory>();

            var (jobParameters, children) = factory.SetupJobs("Child 1", "Child 2", "Child 3");

            var parameters = new ManyJobsParameters { Behavior = JobsBehavior.Parallel, Name = "Parallel", Jobs = jobParameters };
            var job = new ParallelJobs(parameters, factory, db);

            job.Is(JobStatus.Pending);

            job.Start();

            children.ReceivedStart(1, 1, 1);

            job.Is(JobStatus.Running);

            children[0].Update(JobStatus.Running);
            job.Is(JobStatus.Running);

            children[0].Update(JobStatus.Completed);
            job.Is(JobStatus.Running);

            children[1].Update(JobStatus.Running);
            children[2].Update(JobStatus.Running);
            job.Is(JobStatus.Running);

            children[2].Update(JobStatus.Completed);
            job.Is(JobStatus.Running);

            children[1].Update(JobStatus.Completed);
            job.Is(JobStatus.Completed);

            children.ReceivedDelete(0, 0, 0);
            job.Delete();

            children.ReceivedStart(1, 1, 1);
            children.ReceivedCancel(0, 0, 0);
            children.ReceivedRecover(0, 0, 0);
            children.ReceivedDelete(1, 1, 1);
        }

        [Fact]
        public void ParallelJobsWithOneInErrorThenAllowCancelOthersTests()
        {
            var db = new ColonyDbLogs();
            var factory = Substitute.For<IJobFactory>();

            var (jobParameters, children) = factory.SetupJobs("Child 1", "Child 2", "Child 3");

            var parameters = new ManyJobsParameters { Behavior = JobsBehavior.Parallel, Name = "Parallel", Jobs = jobParameters };
            var job = new ParallelJobs(parameters, factory, db);

            job.Is(JobStatus.Pending);

            job.Start();

            children.ReceivedStart(1, 1, 1);

            job.Is(JobStatus.Running);

            children[0].Update(JobStatus.Running);
            children[1].Update(JobStatus.Running);
            children[2].Update(JobStatus.Running);
            job.Is(JobStatus.Running);

            children[1].Update(JobStatus.Error);
            job.Is(JobStatus.Error);

            children[2].Update(JobStatus.Completed);
            job.Is(JobStatus.Error);

            job.Cancel();
            children.ReceivedCancel(1, 0, 0);
            job.Is(JobStatus.CancelRequested);

            children[0].Update(JobStatus.Cancel);
            job.Is(JobStatus.Error);

            children.ReceivedDelete(0, 0, 0);
            job.Delete();

            children.ReceivedStart(1, 1, 1);
            children.ReceivedCancel(1, 0, 0);
            children.ReceivedRecover(0, 0, 0);
            children.ReceivedDelete(1, 1, 1);
        }

        [Fact]
        public void ParallelJobsWithOneInErrorThenCancelOthersThenRecoverTests()
        {
            var db = new ColonyDbLogs();
            var factory = Substitute.For<IJobFactory>();

            var (jobParameters, children) = factory.SetupJobs("Child 1", "Child 2", "Child 3");

            var parameters = new ManyJobsParameters { Behavior = JobsBehavior.Parallel, Name = "Parallel", Jobs = jobParameters };
            var job = new ParallelJobs(parameters, factory, db);

            job.Is(JobStatus.Pending);

            job.Start();

            children.ReceivedStart(1, 1, 1);

            job.Is(JobStatus.Running);

            children[0].Update(JobStatus.Running);
            children[1].Update(JobStatus.Running);
            children[2].Update(JobStatus.Running);
            job.Is(JobStatus.Running);

            children[1].Update(JobStatus.Error);
            job.Is(JobStatus.Error);

            children[2].Update(JobStatus.Completed);
            job.Is(JobStatus.Error);

            job.Cancel();
            children.ReceivedCancel(1, 0, 0);
            job.Is(JobStatus.CancelRequested);

            children[0].Update(JobStatus.Cancel);
            job.Is(JobStatus.Error);

            job.Recover();
            children.ReceivedRecover(1, 1, 0);

            job.Is(JobStatus.Running);

            children[0].Update(JobStatus.Running);
            children[1].Update(JobStatus.Running);
            job.Is(JobStatus.Running);

            children[1].Update(JobStatus.Completed);
            job.Is(JobStatus.Running);

            children[0].Update(JobStatus.Completed);
            job.Is(JobStatus.Completed);

            children.ReceivedDelete(0, 0, 0);
            job.Delete();

            children.ReceivedStart(1, 1, 1);
            children.ReceivedCancel(1, 0, 0);
            children.ReceivedRecover(1, 1, 0);
            children.ReceivedDelete(1, 1, 1);
        }

        [Fact]
        public void TestCanDeleteParallelJobs()
        {
            var db = new ColonyDbLogs();
            var factory = Substitute.For<IJobFactory>();

            var (jobParameters, children) = factory.SetupJobs("Child 1", "Child 2", "Child 3");

            var parameters = new ManyJobsParameters { Behavior = JobsBehavior.Sequential, Name = "Sequential", Jobs = jobParameters };
            var job = new ParallelJobs(parameters, factory, db);

            job.Is(JobStatus.Pending)
                .CanCancel(true)
                .CanRecover(false)
                .CanDelete(true);

            job.Start();

            children.ReceivedStart(1, 1, 1);

            children[0].Update(JobStatus.Running);
            children[1].Update(JobStatus.Running);
            children[2].Update(JobStatus.Running);
            job.Is(JobStatus.Running);


            children[0].Update(JobStatus.Completed);
            job.Is(JobStatus.Running)
                .CanCancel(true)
                .CanRecover(false)
                .CanDelete(false);

            children[2].Update(JobStatus.Completed);
            job.Is(JobStatus.Running)
                .CanCancel(true)
                .CanRecover(false)
                .CanDelete(false);

            children[1].Update(JobStatus.Completed);
            job.Is(JobStatus.Completed)
                .CanCancel(false)
                .CanRecover(false)
                .CanDelete(true);


            children.ReceivedDelete(0, 0, 0);
            job.Delete();

            children.ReceivedStart(1, 1, 1);
            children.ReceivedCancel(0, 0, 0);
            children.ReceivedRecover(0, 0, 0);
            children.ReceivedDelete(1, 1, 1);
        }

        private readonly ITestOutputHelper _output;

        public WorkflowTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ActionBlockTest()
        {
            var queue = new Queue<int>();
            var block = new ActionBlock<Func<ValueTask>>(async a =>
            {
                try { await a(); }
                catch (Exception ex) { _output.WriteLine($"Exception: {ex.Message}"); }
            });

            const int P = 10;
            const int N = 100000;
            var tasks = new List<Task>();
            for (var j = 0; j < P / 2; j++)
            {
                var jClosure = j;
                tasks.Add(Task.Run(async () =>
                {
                    for (var k = 0; k < N; k++)
                        await block.SendAsync(async () => { queue.Enqueue(jClosure); await Task.CompletedTask; });
                }));
            }
            for (var j = P/2; j < P; j++)
            {
                var jClosure = j;
                tasks.Add(Task.Run(() =>
                {
                    for (var k = 0; k < N; k++)
                        block.Post(() => { queue.Enqueue(jClosure); return ValueTask.CompletedTask; });
                }));
            }
            Task.WaitAll(tasks.ToArray());
            
            _output.WriteLine("Prior complete");
            block.Complete();
            _output.WriteLine("After complete");
            block.Completion.Wait();
            _output.WriteLine("After completion done");

            var counters = new int[P];
            while(queue.Count > 0)
                counters[queue.Dequeue()]++;

            foreach (var counter in counters)
                Assert.Equal(N, counter);
        }
    }

    public static class WorkflowTestsExtensions
    {
        public static IJob Is(this IJob job, JobStatus status) 
        { 
            Assert.Equal(status, job.Status);
            return job;
        }

        public static void Update(this IJob job, JobStatus status)
        {
            job.Status.Returns(status);
            job.Updated += Raise.Event<Action<IJob>>(job);
        }

        public static void ReceivedStart(this IJob[] jobs, params int[] counts)
        {
            Assert.Equal(jobs.Length, counts.Length);
            for (int i = 0; i < jobs.Length; i++)
                jobs[i].Received(counts[i]).Start();
        }

        public static void ReceivedCancel(this IJob[] jobs, params int[] counts)
        {
            Assert.Equal(jobs.Length, counts.Length);
            for (int i = 0; i < jobs.Length; i++)
                jobs[i].Received(counts[i]).Cancel();
        }

        public static void ReceivedRecover(this IJob[] jobs, params int[] counts)
        {
            Assert.Equal(jobs.Length, counts.Length);
            for (int i = 0; i < jobs.Length; i++)
                jobs[i].Received(counts[i]).Recover();
        }

        public static void ReceivedDelete(this IJob[] jobs, params int[] counts)
        {
            Assert.Equal(jobs.Length, counts.Length);
            for (int i = 0; i < jobs.Length; i++)
                jobs[i].Received(counts[i]).Delete();
        }

        public static IJob CanCancel(this IJob job, bool canCancel)
        {
            Assert.True(job.CanCancel() == canCancel, "CanCancel");
            return job;
        }

        public static IJob CanRecover(this IJob job, bool cancRecover)
        {
            Assert.True(job.CanRecover() == cancRecover, "CanRecover");
            return job;
        }

        public static IJob CanDelete(this IJob job, bool canDelete)
        {
            Assert.True(job.CanDelete() == canDelete, "CanDelete");
            return job;
        }

        public static (JobParameters[], IJob[]) SetupJobs(this IJobFactory factory, params string[] children)
        {
            var parameters = new JobParameters[children.Length];
            var jobs = new IJob[children.Length];
            for (int i = 0; i < children.Length; i++)
            {
                var name = children[i];
                parameters[i] = new SingleTaskJobParameters { Name = name };
                jobs[i] = Substitute.For<IJob>();
                factory.CreateJob(Arg.Is<JobParameters>(p => p.Name == name)).Returns(jobs[i]);
            }

            return (parameters, jobs);
        }
    }
}
