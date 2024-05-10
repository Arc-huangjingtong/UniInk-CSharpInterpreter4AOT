namespace Arc.UniInk.Test.Utils
{

    using System;


    public class MemoryTool : IDisposable
    {
        public static MemoryTool Create() => new();

        public MemoryTool() => _memory_start = GC.GetTotalMemory(true);


        public long _memory_start = 0;
        public long _memory_end   = 0;



        public void Dispose()
        {
            _memory_end = GC.GetTotalMemory(true);

            var memoryUsed = _memory_end - _memory_start;

            Console.WriteLine($"Memory used by MyMethod: {memoryUsed} bytes");
        }
    }

}