using System.Diagnostics;
using System.Reflection;
using ExploringProtoActor;
using Proto;
using Proto.Cluster;

namespace BroadcastActor;

public class BroadcastGrain : BroadcastGrainBase
{
    private readonly ClusterIdentity _clusterIdentity;
    private readonly List<string> _receivers = new();
    
    public BroadcastGrain(IContext context, ClusterIdentity clusterIdentity) : base(context)
    {
        _clusterIdentity = clusterIdentity;
    }

    public override Task SetReceivers(SetReceiverRequest request)
    {
        _receivers.Clear();
        for (var i = 0; i < request.Number; i++)
        {
            _receivers.Add($"receiver-{i}");
        }

        Console.WriteLine($"[{_clusterIdentity.Identity}] Set {request.Number} receivers");

        return Task.CompletedTask;
    }

    public override async Task Broadcast(BroadcastRequest request)
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        
        await Task.WhenAll(_receivers.Select(async receiver =>
        {
            await Cluster.GetReceiveGrain(receiver).Receive(new Message
            {
                Message_ = request.Message
            }, CancellationToken.None);
        }));
        
        stopWatch.Stop();
        Console.WriteLine($"[{_clusterIdentity.Identity}] Broadcast took {stopWatch.ElapsedMilliseconds} ms");
    }

    public override async Task Clear(ClearRequest request)
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        
        await Task.WhenAll(_receivers.Select(async receiver =>
        {
            await Cluster.GetReceiveGrain(receiver).Clear(CancellationToken.None);
        }));
        
        stopWatch.Stop();
        Console.WriteLine($"[{_clusterIdentity.Identity}] Clear took {stopWatch.ElapsedMilliseconds} ms");
    }
}