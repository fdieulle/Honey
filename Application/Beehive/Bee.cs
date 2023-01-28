using Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Beehive
{
    public class Bee : IBee
    {
        private readonly IBee _proxy;
        private Dictionary<Guid, TaskDto> _tasks = new Dictionary<Guid, TaskDto>();
        private BeeResourcesDto _resources;

        public string Address => Dto.Address;

        public BeeDto Dto { get; } = new BeeDto();

        public Bee(string address, IBee bee)
        {
            _proxy = bee;
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

        private static double Percent(ulong free, ulong total) => Math.Round((double)free / total * 100.0, 2);

        public IEnumerable<TaskDto> GetTasks() => _tasks.Values;

        public IEnumerable<TaskMessageDto> FetchMessages(Guid id, int start, int length) => _proxy.FetchMessages(id, start, length);

        public Guid StartTask(string command, string arguments, int nbCores = 1)
        {
            // Provision untill the real state is asked
            var previousFreeCores = 0;
            if (_resources != null)
            {
                if (nbCores > 0)
                    _resources.NbFreeCores -= nbCores;
                else
                {
                    previousFreeCores = _resources.NbFreeCores;
                    _resources.NbFreeCores = 0;
                }
            }

            var id = _proxy.StartTask(command, arguments, nbCores);
            
            // Rollback the provision
            if (id == Guid.Empty && _resources != null)
            {
                if (nbCores > 0)
                    _resources.NbFreeCores += nbCores;
                else _resources.NbFreeCores = previousFreeCores;
            }

            return id;
        }

        public void CancelTask(Guid id) => _proxy.CancelTask(id);

        public void DeleteTask(Guid id) => _proxy.DeleteTask(id);

        public BeeResourcesDto GetResources() => _resources;
    }
}
