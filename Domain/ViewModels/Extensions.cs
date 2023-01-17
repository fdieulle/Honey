using Domain.Dtos.Workflows;

namespace Domain.ViewModels
{
    public static class Extensions
    {
        public static bool CanStart(this WorkflowViewModel vm) => vm.Status.CanStart();

        public static bool CanCancel(this WorkflowViewModel vm) => vm.Status.CanCancel();

        public static bool CanRecover(this WorkflowViewModel vm) => vm.Status.CanRecover();

        public static bool CanDelete(this WorkflowViewModel vm) => vm.Status.CanDelete();
    }
}
