namespace parallel_sum_test;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Exporters.Csv;

public class ParallelSumTest
{
    [Fact]
    public void ParallelSumCorrectness()
    {
        var numbers = new double[] {
            1, 2, 3, 4, 5, 6, 7, 8
        };
        var a = ParallelSumExample.SequentialSum(numbers);
        var b = ParallelSumExample.ParallelSum(numbers);
        var c = ParallelSumExample.ParallelSumThread(numbers, 1);
        Assert.True(a == b, "The sum should be the same");
        Assert.True(a == c, "The sum should be the same");
    }

    [Fact]
    public void ParallelSumPerformance()
    {
        var logger = new AccumulationLogger();

        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddLogger(logger)
            .AddExporter(CsvMeasurementsExporter.Default)
            .WithOptions(ConfigOptions.DisableOptimizationsValidator);

        BenchmarkRunner.Run<ParallelSumBenchmarks>(config);
    }

    [MaxIterationCount(20)]
    public class ParallelSumBenchmarks {
        private double[] data;
        private int numThread;

        [Params(1024)]
        public int N;

        [ParamsSource(nameof(ValuesForT))]
        public int T;

        // public IEnumerable<int> ValuesForT => Enumerable.Range(1, 32).ToArray();
        public IEnumerable<int> ValuesForT => new[] { 32 };

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random(1234);
            data = ParallelSumExample.MakeRandomArray(random, N);
            numThread = T;
        }

        // [Benchmark]
        // public double SequentialSumPerformance()
        // {
        //     var sum = ParallelSum.ComputeSumSequential(data);
        //     return sum;
        // }

        
        // [Benchmark]
        // public double ParallelSumThreadsBasicPerformance()
        // {
        //     var sum = ParallelSumExample.ParallelSumBasicThread(data, numThread);
        //     return sum;
        // }


        // [Benchmark]
        // public double ParallelSumThreadsPerformance()
        // {
        //     var sum = ParallelSumExample.ParallelSumThread(data, numThread);
        //     return sum;
        // }

        [Benchmark]
        public double ParallelSumThreadsCDEPerformance()
        {
            var sum = ParallelSumExample.ParallelSumThreadCDE(data, numThread);
            return sum;
        }

        [Benchmark]
        public double ParallelSumThreadsBarrierPerformance()
        {
            var sum = ParallelSumExample.ParallelSumThreadBarrier(data, numThread);
            return sum;
        }

        [Benchmark]
        public double ParallelSumThreadPoolPerformance()
        {
            var sum = ParallelSumExample.ParallelSumThreadPool(data, numThread);
            return sum;
        }

        // [Benchmark]
        // public double ParallelSumThreadPoolOld()
        // {
        //     var sum = ParallelSumExample.ParallelSumThreadPoolOld(data, numThread);
        //     return sum;
        // }

        // [Benchmark]
        // public double ParallelSumTaskFactoryPerformance()
        // {
        //     var sum = ParallelSumExample.ParallelSumTaskFactory(data, numThread);
        //     return sum;
        // }
    }
}
