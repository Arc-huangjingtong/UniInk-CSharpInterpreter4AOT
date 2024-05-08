namespace Arc.UniInk.NunitTest
{

    using BenchmarkDotNet.Running;


    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<UniInk_Speed_Tests>();
        }
    }

}