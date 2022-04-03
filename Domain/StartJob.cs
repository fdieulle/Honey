namespace Domain
{
    public class StartJob
    {
        public string Command { get; set; }
        public string Arguments { get; set; }
        public int NbCores { get; set; } = 1;
    }
}
