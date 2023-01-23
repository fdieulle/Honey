using Application.Dojo;
using Application.Ninja;
using Domain;
using Domain.Dtos;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Application.Tests.Dojo
{
    public class QueueTests
    {
        private readonly INinjaFactory _factory;
        private readonly DojoDbLogs _db;
        private readonly TaskTracker _tracker;
        private readonly Application.Dojo.Dojo _dojo;
        private readonly Queue _queue;

        public QueueTests()
        {
            _factory = Substitute.For<INinjaFactory>();
            _db = new DojoDbLogs();
            _dojo = new Application.Dojo.Dojo(_factory, _db);
            _tracker = new TaskTracker();

            _queue = new Queue(QueueDto("Queue 1"), _dojo, _db, _tracker);
        }

        private void Refresh()
        {
            _dojo.Refresh();
            _queue.Refresh();
            _tracker.Refresh();
        }

        [Fact]
        public void StartAndRunTask()
        {
            var ninja = _factory.Setup("Ninja 1");
            _dojo.EnrollNinja("Ninja 1");

            Refresh();

            var taskId = Guid.NewGuid();
            ninja.StartTask(Arg.Is("App 1"), Arg.Is("Arg1, Arg2"), Arg.Is(1)).Returns(taskId);

            _queue.StartTask("Task 1", StartTaskDto("App 1", "Arg1, Arg2", 1));

            ninja.Received(1).StartTask(Arg.Is("App 1"), Arg.Is("Arg1, Arg2"), Arg.Is(1));
            _db.TaskTable.NextCreate().Check("Task 1", "Queue 1", "Ninja 1", "App 1", taskId, RemoteTaskStatus.Running);
            _db.TaskTable.EmptyLogs();

            ninja.GetTasks().Returns(new[] { TaskDto(taskId, TaskStatus.Running, 0.1) });

            Refresh();
            _db.TaskTable.NextUpdate().Check("Task 1", "Queue 1", "Ninja 1", "App 1", taskId, RemoteTaskStatus.Running, TaskStatus.Running, 0.1);
            _db.TaskTable.EmptyLogs();

            ninja.GetTasks().Returns(new[] { TaskDto(taskId, TaskStatus.Running, 0.5) });

            Refresh();
            _db.TaskTable.NextUpdate().Check("Task 1", "Queue 1", "Ninja 1", "App 1", taskId, RemoteTaskStatus.Running, TaskStatus.Running, 0.5);
            _db.TaskTable.EmptyLogs();

            ninja.GetTasks().Returns(new[] { TaskDto(taskId, TaskStatus.Done, 1.0) });

            Refresh();
            _db.TaskTable.NextUpdate().Check("Task 1", "Queue 1", "Ninja 1", "App 1", taskId, RemoteTaskStatus.Completed, TaskStatus.Done, 1.0);
            _db.TaskTable.EmptyLogs();

            Refresh();
            _db.TaskTable.EmptyLogs();
        }

        [Fact]
        public void StartAndRunMultipleTasksOn1Ninja()
        {
            var ninja = _factory.Setup("Ninja 1");
            _dojo.EnrollNinja("Ninja 1");

            Refresh();

            var task1Id = Guid.NewGuid();
            var task2Id = Guid.NewGuid();

            ninja.SetupStartTask("cmd 1", task1Id);
            ninja.SetupStartTask("cmd 2", task2Id);

            _queue.StartTask("Task 1", StartTaskDto("cmd 1"));

            ninja.CheckStartTask("cmd 1");
            _db.CheckCreateTask("Task 1", "Queue 1", "Ninja 1", "cmd 1", task1Id);

            ninja.SetupTaskState(TaskDto(task1Id, TaskStatus.Running, 0.1));

            Refresh();
            _db.CheckTaskUpdate("Task 1", "Queue 1", "Ninja 1", "cmd 1", task1Id, TaskStatus.Running, 0.1);

            _queue.StartTask("Task 2", StartTaskDto("cmd 2"));

            ninja.CheckStartTask("cmd 2");
            _db.CheckCreateTask("Task 2", "Queue 1", "Ninja 1", "cmd 2", task2Id);

            ninja.SetupTaskState(
                TaskDto(task1Id, TaskStatus.Running, 0.5),
                TaskDto(task2Id, TaskStatus.Running, 0.2));

            Refresh();
            _db.CheckTaskUpdate("Task 1", "Queue 1", "Ninja 1", "cmd 1", task1Id, TaskStatus.Running, 0.5);
            _db.CheckTaskUpdate("Task 2", "Queue 1", "Ninja 1", "cmd 2", task2Id, TaskStatus.Running, 0.2);

            ninja.SetupTaskState(
                TaskDto(task1Id, TaskStatus.Done, 1.0),
                TaskDto(task2Id, TaskStatus.Running, 0.8));

            Refresh();
            _db.CheckTaskUpdate("Task 1", "Queue 1", "Ninja 1", "cmd 1", task1Id, TaskStatus.Done, 1.0);
            _db.CheckTaskUpdate("Task 2", "Queue 1", "Ninja 1", "cmd 2", task2Id, TaskStatus.Running, 0.8);

            ninja.SetupTaskState(TaskDto(task2Id, TaskStatus.Done, 1.0));

            Refresh();
            _db.CheckTaskUpdate("Task 2", "Queue 1", "Ninja 1", "cmd 2", task2Id, TaskStatus.Done, 1.0);

            Refresh();
            _db.TaskTable.EmptyLogs();
        }

        [Fact]
        public void StartAndRunMultipleTasksOnMultipleNinjas()
        {
            var ninja1 = _factory.Setup("Ninja 1");
            var ninja2 = _factory.Setup("Ninja 2");
            _dojo.EnrollNinja("Ninja 1");
            _dojo.EnrollNinja("Ninja 2");

            Refresh();

            var task1Id = Guid.NewGuid();
            var task2Id = Guid.NewGuid();

            ninja2.SetupAsFull();
            ninja1.SetupStartTask("cmd 1", task1Id);
            ninja2.SetupStartTask("cmd 2", task2Id);

            _queue.StartTask("Task 1", StartTaskDto("cmd 1"));

            ninja1.CheckStartTask("cmd 1");
            _db.CheckCreateTask("Task 1", "Queue 1", "Ninja 1", "cmd 1", task1Id);

            ninja1.SetupTaskState(TaskDto(task1Id, TaskStatus.Running, 0.1));

            Refresh();
            _db.CheckTaskUpdate("Task 1", "Queue 1", "Ninja 1", "cmd 1", task1Id, TaskStatus.Running, 0.1);

            ninja1.SetupAsFull();
            ninja2.SetupAsEmpty();
            Refresh();
            _db.CheckTaskUpdate("Task 1", "Queue 1", "Ninja 1", "cmd 1", task1Id, TaskStatus.Running, 0.1);

            _queue.StartTask("Task 2", StartTaskDto("cmd 2"));

            ninja2.CheckStartTask("cmd 2");
            _db.CheckCreateTask("Task 2", "Queue 1", "Ninja 2", "cmd 2", task2Id);

            ninja1.SetupTaskState(TaskDto(task1Id, TaskStatus.Running, 0.5));
            ninja2.SetupTaskState(TaskDto(task2Id, TaskStatus.Running, 0.2));

            Refresh();
            _db.CheckTaskUpdate("Task 1", "Queue 1", "Ninja 1", "cmd 1", task1Id, TaskStatus.Running, 0.5);
            _db.CheckTaskUpdate("Task 2", "Queue 1", "Ninja 2", "cmd 2", task2Id, TaskStatus.Running, 0.2);

            ninja1.SetupTaskState(TaskDto(task1Id, TaskStatus.Done, 1.0));
            ninja2.SetupTaskState(TaskDto(task2Id, TaskStatus.Running, 0.8));

            Refresh();
            _db.CheckTaskUpdate("Task 1", "Queue 1", "Ninja 1", "cmd 1", task1Id, TaskStatus.Done, 1.0);
            _db.CheckTaskUpdate("Task 2", "Queue 1", "Ninja 2", "cmd 2", task2Id, TaskStatus.Running, 0.8);

            ninja2.SetupTaskState(TaskDto(task2Id, TaskStatus.Done, 1.0));

            Refresh();
            _db.CheckTaskUpdate("Task 2", "Queue 1", "Ninja 2", "cmd 2", task2Id, TaskStatus.Done, 1.0);

            ninja1.ClearReceivedCalls();
            ninja2.ClearReceivedCalls();

            Refresh();
            _db.TaskTable.EmptyLogs();
        }

        [Fact]
        public void StartButHangTaskBecauseNoAvailableNinja()
        {
            var ninja = _factory.Setup("Ninja 1");
            _dojo.EnrollNinja("Ninja 1");
            Refresh();

            var taskId = Guid.NewGuid();
            ninja.SetupStartTask("cmd 1", taskId);

            ninja.SetupAsFull();
            Refresh();

            _queue.StartTask("Task 1", StartTaskDto("cmd 1"));

            ninja.DidNotReceive().StartTask(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());
            _db.CheckCreateTask("Task 1", "Queue 1", null, "cmd 1", Guid.Empty, RemoteTaskStatus.Pending);

            Refresh();
            ninja.DidNotReceive().StartTask(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());
            _db.CheckTaskUpdate("Task 1", "Queue 1", null, "cmd 1", Guid.Empty, RemoteTaskStatus.Pending);

            ninja.SetupAsEmpty();

            Refresh();
            ninja.CheckStartTask("cmd 1");
            _db.CheckUpdateTask("Task 1", "Queue 1", "Ninja 1", "cmd 1", taskId, RemoteTaskStatus.Running);

            ninja.SetupTaskState(TaskDto(taskId, TaskStatus.Running, 0.1));

            Refresh();
            _db.CheckTaskUpdate("Task 1", "Queue 1", "Ninja 1", "cmd 1", taskId, TaskStatus.Running, 0.1);

            ninja.SetupTaskState(TaskDto(taskId, TaskStatus.Running, 0.5));

            Refresh();
            _db.CheckTaskUpdate("Task 1", "Queue 1", "Ninja 1", "cmd 1", taskId, TaskStatus.Running, 0.5);

            ninja.SetupTaskState(TaskDto(taskId, TaskStatus.Done, 1.0));

            Refresh();
            _db.CheckTaskUpdate("Task 1", "Queue 1", "Ninja 1", "cmd 1", taskId, TaskStatus.Done, 1.0);

            ninja.ClearReceivedCalls();

            _queue.Refresh();
            _db.TaskTable.EmptyLogs();
        }

        [Fact]
        public void StartDojoWithDifferentTaskState()
        {
            var ninja = _factory.Setup("Ninja 1");

            // Create a ninja
            _db.CreateNinja(new NinjaDto { Address = "Ninja 1" });

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
            _db.CreateTask(QueuedTaskDto("Task 1", "Queue 1", "Ninja 1", RemoteTaskStatus.Running, StartTaskDto("cmd 1"), TaskDto(task1Id, TaskStatus.Running)));
            // Pending task
            _db.CreateTask(QueuedTaskDto("Task 2", "Queue 2", "Ninja 1", RemoteTaskStatus.Pending, StartTaskDto("cmd 2"), TaskDto(Guid.Empty, TaskStatus.Pending)));
            // Running task
            _db.CreateTask(QueuedTaskDto("Task 3", "Queue 2", "Ninja 1", RemoteTaskStatus.Running, StartTaskDto("cmd 3"), TaskDto(task3Id, TaskStatus.Running)));
            // Running task no ninja feedback yet
            _db.CreateTask(QueuedTaskDto("Task 4", "Queue 2", "Ninja 1", RemoteTaskStatus.Running, StartTaskDto("cmd 4"), null));
            // Completed task
            _db.CreateTask(QueuedTaskDto("Task 5", "Queue 2", "Ninja 1", RemoteTaskStatus.Completed, StartTaskDto("cmd 5"), TaskDto(task5Id, TaskStatus.Done, 1.0)));
            // Completed task no ninja feedback
            _db.CreateTask(QueuedTaskDto("Task 6", "Queue 2", "Ninja 1", RemoteTaskStatus.Completed, StartTaskDto("cmd 6"), null));
            // Error task
            _db.CreateTask(QueuedTaskDto("Task 7", "Queue 2", "Ninja 1", RemoteTaskStatus.Error, StartTaskDto("cmd 7"), TaskDto(task7Id, TaskStatus.Running, 0.5)));
            // Error task no ninja feedback
            _db.CreateTask(QueuedTaskDto("Task 8", "Queue 2", "Ninja 1", RemoteTaskStatus.Error, StartTaskDto("cmd 8"), null));
            // Cancel requested task
            _db.CreateTask(QueuedTaskDto("Task 9", "Queue 2", "Ninja 1", RemoteTaskStatus.CancelRequested, StartTaskDto("cmd 9"), TaskDto(task9Id, TaskStatus.Running, 0.5)));
            // Cancel requested task no ninja feedback
            _db.CreateTask(QueuedTaskDto("Task 10", "Queue 2", "Ninja 1",  RemoteTaskStatus.CancelRequested, StartTaskDto("cmd 10"), null));
            // Running task but ended during dojo stop
            _db.CreateTask(QueuedTaskDto("Task 11", "Queue 2", "Ninja 1",  RemoteTaskStatus.Running, StartTaskDto("cmd 11"), TaskDto(task11Id, TaskStatus.Running)));
            // Cancel pending task
            _db.CreateTask(QueuedTaskDto("Task 12", "Queue 2", "Ninja 1",  RemoteTaskStatus.CancelPending, StartTaskDto("cmd 12"), TaskDto(task12Id, TaskStatus.Running, 0.5)));
            // Cancel pending task no ninja feedback
            _db.CreateTask(QueuedTaskDto("Task 13", "Queue 2", "Ninja 1",  RemoteTaskStatus.CancelPending, StartTaskDto("cmd 13"), null));
            // Cancel requested task not canceled yet on ninja side
            _db.CreateTask(QueuedTaskDto("Task 14", "Queue 2", "Ninja 1", RemoteTaskStatus.CancelRequested, StartTaskDto("cmd 14"), TaskDto(task14Id, TaskStatus.Running, 0.5)));

            _db.ClearLogs();

            ninja.SetupStartTask("cmd 2", task2Id);
            ninja.SetupTaskState(
                TaskDto(task3Id, TaskStatus.Running),
                TaskDto(task7Id, TaskStatus.Running),
                TaskDto(task9Id, TaskStatus.Cancel),
                TaskDto(task14Id, TaskStatus.Running),
                TaskDto(task12Id, TaskStatus.Running),
                TaskDto(task11Id, TaskStatus.Done));

            var dojo = new Application.Dojo.Dojo(_factory, _db);
            var queue = new Queue(QueueDto("Queue 2"), dojo, _db, _tracker);

            ninja.DidNotReceive().StartTask(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());
            ninja.DidNotReceive().CancelTask(Arg.Any<Guid>());
            ninja.DidNotReceive().DeleteTask(Arg.Any<Guid>());
            _db.NinjaTable.EmptyLogs();
            _db.TaskTable.EmptyLogs();

            dojo.Refresh();
            queue.Refresh();

            var taskUpdates = new List<RemoteTaskDto>();
            RemoteTaskDto dto;
            while ((dto = _db.TaskTable.NextUpdate()) != null)
                taskUpdates.Add(dto);

            // Check task 1
            ninja.DidNotReceive().StartTask(Arg.Is("cmd 1"), Arg.Any<string>(), Arg.Any<int>());
            ninja.DidNotReceive().CancelTask(task1Id);
            ninja.DidNotReceive().DeleteTask(task1Id);

            // Check task 2
            ninja.CheckStartTask("cmd 2");
            taskUpdates.Check("Task 2", "Queue 2", "Ninja 1", "cmd 2", task2Id, RemoteTaskStatus.Running);

            // Check task 3
            taskUpdates.Check("Task 3", "Queue 2", "Ninja 1", "cmd 3", task3Id, RemoteTaskStatus.Running, TaskStatus.Running, 0.5);

            // Check task 4
            taskUpdates.Check("Task 4", "Queue 2", "Ninja 1", "cmd 4", Guid.Empty, RemoteTaskStatus.Error);

            // Nothing to check for task 5
            // Nothing to check for task 6

            // Nothing to check for task 7
            // Nothing to check for task 8

            // Check task 9
            taskUpdates.Check("Task 9", "Queue 2", "Ninja 1", "cmd 9", task9Id, RemoteTaskStatus.Cancel, TaskStatus.Cancel, 0.5);
            ninja.Received(1).DeleteTask(Arg.Is(task9Id));

            // Check task 10
            taskUpdates.Check("Task 10", "Queue 2", "Ninja 1", "cmd 10", Guid.Empty, RemoteTaskStatus.Error);

            // Check task 11
            taskUpdates.Check("Task 11", "Queue 2", "Ninja 1", "cmd 11", task11Id, RemoteTaskStatus.Completed, TaskStatus.Done, 0.5);
            ninja.Received(1).DeleteTask(Arg.Is(task11Id));

            // Check task 12
            taskUpdates.Check("Task 12", "Queue 2", "Ninja 1", "cmd 12", task12Id, RemoteTaskStatus.CancelRequested, TaskStatus.Running, 0.5);
            ninja.Received(1).CancelTask(Arg.Is(task12Id));

            // Check task 13
            taskUpdates.Check("Task 13", "Queue 2", "Ninja 1", "cmd 13", Guid.Empty, RemoteTaskStatus.Error);

            // Check task 14
            taskUpdates.Check("Task 14", "Queue 2", "Ninja 1", "cmd 14", task14Id, RemoteTaskStatus.CancelRequested, TaskStatus.Running, 0.5);
        }

        private static QueueDto QueueDto(string name) => new QueueDto { Name = name };
        private static TaskParameters StartTaskDto(string command, string arguments = null, int nbCores = 1) 
            => new TaskParameters { Command = command, Arguments = arguments, NbCores = nbCores };
        private static TaskDto TaskDto(Guid id, TaskStatus status, double progress = 0.5)
            => QueueTestExtensions.TaskDto(id, status, progress);
        private static ulong taskCounter = 0;
        private static RemoteTaskDto QueuedTaskDto(string name, string queueName, string ninja, RemoteTaskStatus status, TaskParameters start, TaskDto state)
            => new RemoteTaskDto
            {
                Id = Guid.NewGuid(),
                Name = name,
                NinjaAddress = ninja,
                QueueName = queueName,
                NinjaState = state,
                Parameters = start,
                Status = status,
                Order = ++taskCounter,
            };
    }

    public static class QueueTestExtensions
    {
        public static INinja Setup(this INinjaFactory factory, string name)
        {
            var ninja = Substitute.For<INinja>();
            ninja.SetupAsEmpty();
            factory.Create(Arg.Is(name)).Returns(ninja);
            return ninja;
        }

        public static void SetupStartTask(this INinja ninja, string command, Guid taskId)
            => ninja.StartTask(Arg.Is(command), Arg.Any<string>(), Arg.Any<int>()).Returns(taskId);

        public static void SetupTaskState(this INinja ninja, params TaskDto[] tasks) 
            => ninja.GetTasks().Returns(tasks);

        public static void SetupAsFull(this INinja ninja) => ninja.GetResources().Returns(ResourcesDto(0));
        public static void SetupAsEmpty(this INinja ninja) => ninja.GetResources().Returns(ResourcesDto(8));

        public static bool Check(
            this List<RemoteTaskDto> list,
            string name, string queueName, string ninjaAdress, string command,
            Guid taskId, RemoteTaskStatus status,
            TaskStatus? taskStatus = null, double? progress = null)
        {
            var update = list.FirstOrDefault(p => p.Name == name);
            update.Check(name, queueName, ninjaAdress, command, taskId, status, taskStatus, progress);
            list.Remove(update);
            return true;
        }

        public static bool Check(
            this RemoteTaskDto expected, 
            string name, string queueName, string ninjaAdress, string command,
            Guid taskId, RemoteTaskStatus status, 
            TaskStatus? taskStatus = null, double? progress = null)
        {
            var clone = expected.DeepCopy();
            clone.Name = name;
            clone.QueueName = queueName;
            clone.NinjaAddress = ninjaAdress;
            clone.Parameters.Command = command;
            clone.Status = status;

            if (clone.NinjaState != null)
            {
                clone.NinjaState.Id = taskId;
                if (taskStatus.HasValue)
                    clone.NinjaState.Status = taskStatus.Value;
                if (progress.HasValue)
                    clone.NinjaState.ProgressPercent = progress.Value;
            }

            expected.Check(clone);
            return true;
        }

        public static void Check(this RemoteTaskDto expected, RemoteTaskDto actual)
        {
            if (expected == null && actual == null) return;

            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.QueueName, actual.QueueName);
            Assert.Equal(expected.NinjaAddress, actual.NinjaAddress);
            Assert.Equal(expected.Status, actual.Status);
            expected.Parameters.Check(actual.Parameters);
            Assert.Equal(expected.Order, actual.Order);
            expected.NinjaState.Check(actual.NinjaState);
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

        public static void CheckStartTask(this INinja ninja, string command) 
            => ninja.Received(1).StartTask(Arg.Is(command), Arg.Any<string>(), Arg.Any<int>());

        public static void CheckDeleteTask(this INinja ninja, Guid id) 
            => ninja.Received().DeleteTask(Arg.Is(id));

        public static void CheckCreateTask(this DojoDbLogs db, string name, string queue, string ninja, string command, Guid taskId, RemoteTaskStatus status = RemoteTaskStatus.Running)
        {
            db.TaskTable.NextCreate().Check(name, queue, ninja, command, taskId, status);
            db.TaskTable.EmptyLogs();
        }

        public static void CheckUpdateTask(this DojoDbLogs db, string name, string queue, string ninja, string command, Guid taskId, RemoteTaskStatus status = RemoteTaskStatus.Running)
        {
            db.TaskTable.NextUpdate().Check(name, queue, ninja, command, taskId, status);
            db.TaskTable.EmptyLogs();
        }

        public static TaskDto TaskDto(Guid id, TaskStatus status, double progress = 0.5)
            => new TaskDto { Id = id, Status = status, ProgressPercent = progress };

        public static void CheckTaskUpdate(this DojoDbLogs db, string name, string queue, string ninja, string command, Guid taskId, TaskStatus status, double progress)
        {
            var qStatus = status.IsFinal() ? RemoteTaskStatus.Completed : RemoteTaskStatus.Running;

            db.TaskTable.NextUpdate().Check(name, queue, ninja, command, taskId, qStatus, status, progress);
        }

        public static void CheckTaskUpdate(this DojoDbLogs db, string name, string queue, string ninja, string command, Guid taskId, RemoteTaskStatus status)
        {
            db.TaskTable.NextUpdate().Check(name, queue, ninja, command, taskId, status);
        }

        private static NinjaResourcesDto ResourcesDto(int nbFreeCores)
        {
            return new NinjaResourcesDto
            {
                NbCores = 8,
                NbFreeCores = nbFreeCores
            };
        }
    }
}
