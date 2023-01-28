using Infrastructure;

void Log(string message)
{
    Console.WriteLine(message);
    File.AppendAllLines("DemoTasks.log", new[] {message});
}

Log($"Args: {string.Join(',', args)}");

var secondsStr = args.Length > 0 ? args[0] : "10";
if (!double.TryParse(secondsStr, out var seconds))
    seconds = 10;

var shouldCrashStr = args.Length > 1 ? args[1] : "false";
if (!bool.TryParse(shouldCrashStr, out var shouldCrash))
    shouldCrash = false;

var ninjaStr = Environment.GetEnvironmentVariable("NINJA_BASE_URI");
var taskIdStr = Environment.GetEnvironmentVariable("NINJA_TASK_ID");

Log($"The task is hosted by {ninjaStr} with the Id: {taskIdStr}");
Log($"and will run {seconds} seconds");

if (ninjaStr == null && taskIdStr == null)
{
    Log("No ninja environment varaible found.");
    Log($"Process exits with errors !");
    Environment.Exit(-1);
}

var ninja = new NinjaClient(ninjaStr);
var taskId = Guid.Parse(taskIdStr ?? "");

Log($"Parsed task Id: {taskId}");

var start = DateTime.Now;
var end = start.AddSeconds(seconds);
var counter = 0;
while (DateTime.Now < end)
{
    var progress = (DateTime.Now - start).TotalSeconds / seconds;
    ninja.UpdateTask(taskId, progress, end, $"Step {++counter}");
    Log($"Step {++counter}");
    Thread.Sleep(1000);
}

if (shouldCrash) throw new Exception("Carsh !");


ninja.UpdateTask(taskId, 1.0, end, $"Done !");
Log($"Step {++counter}");
Log($"Process done !");
Thread.Sleep(100);
 
