using Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Dojo
{
    public class Ninja : INinja
    {
        private readonly INinja _proxy;
        private Dictionary<Guid, TaskDto> _tasks = new Dictionary<Guid, TaskDto>();
        private NinjaResourcesDto _resources;

        public string Address => Dto.Address;

        public NinjaDto Dto { get; } = new NinjaDto();

        public Ninja(string address, INinja ninja)
        {
            _proxy = ninja;
            Dto.Address = address;
        }

        public TaskDto GetTaskState(Guid id)
        {
            return _tasks.TryGetValue(id, out var job) ? job : null;
        }

        public void Refresh()
        {
            _resources = _proxy.GetResources();
            if (_resources == null)
            {
                Dto.IsUp = false;
                return;
            }

            Dto.IsUp = true;
            Dto.OS = _resources.OSPlatform;
            Dto.PercentFreeCores = Percent((ulong)_resources.NbFreeCores, (ulong)_resources.NbCores);
            Dto.PercentFreeMemory = Percent(_resources.AvailablePhysicalMemory, _resources.TotalPhysicalMemory);
            Dto.PercentFreeDiskSpace = Percent(_resources.DiskFreeSpace, _resources.DiskSpace);

            var tasks = _proxy.GetTasks();
            _tasks = tasks?.ToDictionary(p => p.Id, p => p) ?? new Dictionary<Guid, TaskDto>();
        }

        private static double Percent(ulong free, ulong total) => Math.Round((1.0 - (double)free / total) * 100.0, 2);

        public IEnumerable<TaskDto> GetTasks() => _tasks.Values;

        public IEnumerable<TaskMessageDto> FetchMessages(Guid id, int start, int length) => _proxy.FetchMessages(id, start, length);

        public Guid StartTask(string command, string arguments, int nbCores = 1) => _proxy.StartTask(command, arguments, nbCores);

        public void CancelTask(Guid id) => _proxy.CancelTask(id);

        public void DeleteTask(Guid id) => _proxy.DeleteTask(id);

        public NinjaResourcesDto GetResources() => _resources;

        public void UpdateTask(Guid taskId, double progressPercent, DateTime expectedEndTime, string message) => 
            _proxy.UpdateTask(taskId, progressPercent, expectedEndTime, message);
    }
}
