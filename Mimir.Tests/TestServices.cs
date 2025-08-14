using System.Linq.Expressions;
using System.Numerics;
using System.Security.Cryptography;
using Hangfire;
using HotChocolate;
using HotChocolate.Execution;
using Lib9c.GraphQL.Types;
using Lib9c.Models.States;
using Libplanet.Common;
using Libplanet.Crypto;
using Microsoft.Extensions.DependencyInjection;
using Mimir.MongoDB.Bson;
using Mimir.MongoDB.Repositories;
using Mimir.MongoDB.Services;
using Mimir.Services;
using MongoDB.Driver;
using Moq;

namespace Mimir.Tests;

public static class TestServices
{
    public class ServiceProviderBuilder
    {
        private readonly ServiceCollection _serviceCollection;
        private Action<IServiceCollection>? _configure;

        public ServiceProviderBuilder()
        {
            _serviceCollection = new ServiceCollection();
            _serviceCollection
                .AddGraphQLServer()
                .AddLib9cGraphQLTypes()
                .AddMimirGraphQLTypes()
                .BindRuntimeType(typeof(Address), typeof(AddressType))
                .BindRuntimeType(typeof(BigInteger), typeof(BigIntegerType))
                .BindRuntimeType(typeof(HashDigest<SHA256>), typeof(HashDigestSHA256Type));

            var mockClient = new Mock<IBackgroundJobClient>();
            _serviceCollection.AddSingleton<IBackgroundJobClient>(mockClient.Object);
        }

        public ServiceProviderBuilder With<T>(T service)
            where T : class
        {
            _serviceCollection.AddSingleton(service);
            return this;
        }

        public ServiceProviderBuilder With(Action<IServiceCollection>? configure)
        {
            _configure = configure;
            return this;
        }

        public IServiceProvider Build()
        {
            _configure?.Invoke(_serviceCollection);
            return _serviceCollection.BuildServiceProvider();
        }
    }

    public static ServiceProviderBuilder Builder => new();

    public static async Task<string> ExecuteRequestAsync(
        IServiceProvider serviceProvider,
        Action<OperationRequestBuilder> configureRequest,
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var requestBuilder = new OperationRequestBuilder();
        requestBuilder.SetServices(scope.ServiceProvider);
        configureRequest(requestBuilder);
        var request = requestBuilder.Build();

        var executor = await scope
            .ServiceProvider.GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync(cancellationToken: cancellationToken);
        await using var result = await executor.ExecuteAsync(request, cancellationToken);
        result.ExpectOperationResult();
        return result.ToJson();
    }
}
