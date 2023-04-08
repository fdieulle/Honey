using Application.Colony;
using Domain.Dtos;
using NSubstitute;
using System;
using System.Collections.Generic;
using Xunit;

namespace Application.Tests.Colony
{
    public class ColonyTests
    {
        [Fact]
        public void TestExecuteSimpleTask()
        {
            var database = Substitute.For<IColonyDb>();
            var container = Substitute.For<IBeeFactory>();
            var beeKeeper = new BeeKeeper(container, database);
            var dispatcherFactory = new SynchronousDispatcherFactory();
            var taskTracker = new TaskTracker();
            var beehiveProvider = new BeehiveProvider(beeKeeper, dispatcherFactory, database, taskTracker);
            var colony = new Application.Colony.Colony(beehiveProvider, dispatcherFactory, taskTracker, database);

            // Setup a bee
            var bee = beeKeeper.SetupBee("http://bee1:8080");

            // Create a beehive
            beehiveProvider.CreateBeehive("beehive");

            var id = colony.ExecuteTask("name", "beehive", "user", T("powershell", "-version"));

            Assert.NotEqual(id, Guid.Empty);
            bee.Received().StartTask(
                Arg.Is("powershell"),
                Arg.Is("-version"),
                Arg.Is(1));
        }

        [Fact]
        public void TestCancelSimpleTask()
        {
            var database = Substitute.For<IColonyDb>();
            var container = Substitute.For<IBeeFactory>();
            var beeKeeper = new BeeKeeper(container, database);
            var dispatcherFactory = new SynchronousDispatcherFactory();
            var taskTracker = new TaskTracker();
            var beehiveProvider = new BeehiveProvider(beeKeeper, dispatcherFactory, database, taskTracker);
            var colony = new Application.Colony.Colony(beehiveProvider, dispatcherFactory, taskTracker, database);
            var beeTaskIds = new List<Guid>();

            // Setup a bee
            var bee = beeKeeper.SetupBee("http://bee1:8080", beeTaskIds);

            // Create a beehive
            beehiveProvider.CreateBeehive("beehive");

            var id = colony.ExecuteTask("name", "beehive", "user", T("powershell", "-version"));

            Assert.NotEqual(id, Guid.Empty);
            bee.Received().StartTask(
                Arg.Is("powershell"),
                Arg.Is("-version"),
                Arg.Is(1));
            Assert.NotEqual(id, beeTaskIds[0]);

            taskTracker.Refresh();

            colony.Cancel(id);

            bee.Received().CancelTask(Arg.Is(beeTaskIds[0]));
        }

        [Fact]
        public void TestExecuteMultipleTasks()
        {
            var database = Substitute.For<IColonyDb>();
            var container = Substitute.For<IBeeFactory>();
            var beeKeeper = new BeeKeeper(container, database);
            var dispatcherFactory = new SynchronousDispatcherFactory();
            var taskTracker = new TaskTracker();
            var beehiveProvider = new BeehiveProvider(beeKeeper, dispatcherFactory, database, taskTracker);
            var colony = new Application.Colony.Colony(beehiveProvider, dispatcherFactory, taskTracker, database);
            var beeTaskIds = new List<Guid>();

            // Setup a bee
            var bee = beeKeeper.SetupBee("http://bee1:8080", beeTaskIds);

            // Create a Beehive
            beehiveProvider.CreateBeehive("beehive");

            var id1 = colony.ExecuteTask("name1", "beehive", "user", T("powershell", "-version"));
            var id2 = colony.ExecuteTask("name2", "beehive", "user", T("powershell", "-version"));
            var id3 = colony.ExecuteTask("name", "beehive", "user", T("powershell", "-version"));

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

            taskTracker.Refresh();

            colony.Cancel(id2);
            colony.Cancel(id3);

            bee.DidNotReceive().CancelTask(Arg.Is(beeTaskIds[0]));
            bee.Received().CancelTask(Arg.Is(beeTaskIds[1]));
            bee.Received().CancelTask(Arg.Is(beeTaskIds[2]));
        }

        [Fact]
        public void TestHangingTask()
        {
            var database = Substitute.For<IColonyDb>();
            var container = Substitute.For<IBeeFactory>();
            var beeKeeper = new BeeKeeper(container, database);
            var dispatcherFactory = new SynchronousDispatcherFactory();
            var taskTracker = new TaskTracker();
            var beehiveProvider = new BeehiveProvider(beeKeeper, dispatcherFactory, database, taskTracker);
            var colony = new Application.Colony.Colony(beehiveProvider, dispatcherFactory, taskTracker, database);
            var beeTaskIds = new List<Guid>();

            // Setup a bee
            var bee = beeKeeper.SetupBee("http://bee1:8080", beeTaskIds);

            // Create a Beehive
            beehiveProvider.CreateBeehive("beehive");

            // Turn bee too busy
            beeKeeper.UpdateBeeState("http://bee1:8080", 0);

            var id = colony.ExecuteTask("name", "beehive", "user", T("powershell", "-version"));

            // The task is create into shogun but no sent to a bee yet
            Assert.NotEqual(id, Guid.Empty);
            Assert.Empty(beeTaskIds);
            bee.DidNotReceive().StartTask(
                Arg.Is("powershell"),
                Arg.Is("-version"),
                Arg.Is(1));

            // Make some room on Bee to run the task
            beeKeeper.UpdateBeeState("http://bee1:8080", 2);

            var beehive = beehiveProvider.GetBeehive("beehive");
            beehive.Refresh();
            taskTracker.Refresh();

            bee.Received().StartTask(
                Arg.Is("powershell"),
                Arg.Is("-version"),
                Arg.Is(1));

            Assert.NotEqual(id, beeTaskIds[0]);
            colony.Cancel(id);

            bee.Received().CancelTask(Arg.Is(beeTaskIds[0]));
        }

        [Fact]
        public void TestExecuteTasksInMultipleBeehives()
        {
            var database = Substitute.For<IColonyDb>();
            var container = Substitute.For<IBeeFactory>();
            var beeKeeper = new BeeKeeper(container, database);
            var dispatcherFactory = new SynchronousDispatcherFactory();
            var taskTracker = new TaskTracker();
            var beehiveProvider = new BeehiveProvider(beeKeeper, dispatcherFactory, database, taskTracker);
            var colony = new Application.Colony.Colony(beehiveProvider, dispatcherFactory, taskTracker, database);
            var beeTaskIds = new List<Guid>();

            // Setup a bee
            var bee1 = beeKeeper.SetupBee("http://bee1:8080", beeTaskIds);
            var bee2 = beeKeeper.SetupBee("http://bee2:8080", beeTaskIds);

            // Create a Beehive
            beehiveProvider.CreateBeehive("beehive1", bees: "http://bee1:8080");
            beehiveProvider.CreateBeehive("beehive2", bees: "http://bee2:8080");

            var id1 = colony.ExecuteTask("name", "beehive1", "user", T("powershell", "-version"));
            var id2 = colony.ExecuteTask("name", "beehive2", "user", T("powershell", "-version"));
            var id3 = colony.ExecuteTask("name", "beehive1", "user", T("powershell", "-version"));

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

            taskTracker.Refresh();

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
        public static IBee SetupBee(this BeeKeeper beeKeeper, string address, List<Guid> beeIds = null)
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
            beeKeeper.Container
                .Create(Arg.Is(address))
                .Returns(bee);

            // Enroll a bee and enforce a refresh to set it up
            beeKeeper.EnrollBee(address);
            beeKeeper.GetBee(address)
                .Refresh();

            return bee;
        }

        public static void UpdateBeeState(this BeeKeeper beeKeeper, string address, int nbFreeCores)
        {
            var proxy = beeKeeper.Container.Create(address);
            proxy.GetResources().Returns(R(address, nbFreeCores));

            beeKeeper.GetBee(address)
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

        public static bool CreateBeehive(this IBeehiveProvider provider, string name, int maxParallelTask = -1, params string[] bees)
        {
            return provider.CreateBeehive(new BeehiveDto
            {
                Name = name,
                MaxParallelTasks = maxParallelTask,
                Bees = bees,
            });
        }
    }
}
