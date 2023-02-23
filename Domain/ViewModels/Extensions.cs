namespace Domain.ViewModels
{
    public static class Extensions
    {
        public static bool HasDetails(this JobViewModel job) => job is HostedJobViewModel;

        public static HostedJobViewModel Details(this JobViewModel job) => (job as HostedJobViewModel) ?? HostedJobViewModel.Empty;
    }
}
