using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Domain.Dtos.Workflows;
using Domain.ViewModels;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Linq;

namespace Honey.Components
{
    public class JobNodeModel : NodeModel
    {
        public JobNodeModel(Point position = null) : base(position) { }

        public JobViewModel Job { get; set; } = JobViewModel.Empty;
    }

    public static class JobNodeExtensions
    {
        private static readonly JobViewModel startJob = new JobViewModel { Id = Guid.NewGuid(), Name = "Start", Status = JobStatus.Deleted };
        private static readonly JobViewModel endJob = new JobViewModel { Id = Guid.NewGuid(), Name = "End", Status = JobStatus.Deleted };

        public static bool HasDetails(this JobNodeModel node) => node != null && node.Job.HasDetails();
        public static bool HasDetails(this JobViewModel job) => job is HostedJobViewModel;

        public static HostedJobViewModel Details(this JobNodeModel node) => node?.Details() ?? HostedJobViewModel.Empty;
        public static HostedJobViewModel Details(this JobViewModel job) => (job as HostedJobViewModel) ?? HostedJobViewModel.Empty;

        public static void BuildGraph(this Diagram diagram, JobViewModel root)
        {
            Debug.WriteLine($"========================================");

            diagram.Links.Clear();
            diagram.Nodes.Clear();

            var start = NewNode(startJob, 50, 50, hasLeft: false);
            diagram.Nodes.Add(start);
            var end = NewNode(endJob, 50, 50, hasRight: false);

            var maxX = double.NegativeInfinity;
            var minY = double.PositiveInfinity;
            //var maxY = double.NegativeInfinity;
            foreach (var parent in diagram.BuildGraph(new[] { start }, root))
            {
                maxX = Math.Max(maxX, (parent.Size?.Width ?? 0) + parent.Position.X);
                minY = Math.Min(minY, parent.Position.Y);
                //maxY = Math.Max(maxY, (parent.Size?.Height ?? 0) + parent.Position.Y);

                var link = NewLink(parent, end);
                diagram.Links.Add(link);

                Debug.WriteLine($"Link: {parent.Title} -> End");
            }
            end.SetPosition(maxX + 150, end.Position.Y);
            diagram.Nodes.Add(end);

            Debug.WriteLine($"Node: End - [{end.Position}]");

            Debug.WriteLine($"=== State");
            foreach (var node in diagram.Nodes)
                Debug.WriteLine($"Node: {node.Title} - [{node.Position}]");
            foreach (var link in diagram.Links)
                Debug.WriteLine($"Link: {link.SourcePort.Parent.Title} -> {link.TargetPort.Parent.Title}");
            Debug.WriteLine($"=== EndState");
        }

        private static IEnumerable<NodeModel> BuildGraph(this Diagram diagram, IEnumerable<NodeModel> parents, JobViewModel job)
        {
            if (job is HostedJobViewModel leaf)
            {
                var node = NewNode(leaf, 0, 0);
                diagram.Nodes.Add(node);

                var maxX = double.NegativeInfinity;
                var minY = double.PositiveInfinity;
                //var maxY = double.NegativeInfinity;
                foreach (var parent in parents)
                {
                    var link = NewLink(parent, node);
                    diagram.Links.Add(link);

                    Debug.WriteLine($"Link: {parent.Title} -> {node.Title}");

                    maxX = Math.Max(maxX, (parent.Size?.Width ?? 0) + parent.Position.X);
                    minY = Math.Min(minY, parent.Position.Y);
                    //maxY = Math.Max(maxY, (parent.Size?.Height ?? 0) + parent.Position.Y);
                }

                node.SetPosition(maxX + 150, minY);
                Debug.WriteLine($"Node: {node.Title} - [{node.Position}]");

                yield return node;
            }

            if (job.Type == JobsBehavior.Sequential.ToString())
            {
                foreach (var child in job.Children)
                    parents = diagram.BuildGraph(parents, child);

                foreach (var node in parents)
                    yield return node;
            }

            if (job.Type == JobsBehavior.Parallel.ToString())
            {
                var minY = parents.Min(p => p.Position.Y);
                foreach (var child in job.Children)
                {
                    foreach (var node in diagram.BuildGraph(parents, child))
                    {
                        node.SetPosition(node.Position.X, minY);
                        Debug.WriteLine($"Node: {node.Title} - [{node.Position}]");
                        yield return node;
                        minY += 50;
                    }
                }
            }
        }

        private static JobNodeModel NewNode(JobViewModel job, double x, double y, bool hasLeft = true, bool hasRight = true)
        {
            var node = new JobNodeModel(new Point(x, y))
            {
                Locked = true,
                Title = job.Name,
                Job = job
            };

            if (hasLeft)
                node.AddPort(new PortModel(node, PortAlignment.Left) { Locked = true });
            if (hasRight)
                node.AddPort(new PortModel(node, PortAlignment.Right) { Locked = true });
            return node;
        }

        private static LinkModel NewLink(NodeModel from, NodeModel to) 
            => new LinkModel(from.GetPort(PortAlignment.Right), to.GetPort(PortAlignment.Left)) { Locked = true };
    }
}
