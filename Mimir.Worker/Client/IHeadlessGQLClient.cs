using Libplanet.Crypto;

namespace Mimir.Worker.Client;

public interface IHeadlessGQLClient
{
    Task<(GetAccountDiffsResponse response, string jsonResponse)> GetAccountDiffsAsync(
        long baseIndex,
        long changedIndex,
        Address accountAddress,
        CancellationToken stoppingToken
    );
    Task<(GetTipResponse response, string jsonResponse)> GetTipAsync(
        CancellationToken stoppingToken,
        Address? accountAddress);
    Task<(GetStateResponse response, string jsonResponse)> GetStateAsync(
        Address accountAddress,
        Address address,
        CancellationToken stoppingToken,
        long? blockIndex = null
    );
    Task<GetTransactionsResponse> GetTransactionsAsync(
        long blockIndex,
        CancellationToken stoppingToken
    );
}
