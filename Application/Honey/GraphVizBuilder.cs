using Domain.Dtos.Workflows;
using Domain.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Application.Honey
{
    public static class GraphVizBuilder
    {
        public static string CreateGraph(JobViewModel root)
        {
            var sb = new StringBuilder();
            sb.AppendLine("digraph {");
            sb.AppendLine("rankdir=\"LR\"");

            var start = new JobViewModel { Name = "Start", Type = "Task", Id = Guid.NewGuid(), Status = root.Status };
            sb.AppendNodeAttribute(start);

            var predecessors = sb.AppendNode(root, new List<JobViewModel> { start });

            var end = new JobViewModel { Name = "End", Type = "Task", Id = Guid.NewGuid() };
            if (root.Status.IsFinal())
                end.Status = root.Status;
            sb.AppendNode(end, predecessors);

            sb.AppendLine("}");
            return sb.ToString();
        }

        private static List<JobViewModel> AppendNode(this StringBuilder sb, JobViewModel node, List<JobViewModel> predecessors)
        {
            if (node.Type == JobsBehavior.Parallel.ToString())
                return sb.AppendParallelNode(node, predecessors);
            else if (node.Type == JobsBehavior.Sequential.ToString())
                return sb.AppendSequenceNode(node, predecessors);
            else
                return sb.AppendTaskNode(node, predecessors);
        }

        private static List<JobViewModel> AppendParallelNode(this StringBuilder sb, JobViewModel node, List<JobViewModel> predecessors)
        {
            var successors = new List<JobViewModel>();
            if (node.Children == null) return successors;

            var successor = sb.AppendTaskNode(node, predecessors);
            if (node.Children.Count > 4)
            {
                foreach (var child in node.Children.Take(2))
                    successors.AddRange(sb.AppendNode(child, successor));
                successors.AddRange(sb.AppendTextNode("...", successor));
                foreach (var child in node.Children.Skip(node.Children.Count - 2))
                    successors.AddRange(sb.AppendNode(child, successor));
            }
            else
            {
                foreach (var child in node.Children)
                    successors.AddRange(sb.AppendNode(child, successor));
            }

            return successors.Count > 0 ? successors : successor;
        }

        private static List<JobViewModel> AppendSequenceNode(this StringBuilder sb, JobViewModel node, List<JobViewModel> predecessors)
        {
            var successors = new List<JobViewModel>();
            if (node.Children == null) return successors;

            successors = predecessors;
            foreach (var child in node.Children)
            {
                successors = sb.AppendNode(child, predecessors);
                predecessors = successors;
            }

            return successors;
        }

        private static List<JobViewModel> AppendTaskNode(this StringBuilder sb, JobViewModel node, List<JobViewModel> predecessors)
        {
            sb.AppendNodeAttribute(node);

            foreach (var predecessor in predecessors)
                sb.AppendEdge(predecessor, node);

            return new List<JobViewModel> { node };
        }

        private static List<JobViewModel> AppendTextNode(this StringBuilder sb, string text, List<JobViewModel> predecessors) 
            => sb.AppendTaskNode(new JobViewModel { Id = Guid.NewGuid(), Name = text, Type = "Text" }, predecessors);

        private static void AppendNodeAttribute(this StringBuilder sb, JobViewModel node)
        {
            sb.AppendNodeAttribute(node.Id, "label", node.Name);
            sb.AppendNodeAttribute(node.Id, "style", "filled");
            switch (node.Status)
            {
                case JobStatus.Pending:
                    //sb.AppendNodeAttribute(node.Id, "color", "#fdfdfe");
                    sb.AppendNodeAttribute(node.Id, "color", "#818182");
                    sb.AppendNodeAttribute(node.Id, "fillcolor", "#fefefe");
                    sb.AppendNodeAttribute(node.Id, "fontcolor", "#818182");
                    break;
                case JobStatus.Running:
                    sb.AppendNodeAttribute(node.Id, "color", "#bee5eb");
                    sb.AppendNodeAttribute(node.Id, "fillcolor", "#d1ecf1");
                    sb.AppendNodeAttribute(node.Id, "fontcolor", "#0c5460");
                    break;
                case JobStatus.Completed:
                    sb.AppendNodeAttribute(node.Id, "color", "#c3e6cb");
                    sb.AppendNodeAttribute(node.Id, "fillcolor", "#d4edda");
                    sb.AppendNodeAttribute(node.Id, "fontcolor", "#155724");
                    break;
                case JobStatus.CancelRequested:
                case JobStatus.Cancel:
                    sb.AppendNodeAttribute(node.Id, "color", "#ffeeba");
                    sb.AppendNodeAttribute(node.Id, "fillcolor", "#fff3cd");
                    sb.AppendNodeAttribute(node.Id, "fontcolor", "#856404");
                    break;
                case JobStatus.Error:
                    sb.AppendNodeAttribute(node.Id, "color", "#f5c6cb");
                    sb.AppendNodeAttribute(node.Id, "fillcolor", "#f8d7da");
                    sb.AppendNodeAttribute(node.Id, "fontcolor", "#721c24");
                    break;
                case JobStatus.DeleteRequested:
                case JobStatus.Deleted:
                    sb.AppendNodeAttribute(node.Id, "color", "#c6c8ca");
                    sb.AppendNodeAttribute(node.Id, "fillcolor", "#d6d8d9");
                    sb.AppendNodeAttribute(node.Id, "fontcolor", "#1b1e21");
                    break;
            }
        }

        private static void AppendNodeAttribute(this StringBuilder sb, Guid id, string name, string value)
        {
            sb.Append('"');
            sb.Append(id);
            sb.Append('"');
            sb.Append(" [");
            sb.Append(name);
            sb.Append('=');
            sb.Append('"');
            sb.Append(value);
            sb.Append('"');
            sb.Append("]");
        }

        private static void AppendEdge(this StringBuilder sb, JobViewModel src, JobViewModel dst)
        {
            sb.Append('"');
            sb.Append(src.Id);
            sb.Append('"');
            sb.Append(" -> ");
            sb.Append('"');
            sb.Append(dst.Id);
            sb.Append('"');
        }
    }
}
