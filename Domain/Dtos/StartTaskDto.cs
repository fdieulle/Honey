namespace Domain.Dtos
{
    public class StartTaskDto
    {
        public string Command { get; set; }
        public string Arguments { get; set; }
        public int NbCores { get; set; } = 1;
    }
}
