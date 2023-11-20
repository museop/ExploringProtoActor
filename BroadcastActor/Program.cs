// See https://aka.ms/new-console-template for more information

using BroadcastActor;
using ExploringProtoActor;
using Microsoft.Extensions.Logging;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Partition;
using Proto.Cluster.Testing;
using Proto.Remote.GrpcNet;
using BroadcastGrain = BroadcastActor.BroadcastGrain;

var actorSystem = NewActorSystem();
await actorSystem
    .Cluster()
    .StartMemberAsync();

await CmdLoop();

await actorSystem
    .Cluster()
    .ShutdownAsync();

return;


async Task CmdLoop()
{
    while (true)
    {
        var line = Console.ReadLine();
        if (line is null) break;

        if (line.StartsWith("/q"))
        {
            break;
        }

        var tokens = line.Split(" ");
        switch (tokens)
        {
            case ["/r", _, ..]:
            case ["/receiver", _, ..]:
            {
                if (!int.TryParse(tokens[1], out var number))
                {
                    Console.WriteLine("can't parse an integer");
                    continue;
                }

                var broadcaster = actorSystem.Cluster().GetBroadcastGrain("test-broadcaster");
                await broadcaster.SetReceivers(new SetReceiverRequest { Number = number }, CancellationToken.None);
                break;
            }
            case ["/b"]:
            case ["/broadcast"]:
            {
                if (tokens.Length >= 3)
                {
                    if (!int.TryParse(tokens[1], out var times))
                    {
                        Console.WriteLine("can't parse an integer");
                        continue;
                    }

                    if (!int.TryParse(tokens[2], out var sleepMillis))
                    {
                        Console.WriteLine("can't parse an integer");
                        continue;
                    }

                    var task = async () =>
                    {
                        var broadcaster = actorSystem.Cluster().GetBroadcastGrain("test-broadcaster");
                        await broadcaster.Broadcast(new BroadcastRequest { Message = "Hello Proto Actor!\n" },
                            CancellationToken.None);
                    };
                    for (int i = 0; i < times; i++)
                    {
                        break;
                    }
                }

                var broadcaster = actorSystem.Cluster().GetBroadcastGrain("test-broadcaster");
                await broadcaster.Broadcast(new BroadcastRequest { Message = "Hello Proto Actor!\n" },
                    CancellationToken.None);
                break;
            }
            case ["/c"]:
            case ["/clear"]:
            {
                var broadcaster = actorSystem.Cluster().GetBroadcastGrain("test-broadcaster");
                await broadcaster.Clear(new ClearRequest(), CancellationToken.None);
                break;
            }
        }
    }
}


ActorSystem NewActorSystem()
{
    Log.SetLoggerFactory(LoggerFactory.Create(l => l.AddConsole().SetMinimumLevel(LogLevel.Information)));

    var actorSystemConfig = ActorSystemConfig
        .Setup();

    var remoteConfig = GrpcNetRemoteConfig
        .BindToLocalhost();

    var clusterConfig = ClusterConfig
        .Setup(
            clusterName: "ExploringProtoActor",
            clusterProvider: new TestProvider(new TestProviderOptions(), new InMemAgent()),
            identityLookup: new PartitionIdentityLookup()
        )
        .WithClusterKind(
            kind: BroadcastGrainActor.Kind,
            prop: Props.FromProducer(() =>
                    new BroadcastGrainActor((context, clusterIdentity) => new BroadcastGrain(context, clusterIdentity)))
                .WithClusterRequestDeduplication(TimeSpan.FromSeconds(10)))
        .WithClusterKind(
            kind: ReceiveGrainActor.Kind,
            prop: Props.FromProducer(() =>
                    new ReceiveGrainActor((context, clusterIdentity) => new ReceiverGrain(context, clusterIdentity)))
                .WithClusterRequestDeduplication(TimeSpan.FromSeconds(10)));

    var actorSystem = new ActorSystem(actorSystemConfig)
        .WithRemote(remoteConfig)
        .WithCluster(clusterConfig);

    return actorSystem;
}