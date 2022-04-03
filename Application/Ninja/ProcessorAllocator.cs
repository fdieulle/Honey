using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Ninja
{
    public class ProcessorAllocator
    {
        private readonly Dictionary<int, ProcessAffinity> _processes = new Dictionary<int, ProcessAffinity>();

        public int NbCores { get; }

        public int NbUsedCores { get; private set; }

        public int MaskInUsed { get; private set; }

        public ProcessorAllocator() : this(Environment.ProcessorCount) { }
        public ProcessorAllocator(int processorCount)
        {
            NbCores = processorCount;
        }

        public List<ProcessAffinity> GetAffinityPlan(int pid, int nbCores)
        {
            var result = new List<ProcessAffinity>();
            if (nbCores <= 0 || nbCores + NbUsedCores >= NbCores)
                return result;

            var pa = new ProcessAffinity() { Pid = pid, NbCores = nbCores };

            pa.Fit(MaskInUsed);

            // Max reached so we need to change affinity of running process
            if (pa.Affinity >= (1 << NbCores))
            {
                result.AddRange(Shrink());
                
                pa.Fit(MaskInUsed);
            }

            MaskInUsed |= pa.Affinity;
            _processes.Add(pid, pa);
            NbUsedCores += pa.NbCores;

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

            MaskInUsed = mask;
        }

        public void RemoveProcess(int pid)
        {
            if (!_processes.TryGetValue(pid, out var pa))
                return;

            MaskInUsed ^= pa.Affinity;
            NbUsedCores -= pa.NbCores;
            _processes.Remove(pid);
        }
    }
}
