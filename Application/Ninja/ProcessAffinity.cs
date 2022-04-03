namespace Application.Ninja
{
    public class ProcessAffinity
    {
        public int Pid { get; set; }
        public int NbCores { get; set; }
        public int Affinity { get; set; }

        public bool IsOdd() => NbCores % 2 != 0;

        public void Fit(int maskInUsed)
        {
            Affinity = 0;
            var start = IsOdd() ? 1 : 2; // Keep first core free for the OS
            for (var i = 0; i < NbCores; i++)
                Affinity |= 1 << (start + i);

            while ((Affinity & maskInUsed) != 0)
                Affinity <<= IsOdd() ? 1 : 2;
        }
    }
}
