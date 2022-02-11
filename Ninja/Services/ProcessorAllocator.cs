using System;
using System.Collections.Generic;
using System.Linq;

namespace Ninja.Services
{
    public class ProcessorAllocator
    {
        private readonly int _maxNbProcessors;
        private int _nbProcessors;
        private int _maskInUsed;
        private readonly Dictionary<int, ProcessAffinity> _processes = new Dictionary<int, ProcessAffinity>();

        public int MaskInUsed => _maskInUsed;

        public ProcessorAllocator() : this(Environment.ProcessorCount) { }
        public ProcessorAllocator(int processorCount)
        {
            _maxNbProcessors = processorCount;
        }

        public List<ProcessAffinity> GetAffinityPlan(int pid, int nbCores)
        {
            var result = new List<ProcessAffinity>();
            if (nbCores <= 0 || nbCores + _nbProcessors >= _maxNbProcessors)
                return result;

            var pa = new ProcessAffinity() { Pid = pid, NbCores = nbCores };

            pa.Fit(_maskInUsed);

            // Max reached so we need to change affinity of running process
            if (pa.Affinity >= (1 << _maxNbProcessors))
            {
                result.AddRange(Shrink());
                
                pa.Fit(_maskInUsed);
            }

            _maskInUsed |= pa.Affinity;
            _processes.Add(pid, pa);
            _nbProcessors += pa.NbCores;

            result.Add(pa);
            return result;
        }

        private IEnumerable<ProcessAffinity> Shrink()
        {
            var odds = _processes.Values.Where(p => p.IsOdd()).OrderBy(p => p.NbCores).ToList();
            var evens = _processes.Values.Where(p => !p.IsOdd()).ToList();

            var mask = 0;
            var end = odds.Count % 2 == 0 ? odds.Count - 1 : odds.Count;
            for (var i = 0; i < end; i++)
            {
                odds[i].Fit(mask);
                mask |= odds[i].Affinity;
                yield return odds[i];
            }
            foreach(var even in evens)
            {
                even.Fit(mask);
                mask |= even.Affinity;
                yield return even;
            }
            for (var i = end; i < odds.Count; i++)
            {
                odds[i].Fit(mask);
                mask |= odds[i].Affinity;
                yield return odds[i];
            }

            _maskInUsed = mask;
        }

        public void RemoveProcess(int pid)
        {
            if (!_processes.TryGetValue(pid, out var pa))
                return;

            _maskInUsed ^= pa.Affinity;
            _nbProcessors -= pa.NbCores;
            _processes.Remove(pid);
        }
    }
}
