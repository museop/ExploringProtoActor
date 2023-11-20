using ExploringProtoActor;
using Proto;
using Proto.Cluster;

namespace BroadcastActor;

public class ReceiverGrain : ReceiveGrainBase
{
    private readonly ClusterIdentity _clusterIdentity;
    private readonly string _filePath;

    public ReceiverGrain(IContext context, ClusterIdentity clusterIdentity) : base(context)
    {
        _clusterIdentity = clusterIdentity;
        _filePath = $"/Users/museop/Temp/{_clusterIdentity.Identity}.txt";
    }

    public override async Task Receive(Message request)
    {
        try
        {
            await File.AppendAllTextAsync(_filePath, request.Message_);
        }
        catch (Exception e)
        {
            Console.WriteLine($"error occurred: {e.Message}");
        }
    }

    public override Task Clear()
    {

        try
        {
            File.Delete(_filePath);
        }
        catch (Exception e)
        {
            Console.Write($"error occurred: {e.Message}");
        }

        return Task.CompletedTask;
    }
}