using Confluent.Kafka;

namespace InventoryProducer.Services
{
    public class ProducerService
    {
        private readonly IProducer<Null, string> _producer;
        private readonly ILogger<ProducerService> _logger;

        public ProducerService(IConfiguration configuration, ILogger<ProducerService> logger)
        {
            _logger = logger;

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
            };

            _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
        }

        public async Task ProduceAsync(string topic, string method, int statusCode)
        {
            try
            {
                var message = $"type:response, @timestamp:{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}, tags:[], pid:1, method:{method}, statusCode:{statusCode}";

                var kafkaMessage = new Message<Null, string> { Value = message };
                var deliveryResult = await _producer.ProduceAsync(topic, kafkaMessage);

                _logger.LogInformation($"Message '{message}' delivered to '{deliveryResult.TopicPartitionOffset}'.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error while producing message: {ex.Message}");
            }

           
        }
    }
}