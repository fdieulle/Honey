using Application.Dojo;
using Domain.Dtos;
using NSubstitute;
using System;
using System.Collections.Generic;
using Xunit;

namespace Application.Tests.Dojo
{
    public class ShogunTests
    {
        [Fact]
        public void TestExecuteSimpleTask()
        {
            var database = Substitute.For<IDojoDb>();
            var container = Substitute.For<INinjaFactory>();
            var dojo = new Application.Dojo.Dojo(container, database);
            var queueProvider = new QueueProvider(dojo, database);
            var shogun = new Shogun(queueProvider);

            // Setup a ninja
            var ninja = dojo.SetupNinja("http://ninja1:8080");

            // Create a queue
            queueProvider.CreateQueue("queue");

            var id = shogun.Execute("queue", "name", T("powershell", "-version"));

            Assert.NotEqual(id, Guid.Empty);
            ninja.Received().StartTask(
                Arg.Is("powershell"),
                Arg.Is("-version"),
                Arg.Is(1));
        }

        [Fact]
        public void TestCancelSimpleTask()
        {
            var database = Substitute.For<IDojoDb>();
            var container = Substitute.For<INinjaFactory>();
            var dojo = new Application.Dojo.Dojo(container, database);
            var queueProvider = new QueueProvider(dojo, database);
            var shogun = new Shogun(queueProvider);
            var ninjaTaskIds = new List<Guid>();

            // Setup a ninja
            var ninja = dojo.SetupNinja("http://ninja1:8080", ninjaTaskIds);

            // Create a queue
            queueProvider.CreateQueue("queue");

            var id = shogun.Execute("queue", "name", T("powershell", "-version"));

            Assert.NotEqual(id, Guid.Empty);
            ninja.Received().StartTask(
                Arg.Is("powershell"),
                Arg.Is("-version"),
                Arg.Is(1));
            Assert.NotEqual(id, ninjaTaskIds[0]);

            shogun.Cancel(id);

            ninja.Received().CancelTask(Arg.Is(ninjaTaskIds[0]));
        }

        [Fact]
        public void TestExecuteMultipleTasks()
        {
            var database = Substitute.For<IDojoDb>();
            var container = Substitute.For<INinjaFactory>();
            var dojo = new Application.Dojo.Dojo(container, database);
            var queueProvider = new QueueProvider(dojo, database);
            var shogun = new Shogun(queueProvider);
            var ninjaTaskIds = new List<Guid>();

            // Setup a ninja
            var ninja = dojo.SetupNinja("http://ninja1:8080", ninjaTaskIds);

            // Create a queue
            queueProvider.CreateQueue("queue");

            var id1 = shogun.Execute("queue", "name1", T("powershell", "-version"));
            var id2 = shogun.Execute("queue", "name2", T("powershell", "-version"));
            var id3 = shogun.Execute("queue", "name", T("powershell", "-version"));

            Assert.NotEqual(id1, Guid.Empty);
            Assert.NotEqual(id1, ninjaTaskIds[0]);
            Assert.NotEqual(id2, Guid.Empty);
            Assert.NotEqual(id2, ninjaTaskIds[1]);
            Assert.NotEqual(id3, Guid.Empty);
            Assert.NotEqual(id3, ninjaTaskIds[2]);
            ninja.Received(3).StartTask(
                Arg.Is("powershell"),
                Arg.Is("-version"),
                Arg.Is(1));

            shogun.Cancel(id2);
            shogun.Cancel(id3);

            ninja.DidNotReceive().CancelTask(Arg.Is(ninjaTaskIds[0]));
            ninja.Received().CancelTask(Arg.Is(ninjaTaskIds[1]));
            ninja.Received().CancelTask(Arg.Is(ninjaTaskIds[2]));
        }

        [Fact]
        public void TestHangingTask()
        {
            var database = Substitute.For<IDojoDb>();
            var container = Substitute.For<INinjaFactory>();
            var dojo = new Application.Dojo.Dojo(container, database);
            var queueProvider = new QueueProvider(dojo, database);
            var shogun = new Shogun(queueProvider);
            var ninjaTaskIds = new List<Guid>();

            // Setup a ninja
            var ninja = dojo.SetupNinja("http://ninja1:8080", ninjaTaskIds);

            // Create a queue
            queueProvider.CreateQueue("queue");

            // Turn ninja too busy
            dojo.UpdateNinjaState("http://ninja1:8080", 0);

            var id = shogun.Execute("queue", "name", T("powershell", "-version"));

            // The task is create into shogun but no sent to a ninja yet
            Assert.NotEqual(id, Guid.Empty);
            Assert.Empty(ninjaTaskIds);
            ninja.DidNotReceive().StartTask(
                Arg.Is("powershell"),
                Arg.Is("-version"),
                Arg.Is(1));

            // Make some room on Ninja to run the task
            dojo.UpdateNinjaState("http://ninja1:8080", 2);

            var queue = queueProvider.GetQueue("queue");
            queue.Refresh();

            ninja.Received().StartTask(
                Arg.Is("powershell"),
                Arg.Is("-version"),
                Arg.Is(1));

            Assert.NotEqual(id, ninjaTaskIds[0]);
            shogun.Cancel(id);

            ninja.Received().CancelTask(Arg.Is(ninjaTaskIds[0]));
        }

        [Fact]
        public void TestExecuteTasksInMultipleQueues()
        {
            var database = Substitute.For<IDojoDb>();
            var container = Substitute.For<INinjaFactory>();
            var dojo = new Application.Dojo.Dojo(container, database);
            var queueProvider = new QueueProvider(dojo, database);
            var shogun = new Shogun(queueProvider);
            var ninjaTaskIds = new List<Guid>();

            // Setup a ninja
            var ninja1 = dojo.SetupNinja("http://ninja1:8080", ninjaTaskIds);
            var ninja2 = dojo.SetupNinja("http://ninja2:8080", ninjaTaskIds);

            // Create a queue
            queueProvider.CreateQueue("queue1", ninjas: "http://ninja1:8080");
            queueProvider.CreateQueue("queue2", ninjas: "http://ninja2:8080");

            var id1 = shogun.Execute("queue1", "name", T("powershell", "-version"));
            var id2 = shogun.Execute("queue2", "name", T("powershell", "-version"));
            var id3 = shogun.Execute("queue1", "name", T("powershell", "-version"));

            Assert.NotEqual(id1, Guid.Empty);
            Assert.NotEqual(id1, ninjaTaskIds[0]);
            Assert.NotEqual(id2, Guid.Empty);
            Assert.NotEqual(id2, ninjaTaskIds[1]);
            Assert.NotEqual(id3, Guid.Empty);
            Assert.NotEqual(id3, ninjaTaskIds[2]);
            ninja1.Received(2).StartTask(
                Arg.Is("powershell"),
                Arg.Is("-version"),
                Arg.Is(1));
            ninja1.Received().StartTask(
                Arg.Is("powershell"),
                Arg.Is("-version"),
                Arg.Is(1));

            shogun.Cancel(id2);
            shogun.Cancel(id3);

            ninja1.DidNotReceive().CancelTask(Arg.Is(ninjaTaskIds[0]));
            ninja2.Received().CancelTask(Arg.Is(ninjaTaskIds[1]));
            ninja1.Received().CancelTask(Arg.Is(ninjaTaskIds[2]));
        }

        private static StartTaskDto T(string command, string arguments, int nbCores = 1) => new StartTaskDto
        {
            Command = command,
            Arguments = arguments,
            NbCores = nbCores,
        };
    }

    public static class ShogunTestsExtensions
    {
        public static INinja SetupNinja(this Application.Dojo.Dojo dojo, string address, List<Guid> ninjaIds = null)
        {
            var ninja = Substitute.For<INinja>();
            ninjaIds ??= new List<Guid>();

            // Setup a ninja
            ninja.GetResources().Returns(R(address));
            ninja.StartTask(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int>())
                .Returns(x =>
                {
                    var id = Guid.NewGuid();
                    ninjaIds.Add(id);
                    return id;
                });

            // Make it accessible from the container
            dojo.Container
                .Create(Arg.Is(address))
                .Returns(ninja);

            // Enroll a ninja and enforce a refresh to set it up
            dojo.EnrollNinja(address);
            dojo.GetNinja(address)
                .Refresh();

            return ninja;
        }

        public static void UpdateNinjaState(this Application.Dojo.Dojo dojo, string address, int nbFreeCores)
        {
            var proxy = dojo.Container.Create(address);
            proxy.GetResources().Returns(R(address, nbFreeCores));

            dojo.GetNinja(address)
                .Refresh();
        }

        private static NinjaResourcesDto R(string address, int nbFreeCores = 9) => new NinjaResourcesDto()
        {
            MachineName = address,
            OSPlatform = "Win32",
            NbCores = 10,
            NbFreeCores = nbFreeCores,
            AvailablePhysicalMemory = (ulong)24e9,
            TotalPhysicalMemory = (ulong)32e9,
            DiskSpace = (ulong)250e9,
            DiskFreeSpace = (ulong)198e9,
        };

        public static bool CreateQueue(this IQueueProvider provider, string name, int maxParallelTask = -1, params string[] ninjas)
        {
            return provider.CreateQueue(new QueueDto
            {
                Name = name,
                MaxParallelTasks = maxParallelTask,
                Ninjas = ninjas,
            });
        }
    }
}
