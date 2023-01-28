using Application.Beehive;
using Domain.Dtos;
using NSubstitute;
using System;
using System.Collections.Generic;
using Xunit;

namespace Application.Tests.Beehive
{
    public class ColonyTests
    {
        [Fact]
        public void TestExecuteSimpleTask()
        {
            var database = Substitute.For<IBeehiveDb>();
            var container = Substitute.For<IBeeFactory>();
            var beehive = new Application.Beehive.Beehive(container, database);
            var taskTracker = new TaskTracker();
            var queueProvider = new QueueProvider(beehive, database, taskTracker);
            var colony = new Colony(queueProvider, taskTracker, database);

            // Setup a bee
            var bee = beehive.SetupBee("http://bee1:8080");

            // Create a queue
            queueProvider.CreateQueue("queue");

            var id = colony.ExecuteTask("name", "queue", T("powershell", "-version"));

            Assert.NotEqual(id, Guid.Empty);
            bee.Received().StartTask(
                Arg.Is("powershell"),
                Arg.Is("-version"),
                Arg.Is(1));
        }

        [Fact]
        public void TestCancelSimpleTask()
        {
            var database = Substitute.For<IBeehiveDb>();
            var container = Substitute.For<IBeeFactory>();
            var beehive = new Application.Beehive.Beehive(container, database);
            var taskTracker = new TaskTracker();
            var queueProvider = new QueueProvider(beehive, database, taskTracker);
            var colony = new Colony(queueProvider, taskTracker, database);
            var beeTaskIds = new List<Guid>();

            // Setup a bee
            var bee = beehive.SetupBee("http://bee1:8080", beeTaskIds);

            // Create a queue
            queueProvider.CreateQueue("queue");

            var id = colony.ExecuteTask("name", "queue", T("powershell", "-version"));

            Assert.NotEqual(id, Guid.Empty);
            bee.Received().StartTask(
                Arg.Is("powershell"),
                Arg.Is("-version"),
                Arg.Is(1));
            Assert.NotEqual(id, beeTaskIds[0]);

            colony.Cancel(id);

            bee.Received().CancelTask(Arg.Is(beeTaskIds[0]));
        }

        [Fact]
        public void TestExecuteMultipleTasks()
        {
            var database = Substitute.For<IBeehiveDb>();
            var container = Substitute.For<IBeeFactory>();
            var beehive = new Application.Beehive.Beehive(container, database);
            var taskTracker = new TaskTracker();
            var queueProvider = new QueueProvider(beehive, database, taskTracker);
            var colony = new Colony(queueProvider, taskTracker, database);
            var beeTaskIds = new List<Guid>();

            // Setup a bee
            var bee = beehive.SetupBee("http://bee1:8080", beeTaskIds);

            // Create a queue
            queueProvider.CreateQueue("queue");

            var id1 = colony.ExecuteTask("name1", "queue", T("powershell", "-version"));
            var id2 = colony.ExecuteTask("name2", "queue", T("powershell", "-version"));
            var id3 = colony.ExecuteTask("name", "queue", T("powershell", "-version"));

            Assert.NotEqual(id1, Guid.Empty);
            Assert.NotEqual(id1, beeTaskIds[0]);
            Assert.NotEqual(id2, Guid.Empty);
            Assert.NotEqual(id2, beeTaskIds[1]);
            Assert.NotEqual(id3, Guid.Empty);
            Assert.NotEqual(id3, beeTaskIds[2]);
            bee.Received(3).StartTask(
                Arg.Is("powershell"),
                Arg.Is("-version"),
                Arg.Is(1));

            colony.Cancel(id2);
            colony.Cancel(id3);

            bee.DidNotReceive().CancelTask(Arg.Is(beeTaskIds[0]));
            bee.Received().CancelTask(Arg.Is(beeTaskIds[1]));
            bee.Received().CancelTask(Arg.Is(beeTaskIds[2]));
        }

        [Fact]
        public void TestHangingTask()
        {
            var database = Substitute.For<IBeehiveDb>();
            var container = Substitute.For<IBeeFactory>();
            var beehive = new Application.Beehive.Beehive(container, database);
            var taskTracker = new TaskTracker();
            var queueProvider = new QueueProvider(beehive, database, taskTracker);
            var colony = new Colony(queueProvider, taskTracker, database);
            var beeTaskIds = new List<Guid>();

            // Setup a bee
            var bee = beehive.SetupBee("http://bee1:8080", beeTaskIds);

            // Create a queue
            queueProvider.CreateQueue("queue");

            // Turn bee too busy
            beehive.UpdateBeeState("http://bee1:8080", 0);

            var id = colony.ExecuteTask("name", "queue", T("powershell", "-version"));

            // The task is create into shogun but no sent to a bee yet
            Assert.NotEqual(id, Guid.Empty);
            Assert.Empty(beeTaskIds);
            bee.DidNotReceive().StartTask(
                Arg.Is("powershell"),
                Arg.Is("-version"),
                Arg.Is(1));

            // Make some room on Bee to run the task
            beehive.UpdateBeeState("http://bee1:8080", 2);

            var queue = queueProvider.GetQueue("queue");
            queue.Refresh();

            bee.Received().StartTask(
                Arg.Is("powershell"),
                Arg.Is("-version"),
                Arg.Is(1));

            Assert.NotEqual(id, beeTaskIds[0]);
            colony.Cancel(id);

            bee.Received().CancelTask(Arg.Is(beeTaskIds[0]));
        }

        [Fact]
        public void TestExecuteTasksInMultipleQueues()
        {
            var database = Substitute.For<IBeehiveDb>();
            var container = Substitute.For<IBeeFactory>();
            var beehive = new Application.Beehive.Beehive(container, database);
            var taskTracker = new TaskTracker();
            var queueProvider = new QueueProvider(beehive, database, taskTracker);
            var colony = new Colony(queueProvider, taskTracker, database);
            var beeTaskIds = new List<Guid>();

            // Setup a bee
            var bee1 = beehive.SetupBee("http://bee1:8080", beeTaskIds);
            var bee2 = beehive.SetupBee("http://bee2:8080", beeTaskIds);

            // Create a queue
            queueProvider.CreateQueue("queue1", bees: "http://bee1:8080");
            queueProvider.CreateQueue("queue2", bees: "http://bee2:8080");

            var id1 = colony.ExecuteTask("name", "queue1", T("powershell", "-version"));
            var id2 = colony.ExecuteTask("name", "queue2", T("powershell", "-version"));
            var id3 = colony.ExecuteTask("name", "queue1", T("powershell", "-version"));

            Assert.NotEqual(id1, Guid.Empty);
            Assert.NotEqual(id1, beeTaskIds[0]);
            Assert.NotEqual(id2, Guid.Empty);
            Assert.NotEqual(id2, beeTaskIds[1]);
            Assert.NotEqual(id3, Guid.Empty);
            Assert.NotEqual(id3, beeTaskIds[2]);
            bee1.Received(2).StartTask(
                Arg.Is("powershell"),
                Arg.Is("-version"),
                Arg.Is(1));
            bee1.Received().StartTask(
                Arg.Is("powershell"),
                Arg.Is("-version"),
                Arg.Is(1));

            colony.Cancel(id2);
            colony.Cancel(id3);

            bee1.DidNotReceive().CancelTask(Arg.Is(beeTaskIds[0]));
            bee2.Received().CancelTask(Arg.Is(beeTaskIds[1]));
            bee1.Received().CancelTask(Arg.Is(beeTaskIds[2]));
        }

        private static TaskParameters T(string command, string arguments, int nbCores = 1) => new TaskParameters
        {
            Command = command,
            Arguments = arguments,
            NbCores = nbCores,
        };
    }

    public static class ColonyTestsExtensions
    {
        public static IBee SetupBee(this Application.Beehive.Beehive beehive, string address, List<Guid> beeIds = null)
        {
            var bee = Substitute.For<IBee>();
            beeIds ??= new List<Guid>();

            // Setup a bee
            bee.GetResources().Returns(R(address));
            bee.StartTask(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int>())
                .Returns(x =>
                {
                    var id = Guid.NewGuid();
                    beeIds.Add(id);
                    return id;
                });

            // Make it accessible from the container
            beehive.Container
                .Create(Arg.Is(address))
                .Returns(bee);

            // Enroll a bee and enforce a refresh to set it up
            beehive.EnrollBee(address);
            beehive.GetBee(address)
                .Refresh();

            return bee;
        }

        public static void UpdateBeeState(this Application.Beehive.Beehive beehive, string address, int nbFreeCores)
        {
            var proxy = beehive.Container.Create(address);
            proxy.GetResources().Returns(R(address, nbFreeCores));

            beehive.GetBee(address)
                .Refresh();
        }

        private static BeeResourcesDto R(string address, int nbFreeCores = 9) => new BeeResourcesDto()
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

        public static bool CreateQueue(this IQueueProvider provider, string name, int maxParallelTask = -1, params string[] bees)
        {
            return provider.CreateQueue(new QueueDto
            {
                Name = name,
                MaxParallelTasks = maxParallelTask,
                Bees = bees,
            });
        }
    }
}
