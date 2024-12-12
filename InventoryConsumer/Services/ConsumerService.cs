using Confluent.Kafka;

using OpenSearch.Client;

namespace InventoryConsumer.Services
{
    public class ConsumerService : BackgroundService
    {
        private readonly IConsumer<Ignore, string> _consumer;
        private readonly ILogger<ConsumerService> _logger;
        private readonly IOpenSearchClient _openSearchClient;

        public ConsumerService(IConfiguration configuration, ILogger<ConsumerService> logger, IOpenSearchClient openSearchClient)
        {
            _logger = logger;
            _openSearchClient = openSearchClient;

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                GroupId = "InventoryConsumerGroup",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            _consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
        }
    
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(() =>
            {
                _logger.LogInformation("ConsumerService is shutting down...");
                _consumer.Unsubscribe();
                _consumer.Close();
            });

            _consumer.Subscribe("inventory-updates");
            _logger.LogInformation("Subscribed to Kafka topic: inventory-updates");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);
                    var message = consumeResult.Message.Value;

                    _logger.LogInformation($"Received inventory update: {message}");

                    var response = await _openSearchClient.IndexAsync(new IndexRequest<string>("inventory-updates")
                    {
                        Document = message
                    }, stoppingToken);

                    if (response.IsValid)
                    {
                        _logger.LogInformation("Message successfully indexed to OpenSearch.");
                    }
                    else
                    {
                        _logger.LogError($"Failed to index message: {response.OriginalException.Message}");
                    }

                    _consumer.Commit(consumeResult);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Unexpected error: {ex.Message}");
                }
                
            }
        }
    }
}
