using NATS.Client;
using NATS.Client.JetStream;

namespace TaskLogger.Infrastructure.Messaging;

public interface INatsConnection : IDisposable
{
    IConnection Connection { get; }
    IJetStream JetStream { get; }
    IJetStreamManagement JetStreamManagement { get; }
}

public class NatsConnection : INatsConnection
{
    private readonly IConnection _connection;
    private readonly IJetStream _jetStream;
    private readonly IJetStreamManagement _jsm;

    public NatsConnection(string natsUrl = "nats://localhost:4222")
    {
        var factory = new ConnectionFactory();
        _connection = factory.CreateConnection(natsUrl);
        _jetStream = _connection.CreateJetStreamContext();
        _jsm = _connection.CreateJetStreamManagementContext();
        
        InitializeStreams();
    }

    public IConnection Connection => _connection;
    public IJetStream JetStream => _jetStream;
    public IJetStreamManagement JetStreamManagement => _jsm;

    private void InitializeStreams()
    {
        try
        {
            // Stream untuk task queue
            var streamConfig = StreamConfiguration.Builder()
                .WithName("TASKS")
                .WithSubjects("tasks.>")
                .WithStorageType(StorageType.File)
                .WithRetentionPolicy(RetentionPolicy.WorkQueue)
                .Build();
            
            _jsm.AddStream(streamConfig);
        }
        catch (NATSJetStreamException ex) when (ex.ErrorCode == 400)
        {
            // Stream already exists
            Console.WriteLine("Stream TASKS already exists");
        }

        try
        {
            // Stream untuk logs
            var logStreamConfig = StreamConfiguration.Builder()
                .WithName("LOGS")
                .WithSubjects("logs.>")
                .WithStorageType(StorageType.File)
                .WithRetentionPolicy(RetentionPolicy.Limits)
                .WithMaxAge(TimeSpan.FromDays(7))
                .Build();
            
            _jsm.AddStream(logStreamConfig);
        }
        catch (NATSJetStreamException ex) when (ex.ErrorCode == 400)
        {
            Console.WriteLine("Stream LOGS already exists");
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}