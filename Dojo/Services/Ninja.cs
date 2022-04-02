using Dojo.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Yumi;
using Yumi.Application;
using Yumi.Domain.Dto;

namespace Dojo.Services
{
    public class Ninja : INinja
    {
        private readonly INinja _proxy;
        private Dictionary<Guid, Job> _jobs = new Dictionary<Guid, Job>();
        private NinjaResources _resources;

        public string Address => Model.Address;

        public NinjaModel Model { get; } = new NinjaModel();

        public NinjaDto Dto { get; } = new NinjaDto();

        public Ninja(string address, INinja ninja)
        {
            _proxy = ninja;
            Model.Address = address;
            Dto.Address = address;
        }

        public Job GetJobStatus(Guid id)
        {
            return _jobs.TryGetValue(id, out var job) ? job : null;
        }

        public void Refresh()
        {
            _resources = _proxy.GetResources();
            if (_resources == null)
            {
                Model.IsUp = false;
                return;
            }

            Model.IsUp = true;
            Model.OS = _resources.OSPlatform;
            Model.PercentFreeCores = Percent((ulong)_resources.NbFreeCores, (ulong)_resources.NbCores);
            Model.PercentFreeMemory = Percent(_resources.AvailablePhysicalMemory, _resources.TotalPhysicalMemory);
            Model.PercentFreeDiskSpace = Percent(_resources.DiskFreeSpace, _resources.DiskSpace);

            Dto.IsUp = Model.IsUp;
            Dto.OS = Model.OS;
            Dto.PercentFreeCores = Model.PercentFreeCores;
            Dto.PercentFreeCores = Model.PercentFreeCores;
            Dto.PercentFreeCores = Model.PercentFreeCores;

            var jobs = _proxy.GetJobs();
            _jobs = jobs?.ToDictionary(p => p.Id, p => p) ?? new Dictionary<Guid, Job>();
        }

        private static double Percent(ulong free, ulong total) => Math.Round((1.0 - (double)free / total) * 100.0, 2);

        public IEnumerable<Job> GetJobs() => _jobs.Values;

        public IEnumerable<JobMessage> FetchMessages(Guid id, int start, int length) => _proxy.FetchMessages(id, start, length);

        public Guid StartJob(string command, string arguments, int nbCores = 1) => _proxy.StartJob(command, arguments, nbCores);

        public void CancelJob(Guid id) => _proxy.CancelJob(id);

        public void DeleteJob(Guid id) => _proxy.DeleteJob(id);

        public NinjaResources GetResources() => _resources;
    }
}
