using Application.Dojo;
using Domain.Dtos;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Application.Tests.Dojo
{
    public class ShogunTests
    {
        [Fact]
        public void TestExecute()
        {
            var container = Substitute.For<INinjaContainer>();
            var ninja = Substitute.For<INinja>();

            var dojo = new Application.Dojo.Dojo(container);
            var queueProvider = new QueueProvider(dojo);
            var shogun = new Shogun(queueProvider);

            // Setup a ninja
            ninja.GetResources().Returns(R("http://ninja1:8080"));
            ninja.StartTask(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int>())
                .Returns(Guid.NewGuid());
            container.Resolve(Arg.Any<string>()).Returns(ninja);

            // Enroll a ninja and enforce a refreseh to set it up
            dojo.EnrollNinja("http://ninja1:8080");
            dojo.GetNinja("http://ninja1:8080").Refresh();

            // Create a queue
            queueProvider.CreateQueue(Q("queue"));

            var id = shogun.Execute("queue", T("powershell", "-version"));

            Assert.NotEqual(id, Guid.Empty);
            ninja.Received().StartTask(
                Arg.Is("powershell"),
                Arg.Is("-version"),
                Arg.Is(1));
        }

        private static NinjaResourcesDto R(string address) => new NinjaResourcesDto()
        {
            MachineName = address,
            OSPlatform = "Win32",
            NbCores = 10,
            NbFreeCores = 9,
            AvailablePhysicalMemory = (ulong)24e9,
            TotalPhysicalMemory = (ulong)32e9,
            DiskSpace = (ulong)250e9,
            DiskFreeSpace = (ulong)198e9,
        };

        private static QueueDto Q(string name) => new QueueDto
        {
            Name = name,
            MaxParallelTasks = -1
        };

        private static StartTaskDto T(string command, string arguments, int nbCores = 1) => new StartTaskDto
        {
            Command = command,
            Arguments = arguments,
            NbCores = nbCores,
        };
    }
}
