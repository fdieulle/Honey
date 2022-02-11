using Ninja.Services;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Ninja.Tests
{
    public class ProcessortAllocatorTests
    {
        [Fact]
        public void TestAllocSingleCoreFirst()
        {
            var allocator = new ProcessorAllocator(8);

            var result = allocator.GetAffinityPlan(1, 1);
            result.Check("01000000");
            allocator.Check("01000000");
        }

        [Fact]
        public void TestAlloc2CoresFirst()
        {
            var allocator = new ProcessorAllocator(8);

            var result = allocator.GetAffinityPlan(1, 2);
            result.Check("00110000");
            allocator.Check("00110000");
        }

        [Fact]
        public void TestAllocWf1()
        {
            var allocator = new ProcessorAllocator(8);

            var result = allocator.GetAffinityPlan(1, 1);
            result.Check("01000000");
            allocator.Check("01000000");

            result = allocator.GetAffinityPlan(2, 2);
            result.Check("00110000");
            allocator.Check("01110000");

            allocator.RemoveProcess(1);
            allocator.Check("00110000");

            result = allocator.GetAffinityPlan(3, 2);
            result.Check("00001100");
            allocator.Check("00111100");

            allocator.RemoveProcess(2);
            allocator.Check("00001100");

            result = allocator.GetAffinityPlan(4, 1);
            result.Check("01000000");
            allocator.Check("01001100");

            result = allocator.GetAffinityPlan(5, 2);
            result.Check("00110000");
            allocator.Check("01111100");
        }

        [Fact]
        public void TestAllocWf2()
        {
            var allocator = new ProcessorAllocator(8);
            var pid = 0;

            for(var i = 0; i < 7; i++)
                allocator.GetAffinityPlan(++pid, 1);
            allocator.Check("01111111");

            allocator.RemoveProcess(2);
            allocator.RemoveProcess(6);

            allocator.Check("01011101");

            var result = allocator.GetAffinityPlan(++pid, 2);
            result.Check(
                "01000000",
                "00100000",
                "00010000",
                "00001000",
                "00000100",
                "00000011");
            allocator.Check("01111111");
        }
    }

    public static class ProcessortAllocatorTestsExtensions
    {
        public static void Check(this List<ProcessAffinity> affinities, params string[] checks)
        {
            Assert.Equal(affinities.Count, checks.Length);
            for(var i=0; i < affinities.Count; i++)
                affinities[i].Affinity.Check(checks[i]);
        }

        public static void Check(this int affinity, string check, int max = 8)
        {
            Assert.True(affinity < (1 << max));
            var str = "";
            for (var j = 0; j < max; j++)
            {
                var bit = (1 << j);
                if (((affinity & bit) ^ bit) == 0)
                    str += '1';
                else str += '0';
            }

            Assert.Equal(str, check);
        }

        public static void Check(this ProcessorAllocator allocator, string check)
        {
            allocator.MaskInUsed.Check(check);
        }
    }
}
