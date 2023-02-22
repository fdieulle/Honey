using Domain.Dtos;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Colony
{
    public class Bee : IBee
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IBee _proxy;
        private Dictionary<Guid, TaskDto> _tasks = new Dictionary<Guid, TaskDto>();
        private BeeResourcesDto _resources;

        public string Address => Dto.Address;

        public BeeDto Dto { get; } = new BeeDto();

        public int NbFreeCores => _resources?.NbFreeCores ?? 0;

        public Bee(string address, IBee bee)
        {
            _proxy = bee;
            Dto.Address = address;
        }

        public TaskDto GetTaskState(Guid id) => _tasks.TryGetValue(id, out var job) ? job : null;

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

        public async Task<List<string>> FetchLogsAsync(Guid id, int start = 0, int length = -1) 
            => await _proxy.FetchLogsAsync(id, start, length);

        public Guid StartTask(string command, string arguments, int nbCores = 1)
        {
            Logger.InfoFormat("[{0}] Starting task. Command: {1}, Arguments: {2}, NbCores: {3}", Address, command, arguments, nbCores);

            // Provision untill the real state is asked
            var previousFreeCores = 0;
            if (_resources != null)
            {
                Logger.InfoFormat("[{0}] NbFreeCores: {1} (before)", Address, _resources.NbFreeCores);

                if (nbCores > 0)
                    _resources.NbFreeCores -= nbCores;
                else
                {
                    previousFreeCores = _resources.NbFreeCores;
                    _resources.NbFreeCores = 0;
                }

                Logger.InfoFormat("[{0}] NbFreeCores: {1} (predict)", Address, _resources.NbFreeCores);
            }

            var id = _proxy.StartTask(command, arguments, nbCores);
            
            // Rollback the provision
            if (id == Guid.Empty && _resources != null)
            {
                if (nbCores > 0)
                    _resources.NbFreeCores += nbCores;
                else _resources.NbFreeCores = previousFreeCores;

                Logger.InfoFormat("[{0}] NbFreeCores: {1} (rollback)", Address, _resources.NbFreeCores);
            }

            return id;
        }

        public void CancelTask(Guid id)
        {
            Logger.InfoFormat("[{0}] Cancel task with Id: {1}", Address, id);
            _proxy.CancelTask(id);
        }

        public void DeleteTask(Guid id) 
        {
            Logger.InfoFormat("[{0}] Delete task with Id: {1}", Address, id);
            _proxy.DeleteTask(id); 
        }

        public BeeResourcesDto GetResources() => _resources;

        public override string ToString() => Dto.ToString();
    }
}
