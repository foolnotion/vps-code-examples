using System.Diagnostics;
using System.Threading;
// using System.Random;

public class ParallelSumExample
{
    public static int N = 1024; // number of tasks
    public static int T = 1;    // operation duration (in milliseconds)


    private static double Add(double a, double b, int t = 1) {
        Thread.Sleep(t);
        return a + b;
    }

    private static void PartialSum(object obj)
    {
        var (arr, i, j, t) = (Tuple<double[], int, int, int>)obj;
        arr[i] = Add(arr[i], arr[j], t);
    }

    public static double[] MakeRandomArray(Random random, int size)
    {
        var arr = new double[size];
        for (var i = 0; i < size; ++i) { arr[i] = random.NextDouble(); }
        return arr;
    }

    public static double SequentialSum(double[] data)
    {
        double sum = 0;
        foreach(var v in data) {
            sum = Add(sum, v, T);
        }
        return sum;
    }

    public static double ParallelSum(double[] data)
    {
        var arr = data.ToArray();
        var len = arr.Length;
        var span = (int)Math.Log2(arr.Length);

        for(var i = 1; i <= span; ++i) {
            var p = (int)Math.Pow(2, i);
            for (var j = 0; j < data.Length; ++j) {
                if ((j+1) % p == 0) {
                    arr[j] = Add(arr[j], arr[j - p/2], T);
                }
            }
        }
        return arr[len-1];
    }

    public static double ParallelSumBasicThread(double[] data, int numThreads)
    {
        var sum = 0d;
        var locker = new object();
        var semaphore = new SemaphoreSlim(numThreads, numThreads);

        using (var countdownEvent = new CountdownEvent(1)) {
            for(var i = 0; i < data.Length; ++i) {
                countdownEvent.AddCount();
                var t = new Thread(x => {
                    try {
                        semaphore.Wait();
                        var k = (int)x;
                        lock(locker) { sum = Add(sum, data[k]); }
                        semaphore.Release();
                    } finally {
                        countdownEvent.Signal();
                }});
                t.Start(i);
            }
            countdownEvent.Signal();
            countdownEvent.Wait();
        }
        return sum;
    }

    public static double ParallelSumThread(double[] data, int numThreads)
    {
        var arr = data.ToArray();
        var len = arr.Length;
        var span = (int)Math.Log2(arr.Length);

        for(var i = 1; i <= span; ++i) {
            var p = (int)Math.Pow(2, i);
            var threads = new List<Thread>();
            var semaphore = new SemaphoreSlim(numThreads, numThreads);

            for (var j = 0; j < data.Length; ++j) {
                if ((j+1) % p == 0) {
                    var t = new Thread(x => {
                        semaphore.Wait();
                        var k = (int)x;
                        arr[k] = Add(arr[k], arr[k - p/2]);
                        semaphore.Release();
                    });
                    threads.Add(t);
                    t.Start(j);
                }
            }
            foreach(var t in threads) {
                t.Join();
            }
        }
        return arr[len-1];
    }

    public static double ParallelSumThreadCDE(double[] data, int numThreads)
    {
        var arr = data.ToArray();
        var len = arr.Length;
        var span = (int)Math.Log2(arr.Length);

        for(var i = 1; i <= span; ++i) {
            var p = (int)Math.Pow(2, i);
            var semaphore = new SemaphoreSlim(numThreads, numThreads);

            using (var countdownEvent = new CountdownEvent(1)) {
                for (var j = 0; j < data.Length; ++j) {
                    if ((j+1) % p == 0) {
                        countdownEvent.AddCount();
                        var t = new Thread(x => {
                            try {
                                semaphore.Wait();
                                var k = (int)x;
                                arr[k] = Add(arr[k], arr[k - p/2]);
                                semaphore.Release();
                            } finally {
                                countdownEvent.Signal();
                            }});
                        t.Start(j);
                    }
                }
                countdownEvent.Signal();
                countdownEvent.Wait();
            }
        }
        return arr[len-1];
    }

    public static double ParallelSumThreadBarrier(double[] data, int numThreads)
    {
        var arr = data.ToArray();
        var len = arr.Length;
        var span = (int)Math.Log2(arr.Length);

        for(var i = 1; i <= span; ++i) {
            var p = (int)Math.Pow(2, i);
            var semaphore = new SemaphoreSlim(numThreads, numThreads);

            using (var barrier = new Barrier(1)) {
                for (var j = 0; j < data.Length; ++j) {
                    if ((j+1) % p == 0) {
                        barrier.AddParticipant();
                        var t = new Thread(x => {
                            try {
                                semaphore.Wait();
                                var k = (int)x;
                                arr[k] = Add(arr[k], arr[k - p/2]);
                                semaphore.Release();
                            } finally {
                                barrier.SignalAndWait();
                            }});
                        t.Start(j);
                    }
                }
                barrier.SignalAndWait();
            }
        }
        return arr[len-1];
    }

    public static double ParallelSumThreadPool(double[] data, int numThreads)
    {
        var arr = data.ToArray();
        var len = arr.Length;
        var span = (int)Math.Log2(arr.Length);

        for(var i = 1; i <= span; ++i) {
            var p = (int)Math.Pow(2, i);
            var semaphore = new SemaphoreSlim(numThreads, numThreads);
            using (var countDownEvent = new CountdownEvent(1))
            {
                for (var j = 0; j < data.Length; ++j) {
                    if ((j+1) % p == 0) {
                        countDownEvent.AddCount();
                        ThreadPool.QueueUserWorkItem(x => {
                            try {
                                semaphore.Wait();
                                var k = (int)x;
                                arr[k] = Add(arr[k], arr[k - p/2]);
                                semaphore.Release();
                            } finally {
                                countDownEvent.Signal();
                            }
                        }, j);
                    }
                }
                countDownEvent.Signal();
                countDownEvent.Wait();
            }
        }
        return arr[len-1];
    }

    public static double ParallelSumThreadPoolOld(double[] data, int numThreads)
    {
        var arr = data.ToArray();
        var len = arr.Length;
        var span = (int)Math.Log2(len);
        var stride = 1;

        for (var s = 0; s < span; ++s)
        {
            var semaphore = new SemaphoreSlim(numThreads, numThreads);
            using (var countDownEvent = new CountdownEvent(1))
            {
                for (var i = 0; i < len; i += 2)
                {
                    countDownEvent.AddCount();
                    ThreadPool.QueueUserWorkItem(x =>
                    {
                        try {
                            semaphore.Wait();
                            PartialSum(x);
                            semaphore.Release();
                        } finally {
                            countDownEvent.Signal();
                        }
                    }, Tuple.Create(arr, i * stride, (i + 1) * stride, T));
                }
                countDownEvent.Signal();
                countDownEvent.Wait();
            }
            stride *= 2;
            len /= 2;
        }

        return arr[0];
    }

    public static double ParallelSumTaskFactory(double[] data, int numThreads)
    {
        var arr = data.ToArray();
        var span = Math.Log2(arr.Length);
        var len = arr.Length;
        var stride = 1;

        for(var i = 1; i <= span; ++i) {
            var p = (int)Math.Pow(2, i);
            var semaphore = new SemaphoreSlim(numThreads, numThreads);
            var tasks = new List<Task>();

            for (var j = 0; j < data.Length; ++j) {
                if ((j+1) % p == 0) {
                    var k = j; // use local capture
                    tasks.Add(Task.Run(() => {
                        semaphore.Wait();
                        arr[k] = Add(arr[k], arr[k - p/2]);
                        semaphore.Release();
                    }));
                }
            }
            Task.WaitAll(tasks.ToArray());
        }
        return arr[len-1];
    }

    public static void Main()
    {
        var random = new Random(1234);
        var numbers = MakeRandomArray(random, N);

        var sum = 0.0;

        Console.WriteLine($"sequential:        {SequentialSum(numbers)}");
        Console.WriteLine($"pseudo-parallel:   {ParallelSum(numbers)}");
        Console.WriteLine($"basic-threads-sum: {ParallelSumBasicThread(numbers, 32)}");
        Console.WriteLine($"basic-threads-sem: {ParallelSumThread(numbers, 32)}");
        Console.WriteLine($"basic-threads-cde: {ParallelSumThreadCDE(numbers, 32)}");
        Console.WriteLine($"basic-threads-bar: {ParallelSumThreadBarrier(numbers, 32)}");
        Console.WriteLine($"threadpool-1:      {ParallelSumThreadPool(numbers, 32)}");
        Console.WriteLine($"threadpool-2:      {ParallelSumThreadPoolOld(numbers, 32)}");
        Console.WriteLine($"task-factory:      {ParallelSumTaskFactory(numbers, 32)}");
    }
}