using Application.Honey;
using Domain.Dtos.Workflows;
using Domain.ViewModels;
using System;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Application.Tests.Honey
{
    public class GraphVizBuilderTests
    {
        [Fact]
        public void TestComplexGraph()
        {
            var root =
                Sequence("build model",
                    Sequence("build feed checkers",
                        Parallel("build feed checkers", Tasks(10, "[I {0}] build feed checkers")),
                        Task("aggregate feed checkers")),
                    Parallel("select features",
                        Sequence("[Sk 1]",
                            Parallel("Log features", Tasks(50, "#{0} log features")),
                            Parallel("Compress features", Tasks(50, "#{0} compress features")),
                            Parallel("[Sk 1] select features", Task("select features"))),
                        Sequence("[Sk 2]",
                            Parallel("Log features", Tasks(50, "#{0} log features")),
                            Parallel("Compress features", Tasks(50, "#{0} compress features")),
                            Parallel("[Sk 2] select features", Task("select features"))),
                        Sequence("[Sk 3]",
                            Parallel("Log features", Tasks(50, "#{0} log features")),
                            Parallel("Compress features", Tasks(50, "#{0} compress features")),
                            Parallel("[Sk 3] select features", Task("select features"))),
                        Sequence("[Sk 4]",
                            Parallel("Log features", Tasks(50, "#{0} log features")),
                            Parallel("Compress features", Tasks(50, "#{0} compress features")),
                            Parallel("[Sk 4] select features", Task("select features"))),
                        Sequence("[Sk 5]",
                            Parallel("Log features", Tasks(50, "#{0} log features")),
                            Parallel("Compress features", Tasks(50, "#{0} compress features")),
                            Parallel("[Sk 5] select features", Task("select features")))),
                    Parallel("Learn CV", Tasks(10, "learn Fold #{0}")),
                    Task("Build model"));

            var result = GraphVizBuilder.CreateGraph(root);
            Assert.NotNull(result);
        }

        private static JobViewModel Task(string name) => new JobViewModel { Name = name, Id = Guid.NewGuid() };
        private static JobViewModel[] Tasks(int count, string name) 
            => Enumerable.Range(1, 10).Select(p => Task(string.Format(name, p))).ToArray();

        private static JobViewModel Parallel(string name, params JobViewModel[] jobs) 
            => new JobViewModel { Name = name, Id = Guid.NewGuid(), Children = jobs?.ToList(), Type = JobsBehavior.Parallel.ToString() };
        private static JobViewModel Sequence(string name, params JobViewModel[] jobs)
            => new JobViewModel { Name = name, Id = Guid.NewGuid(), Children = jobs?.ToList(), Type = JobsBehavior.Sequential.ToString() };
    }
}
