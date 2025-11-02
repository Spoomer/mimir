namespace Mimir.Worker.Initializer.Manager;

public class TableSheetInitializerManager : IInitializerManager
{
    private readonly List<BaseInitializer> _initializers;

    public TableSheetInitializerManager(TableSheetInitializer tableSheetInitializer)
    {
        _initializers =
        [
            tableSheetInitializer
        ];
    }

    public async Task WaitInitializers(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && _initializers.Any(initializer => initializer.ExecuteTask is null))
        {
            await Task.Yield();
        }

        await Task.WhenAll(_initializers.Select(initializer => initializer.ExecuteTask!));
    }
}
