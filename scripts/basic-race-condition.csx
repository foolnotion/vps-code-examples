using System.Threading;

private static void PrintMessage(string message, TimeSpan sleep) {
    Thread.Sleep(sleep);
    Console.WriteLine(message);
}

private static TimeSpan sleep = TimeSpan.FromSeconds(1);

private static void Yes() => PrintMessage("yes", sleep);
private static void No() => PrintMessage("no", sleep);

var threads = new[] {
    new Thread(Yes),
    new Thread(No)
};

foreach(var t in threads) {
    t.Start();
}

foreach(var t in threads) {
    t.Join();
}