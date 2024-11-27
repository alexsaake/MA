namespace ProceduralLandscapeGeneration;

internal class GameLoop : IGameLoop
{
    private bool _isRunning = true;
    public void Run()
    {
        MainLoop().GetAwaiter().GetResult();
    }

    private Task MainLoop()
    {
        while (_isRunning)
        {
            Console.WriteLine("Running..");
        }

        Console.WriteLine("Exiting..");
        return Task.CompletedTask;
    }
}
