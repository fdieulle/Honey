using System;
using System.Collections.Generic;
using System.Diagnostics;
using Domain;
using Domain.Entities;

namespace Application.Ninja
{
    public static class Extensions
    {
        public static bool IsFinalState(this RunningTask task) => task.Status.IsFinal();

        public static bool IsFinalState(this TaskEntity task) => task.Status.IsFinal();

        public static bool TryGetValueLocked<TKey, TValue>(this Dictionary<TKey, TValue> dico, TKey key, out TValue value)
        {
            lock(dico)
                return dico.TryGetValue(key, out value);
        }

        public static void SetAffinity(this ProcessorAllocator allocator, int pid, int nbCores)
        {
            if (pid <= 0 || nbCores <= 0) return;

            foreach (var aff in allocator.GetAffinityPlan(pid, nbCores))
            {
                try
                {
                    var process = Process.GetProcessById(aff.Pid);
                    process.ProcessorAffinity = (IntPtr)aff.Affinity;
                }
                catch (Exception)
                {
                    allocator.RemoveProcess(aff.Pid);
                }
            }
        }
    }
}
