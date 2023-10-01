using System;
using System.Threading;

private static void DoStuff() {
    Console.WriteLine($"thread {Thread.CurrentThread.ManagedThreadId} did stuff.");
}

private static void DoStuff(object data) {
    Console.WriteLine($"thread {Thread.CurrentThread.ManagedThreadId} did stuff with data (data = {data}).");
}

var t1 = new Thread(new ThreadStart(DoStuff));
t1.Start();

var t2 = new Thread(new ParameterizedThreadStart(DoStuff));
t2.Start(42);

var t3 = new Thread(() => {
    Console.WriteLine($"thread {Thread.CurrentThread.ManagedThreadId} ran with a lambda");
});
t3.Start();

var t4 = new Thread(x => {
    Console.WriteLine($"thread {Thread.CurrentThread.ManagedThreadId} ran with a parameterized lambda (x = {x})");
});
t4.Start(1234);