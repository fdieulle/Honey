using Application.Beehive;
using Domain.Dtos;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Application.Tests.Beehive
{
    public class ColonyTests
    {
        private readonly IBeeFactory _factory;
        private readonly BeehiveDbLogs _db;
        private readonly TaskTracker _tracker;
        private readonly BeeKeeper _beeKeeper;
        private readonly Colony _colony;

        public ColonyTests()
        {
            _factory = Substitute.For<IBeeFactory>();
            _db = new BeehiveDbLogs();
            _beeKeeper = new BeeKeeper(_factory, _db);
            _tracker = new TaskTracker();

            _colony = new Colony(ColonyDto("Colony 1"), _beeKeeper, _db, _tracker);
        }

        private void Refresh()
        {
            _beeKeeper.Refresh();
            _colony.Refresh();
            _tracker.Refresh();
        }

        [Fact]
        public void StartAndRunTask()
        {
            var bee = _factory.Setup("Bee 1");
            _beeKeeper.EnrollBee("Bee 1");

            Refresh();

            var taskId = Guid.NewGuid();
            bee.StartTask(Arg.Is("App 1"), Arg.Is("Arg1, Arg2"), Arg.Is(1)).Returns(taskId);

            _colony.StartTask("Task 1", StartTaskDto("App 1", "Arg1, Arg2", 1));

            bee.Received(1).StartTask(Arg.Is("App 1"), Arg.Is("Arg1, Arg2"), Arg.Is(1));
            _db.TaskTable.NextCreate().Check("Task 1", "Colony 1", "Bee 1", "App 1", taskId, RemoteTaskStatus.Running);
            _db.TaskTable.EmptyLogs();

            bee.GetTasks().Returns(new[] { TaskDto(taskId, TaskStatus.Running, 0.1) });

            Refresh();
            _db.TaskTable.NextUpdate().Check("Task 1", "Colony 1", "Bee 1", "App 1", taskId, RemoteTaskStatus.Running, TaskStatus.Running, 0.1);
            _db.TaskTable.EmptyLogs();

            bee.GetTasks().Returns(new[] { TaskDto(taskId, TaskStatus.Running, 0.5) });

            Refresh();
            _db.TaskTable.NextUpdate().Check("Task 1", "Colony 1", "Bee 1", "App 1", taskId, RemoteTaskStatus.Running, TaskStatus.Running, 0.5);
            _db.TaskTable.EmptyLogs();

            bee.GetTasks().Returns(new[] { TaskDto(taskId, TaskStatus.Done, 1.0) });

            Refresh();
            _db.TaskTable.NextUpdate().Check("Task 1", "Colony 1", "Bee 1", "App 1", taskId, RemoteTaskStatus.Completed, TaskStatus.Done, 1.0);
            _db.TaskTable.EmptyLogs();

            Refresh();
            _db.TaskTable.EmptyLogs();
        }

        [Fact]
        public void StartAndRunMultipleTasksOn1Bee()
        {
            var bee = _factory.Setup("Bee 1");
            _beeKeeper.EnrollBee("Bee 1");

            Refresh();

            var task1Id = Guid.NewGuid();
            var task2Id = Guid.NewGuid();

            bee.SetupStartTask("cmd 1", task1Id);
            bee.SetupStartTask("cmd 2", task2Id);

            _colony.StartTask("Task 1", StartTaskDto("cmd 1"));

            bee.CheckStartTask("cmd 1");
            _db.CheckCreateTask("Task 1", "Colony 1", "Bee 1", "cmd 1", task1Id);

            bee.SetupTaskState(TaskDto(task1Id, TaskStatus.Running, 0.1));

            Refresh();
            _db.CheckTaskUpdate("Task 1", "Colony 1", "Bee 1", "cmd 1", task1Id, TaskStatus.Running, 0.1);

            _colony.StartTask("Task 2", StartTaskDto("cmd 2"));

            bee.CheckStartTask("cmd 2");
            _db.CheckCreateTask("Task 2", "Colony 1", "Bee 1", "cmd 2", task2Id);

            bee.SetupTaskState(
                TaskDto(task1Id, TaskStatus.Running, 0.5),
                TaskDto(task2Id, TaskStatus.Running, 0.2));

            Refresh();
            _db.CheckTaskUpdate("Task 1", "Colony 1", "Bee 1", "cmd 1", task1Id, TaskStatus.Running, 0.5);
            _db.CheckTaskUpdate("Task 2", "Colony 1", "Bee 1", "cmd 2", task2Id, TaskStatus.Running, 0.2);

            bee.SetupTaskState(
                TaskDto(task1Id, TaskStatus.Done, 1.0),
                TaskDto(task2Id, TaskStatus.Running, 0.8));

            Refresh();
            _db.CheckTaskUpdate("Task 1", "Colony 1", "Bee 1", "cmd 1", task1Id, TaskStatus.Done, 1.0);
            _db.CheckTaskUpdate("Task 2", "Colony 1", "Bee 1", "cmd 2", task2Id, TaskStatus.Running, 0.8);

            bee.SetupTaskState(TaskDto(task2Id, TaskStatus.Done, 1.0));

            Refresh();
            _db.CheckTaskUpdate("Task 2", "Colony 1", "Bee 1", "cmd 2", task2Id, TaskStatus.Done, 1.0);

            Refresh();
            _db.TaskTable.EmptyLogs();
        }

        [Fact]
        public void StartAndRunMultipleTasksOnMultipleBees()
        {
            var bee1 = _factory.Setup("Bee 1");
            var bee2 = _factory.Setup("Bee 2");
            _beeKeeper.EnrollBee("Bee 1");
            _beeKeeper.EnrollBee("Bee 2");

            Refresh();

            var task1Id = Guid.NewGuid();
            var task2Id = Guid.NewGuid();

            bee2.SetupAsFull();
            bee1.SetupStartTask("cmd 1", task1Id);
            bee2.SetupStartTask("cmd 2", task2Id);

            _colony.StartTask("Task 1", StartTaskDto("cmd 1"));

            bee1.CheckStartTask("cmd 1");
            _db.CheckCreateTask("Task 1", "Colony 1", "Bee 1", "cmd 1", task1Id);

            bee1.SetupTaskState(TaskDto(task1Id, TaskStatus.Running, 0.1));

            Refresh();
            _db.CheckTaskUpdate("Task 1", "Colony 1", "Bee 1", "cmd 1", task1Id, TaskStatus.Running, 0.1);

            bee1.SetupAsFull();
            bee2.SetupAsEmpty();
            Refresh();
            _db.CheckTaskUpdate("Task 1", "Colony 1", "Bee 1", "cmd 1", task1Id, TaskStatus.Running, 0.1);

            _colony.StartTask("Task 2", StartTaskDto("cmd 2"));

            bee2.CheckStartTask("cmd 2");
            _db.CheckCreateTask("Task 2", "Colony 1", "Bee 2", "cmd 2", task2Id);

            bee1.SetupTaskState(TaskDto(task1Id, TaskStatus.Running, 0.5));
            bee2.SetupTaskState(TaskDto(task2Id, TaskStatus.Running, 0.2));

            Refresh();
            _db.CheckTaskUpdate("Task 1", "Colony 1", "Bee 1", "cmd 1", task1Id, TaskStatus.Running, 0.5);
            _db.CheckTaskUpdate("Task 2", "Colony 1", "Bee 2", "cmd 2", task2Id, TaskStatus.Running, 0.2);

            bee1.SetupTaskState(TaskDto(task1Id, TaskStatus.Done, 1.0));
            bee2.SetupTaskState(TaskDto(task2Id, TaskStatus.Running, 0.8));

            Refresh();
            _db.CheckTaskUpdate("Task 1", "Colony 1", "Bee 1", "cmd 1", task1Id, TaskStatus.Done, 1.0);
            _db.CheckTaskUpdate("Task 2", "Colony 1", "Bee 2", "cmd 2", task2Id, TaskStatus.Running, 0.8);

            bee2.SetupTaskState(TaskDto(task2Id, TaskStatus.Done, 1.0));

            Refresh();
            _db.CheckTaskUpdate("Task 2", "Colony 1", "Bee 2", "cmd 2", task2Id, TaskStatus.Done, 1.0);

            bee1.ClearReceivedCalls();
            bee2.ClearReceivedCalls();

            Refresh();
            _db.TaskTable.EmptyLogs();
        }

        [Fact]
        public void StartButHangTaskBecauseNoAvailableBee()
        {
            var bee = _factory.Setup("Bee 1");
            _beeKeeper.EnrollBee("Bee 1");
            Refresh();

            var taskId = Guid.NewGuid();
            bee.SetupStartTask("cmd 1", taskId);

            bee.SetupAsFull();
            Refresh();

            _colony.StartTask("Task 1", StartTaskDto("cmd 1"));

            bee.DidNotReceive().StartTask(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());
            _db.CheckCreateTask("Task 1", "Colony 1", null, "cmd 1", Guid.Empty, RemoteTaskStatus.Pending);

            Refresh();
            bee.DidNotReceive().StartTask(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());
            _db.CheckTaskUpdate("Task 1", "Colony 1", null, "cmd 1", Guid.Empty, RemoteTaskStatus.Pending);

            bee.SetupAsEmpty();

            Refresh();
            bee.CheckStartTask("cmd 1");
            _db.CheckUpdateTask("Task 1", "Colony 1", "Bee 1", "cmd 1", taskId, RemoteTaskStatus.Running);

            bee.SetupTaskState(TaskDto(taskId, TaskStatus.Running, 0.1));

            Refresh();
            _db.CheckTaskUpdate("Task 1", "Colony 1", "Bee 1", "cmd 1", taskId, TaskStatus.Running, 0.1);

            bee.SetupTaskState(TaskDto(taskId, TaskStatus.Running, 0.5));

            Refresh();
            _db.CheckTaskUpdate("Task 1", "Colony 1", "Bee 1", "cmd 1", taskId, TaskStatus.Running, 0.5);

            bee.SetupTaskState(TaskDto(taskId, TaskStatus.Done, 1.0));

            Refresh();
            _db.CheckTaskUpdate("Task 1", "Colony 1", "Bee 1", "cmd 1", taskId, TaskStatus.Done, 1.0);

            bee.ClearReceivedCalls();

            _colony.Refresh();
            _db.TaskTable.EmptyLogs();
        }

        [Fact]
        public void StartButHangTaskAndCancelWhenHanging()
        {
            var bee = _factory.Setup("Bee 1");
            _beeKeeper.EnrollBee("Bee 1");
            Refresh();

            bee.SetupStartTask("cmd 1", Guid.NewGuid());

            bee.SetupAsFull();
            Refresh();

            var taskId = _colony.StartTask("Task 1", StartTaskDto("cmd 1"));

            bee.DidNotReceive().StartTask(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());
            _db.CheckCreateTask("Task 1", "Colony 1", null, "cmd 1", Guid.Empty, RemoteTaskStatus.Pending);

            Refresh();
            bee.DidNotReceive().StartTask(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());
            _db.CheckTaskUpdate("Task 1", "Colony 1", null, "cmd 1", Guid.Empty, RemoteTaskStatus.Pending);

            _colony.CancelTask(taskId);

            Refresh();
            bee.DidNotReceive().StartTask(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());
            _db.CheckTaskUpdate("Task 1", "Colony 1", null, "cmd 1", Guid.Empty, RemoteTaskStatus.Cancel);

            bee.ClearReceivedCalls();

            _colony.Refresh();
            _db.TaskTable.EmptyLogs();
        }


        [Fact]
        public void StartBeehiveWithDifferentTaskState()
        {
            var bee = _factory.Setup("Bee 1");

            // Create a Bee
            _db.CreateBee(new BeeDto { Address = "Bee 1" });

            var task1Id = Guid.NewGuid();
            var task2Id = Guid.NewGuid();
            var task3Id = Guid.NewGuid();
            var task5Id = Guid.NewGuid();
            var task7Id = Guid.NewGuid();
            var task9Id = Guid.NewGuid();
            var task11Id = Guid.NewGuid();
            var task12Id = Guid.NewGuid();
            var task14Id = Guid.NewGuid();

            // Not in the queue task
            _db.CreateTask(RemoteTaskDto("Task 1", "Colony 1", "Bee 1", RemoteTaskStatus.Running, StartTaskDto("cmd 1"), TaskDto(task1Id, TaskStatus.Running)));
            // Pending task
            _db.CreateTask(RemoteTaskDto("Task 2", "Colony 2", "Bee 1", RemoteTaskStatus.Pending, StartTaskDto("cmd 2"), TaskDto(Guid.Empty, TaskStatus.Pending)));
            // Running task
            _db.CreateTask(RemoteTaskDto("Task 3", "Colony 2", "Bee 1", RemoteTaskStatus.Running, StartTaskDto("cmd 3"), TaskDto(task3Id, TaskStatus.Running)));
            // Running task no Bee feedback yet
            _db.CreateTask(RemoteTaskDto("Task 4", "Colony 2", "Bee 1", RemoteTaskStatus.Running, StartTaskDto("cmd 4"), null));
            // Completed task
            _db.CreateTask(RemoteTaskDto("Task 5", "Colony 2", "Bee 1", RemoteTaskStatus.Completed, StartTaskDto("cmd 5"), TaskDto(task5Id, TaskStatus.Done, 1.0)));
            // Completed task no Bee feedback
            _db.CreateTask(RemoteTaskDto("Task 6", "Colony 2", "Bee 1", RemoteTaskStatus.Completed, StartTaskDto("cmd 6"), null));
            // Error task
            _db.CreateTask(RemoteTaskDto("Task 7", "Colony 2", "Bee 1", RemoteTaskStatus.Error, StartTaskDto("cmd 7"), TaskDto(task7Id, TaskStatus.Running, 0.5)));
            // Error task no Bee feedback
            _db.CreateTask(RemoteTaskDto("Task 8", "Colony 2", "Bee 1", RemoteTaskStatus.Error, StartTaskDto("cmd 8"), null));
            // Cancel requested task
            _db.CreateTask(RemoteTaskDto("Task 9", "Colony 2", "Bee 1", RemoteTaskStatus.CancelRequested, StartTaskDto("cmd 9"), TaskDto(task9Id, TaskStatus.Running, 0.5)));
            // Cancel requested task no Bee feedback
            _db.CreateTask(RemoteTaskDto("Task 10", "Colony 2", "Bee 1",  RemoteTaskStatus.CancelRequested, StartTaskDto("cmd 10"), null));
            // Running task but ended during beehive stop
            _db.CreateTask(RemoteTaskDto("Task 11", "Colony 2", "Bee 1",  RemoteTaskStatus.Running, StartTaskDto("cmd 11"), TaskDto(task11Id, TaskStatus.Running)));
            // Cancel pending task should be cancel by Bee
            _db.CreateTask(RemoteTaskDto("Task 12", "Colony 2", "Bee 1",  RemoteTaskStatus.CancelPending, StartTaskDto("cmd 12"), TaskDto(task12Id, TaskStatus.Running, 0.5)));
            // Cancel pending task no Bee feedback
            _db.CreateTask(RemoteTaskDto("Task 13", "Colony 2", "Bee 1",  RemoteTaskStatus.CancelPending, StartTaskDto("cmd 13"), null));
            // Cancel requested task not canceled yet on Bee side
            _db.CreateTask(RemoteTaskDto("Task 14", "Colony 2", "Bee 1", RemoteTaskStatus.CancelRequested, StartTaskDto("cmd 14"), TaskDto(task14Id, TaskStatus.Running, 0.5)));

            _db.ClearLogs();

            bee.SetupStartTask("cmd 2", task2Id);
            bee.SetupTaskState(
                TaskDto(task3Id, TaskStatus.Running),
                TaskDto(task7Id, TaskStatus.Running),
                TaskDto(task9Id, TaskStatus.Cancel),
                TaskDto(task14Id, TaskStatus.Running),
                TaskDto(task12Id, TaskStatus.Running),
                TaskDto(task11Id, TaskStatus.Done));

            var beehive = new Application.Beehive.BeeKeeper(_factory, _db);
            var queue = new Colony(ColonyDto("Queue 2"), beehive, _db, _tracker);

            bee.DidNotReceive().StartTask(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());
            bee.DidNotReceive().CancelTask(Arg.Any<Guid>());
            bee.DidNotReceive().DeleteTask(Arg.Any<Guid>());
            _db.BeeTable.EmptyLogs();
            _db.TaskTable.EmptyLogs();

            beehive.Refresh();
            queue.Refresh();

            var taskUpdates = new List<RemoteTaskDto>();
            RemoteTaskDto dto;
            while ((dto = _db.TaskTable.NextUpdate()) != null)
                taskUpdates.Add(dto);

            // Check task 1
            bee.DidNotReceive().StartTask(Arg.Is("cmd 1"), Arg.Any<string>(), Arg.Any<int>());
            bee.DidNotReceive().CancelTask(task1Id);
            bee.DidNotReceive().DeleteTask(task1Id);

            // Check task 2
            bee.CheckStartTask("cmd 2");
            taskUpdates.Check("Task 2", "Colony 2", "Bee 1", "cmd 2", task2Id, RemoteTaskStatus.Running);

            // Check task 3
            taskUpdates.Check("Task 3", "Colony 2", "Bee 1", "cmd 3", task3Id, RemoteTaskStatus.Running, TaskStatus.Running, 0.5);

            // Check task 4
            taskUpdates.Check("Task 4", "Colony 2", "Bee 1", "cmd 4", Guid.Empty, RemoteTaskStatus.Error);

            // Nothing to check for task 5
            // Nothing to check for task 6

            // Nothing to check for task 7
            // Nothing to check for task 8

            // Check task 9
            taskUpdates.Check("Task 9", "Colony 2", "Bee 1", "cmd 9", task9Id, RemoteTaskStatus.Cancel, TaskStatus.Cancel, 0.5);
            bee.Received(1).DeleteTask(Arg.Is(task9Id));

            // Check task 10
            taskUpdates.Check("Task 10", "Colony 2", "Bee 1", "cmd 10", Guid.Empty, RemoteTaskStatus.Error);

            // Check task 11
            taskUpdates.Check("Task 11", "Colony 2", "Bee 1", "cmd 11", task11Id, RemoteTaskStatus.Completed, TaskStatus.Done, 0.5);
            bee.Received(1).DeleteTask(Arg.Is(task11Id));

            // Check task 12
            taskUpdates.Check("Task 12", "Colony 2", "Bee 1", "cmd 12", task12Id, RemoteTaskStatus.CancelRequested, TaskStatus.Running, 0.5);
            bee.Received(1).CancelTask(Arg.Is(task12Id));

            // Check task 13
            taskUpdates.Check("Task 13", "Colony 2", "Bee 1", "cmd 13", Guid.Empty, RemoteTaskStatus.Error);

            // Check task 14
            taskUpdates.Check("Task 14", "Colony 2", "Bee 1", "cmd 14", task14Id, RemoteTaskStatus.CancelRequested, TaskStatus.Running, 0.5);
        }

        private static ColonyDto ColonyDto(string name) => new ColonyDto { Name = name };
        private static TaskParameters StartTaskDto(string command, string arguments = null, int nbCores = 1) 
            => new TaskParameters { Command = command, Arguments = arguments, NbCores = nbCores };
        private static TaskDto TaskDto(Guid id, TaskStatus status, double progress = 0.5)
            => QueueTestExtensions.TaskDto(id, status, progress);
        private static ulong taskCounter = 0;
        private static RemoteTaskDto RemoteTaskDto(string name, string queueName, string bee, RemoteTaskStatus status, TaskParameters start, TaskDto state)
            => new RemoteTaskDto
            {
                Id = Guid.NewGuid(),
                Name = name,
                BeeAddress = bee,
                Colony = queueName,
                BeeState = state,
                Parameters = start,
                Status = status,
                Order = ++taskCounter,
            };
    }

    public static class QueueTestExtensions
    {
        public static IBee Setup(this IBeeFactory factory, string name)
        {
            var bee = Substitute.For<IBee>();
            bee.SetupAsEmpty();
            factory.Create(Arg.Is(name)).Returns(bee);
            return bee;
        }

        public static void SetupStartTask(this IBee bee, string command, Guid taskId)
            => bee.StartTask(Arg.Is(command), Arg.Any<string>(), Arg.Any<int>()).Returns(taskId);

        public static void SetupTaskState(this IBee bee, params TaskDto[] tasks) 
            => bee.GetTasks().Returns(tasks);

        public static void SetupAsFull(this IBee bee) => bee.GetResources().Returns(ResourcesDto(0));
        public static void SetupAsEmpty(this IBee bee) => bee.GetResources().Returns(ResourcesDto(8));

        public static bool Check(
            this List<RemoteTaskDto> list,
            string name, string colony, string beeAddress, string command,
            Guid taskId, RemoteTaskStatus status,
            TaskStatus? taskStatus = null, double? progress = null)
        {
            var update = list.FirstOrDefault(p => p.Name == name);
            update.Check(name, colony, beeAddress, command, taskId, status, taskStatus, progress);
            list.Remove(update);
            return true;
        }

        public static bool Check(
            this RemoteTaskDto expected, 
            string name, string colony, string beeAdress, string command,
            Guid taskId, RemoteTaskStatus status, 
            TaskStatus? taskStatus = null, double? progress = null)
        {
            var clone = expected.DeepCopy();
            clone.Name = name;
            clone.Colony = colony;
            clone.BeeAddress = beeAdress;
            clone.Parameters.Command = command;
            clone.Status = status;

            if (clone.BeeState != null)
            {
                clone.BeeState.Id = taskId;
                if (taskStatus.HasValue)
                    clone.BeeState.Status = taskStatus.Value;
                if (progress.HasValue)
                    clone.BeeState.ProgressPercent = progress.Value;
            }

            expected.Check(clone);
            return true;
        }

        public static void Check(this RemoteTaskDto expected, RemoteTaskDto actual)
        {
            if (expected == null && actual == null) return;

            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Colony, actual.Colony);
            Assert.Equal(expected.BeeAddress, actual.BeeAddress);
            Assert.Equal(expected.Status, actual.Status);
            expected.Parameters.Check(actual.Parameters);
            Assert.Equal(expected.Order, actual.Order);
            expected.BeeState.Check(actual.BeeState);
        }

        public static void Check(this TaskParameters expected, TaskParameters actual)
        {
            if (expected == null && actual == null) return;

            Assert.Equal(expected.Command, actual.Command);
            Assert.Equal(expected.Arguments, actual.Arguments);
            Assert.Equal(expected.NbCores, actual.NbCores);
        }

        public static void Check(this TaskDto expected, TaskDto actual)
        {
            if (expected == null && actual == null) return;

            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.Status, actual.Status);
            Assert.Equal(expected.StartTime, actual.StartTime);
            Assert.Equal(expected.EndTime, actual.EndTime);
            Assert.Equal(expected.ProgressPercent, actual.ProgressPercent);
            Assert.Equal(expected.ExpectedEndTime, actual.ExpectedEndTime);
            Assert.Equal(expected.Message, actual.Message);
        }

        public static void CheckStartTask(this IBee bee, string command) 
            => bee.Received(1).StartTask(Arg.Is(command), Arg.Any<string>(), Arg.Any<int>());

        public static void CheckDeleteTask(this IBee bee, Guid id) 
            => bee.Received().DeleteTask(Arg.Is(id));

        public static void CheckCreateTask(this BeehiveDbLogs db, string name, string queue, string bee, string command, Guid taskId, RemoteTaskStatus status = RemoteTaskStatus.Running)
        {
            db.TaskTable.NextCreate().Check(name, queue, bee, command, taskId, status);
            db.TaskTable.EmptyLogs();
        }

        public static void CheckUpdateTask(this BeehiveDbLogs db, string name, string queue, string bee, string command, Guid taskId, RemoteTaskStatus status = RemoteTaskStatus.Running)
        {
            db.TaskTable.NextUpdate().Check(name, queue, bee, command, taskId, status);
            db.TaskTable.EmptyLogs();
        }

        public static TaskDto TaskDto(Guid id, TaskStatus status, double progress = 0.5)
            => new TaskDto { Id = id, Status = status, ProgressPercent = progress };

        public static void CheckTaskUpdate(this BeehiveDbLogs db, string name, string queue, string bee, string command, Guid taskId, TaskStatus status, double progress)
        {
            var qStatus = status.IsFinal() ? RemoteTaskStatus.Completed : RemoteTaskStatus.Running;

            db.TaskTable.NextUpdate().Check(name, queue, bee, command, taskId, qStatus, status, progress);
        }

        public static void CheckTaskUpdate(this BeehiveDbLogs db, string name, string queue, string bee, string command, Guid taskId, RemoteTaskStatus status)
        {
            db.TaskTable.NextUpdate().Check(name, queue, bee, command, taskId, status);
        }

        private static BeeResourcesDto ResourcesDto(int nbFreeCores)
        {
            return new BeeResourcesDto
            {
                NbCores = 8,
                NbFreeCores = nbFreeCores
            };
        }
    }
}
