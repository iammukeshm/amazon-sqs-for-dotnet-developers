using Amazon.SQS;
using Amazon.SQS.Model;
using Messages;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Producer.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly ILogger<OrdersController> _logger;
        private const string OrderCreatedEventQueueName = "order-created";

        public OrdersController(IAmazonSQS sqsClient, ILogger<OrdersController> logger)
        {
            _sqsClient = sqsClient;
            _logger = logger;
        }
        [HttpPost]
        public async Task<IActionResult> CreateOrderAsync()
        {
            //assume that this method accepts a request with valid json body like customerId,itemDetails and so on.
            //order details validation
            //save incoming order to database
            //create event
            var @event = new OrderCreatedEvent(Guid.NewGuid(), Guid.NewGuid());
            //send message to queue
            var queueUrl = await GetQueueUrl(OrderCreatedEventQueueName);
            var sendMessageRequest = new SendMessageRequest()
            {
                QueueUrl = queueUrl,
                MessageBody = JsonSerializer.Serialize(@event)
            };
            _logger.LogInformation("Publishing message to Queue {queueName} with body : \n {request}", OrderCreatedEventQueueName, sendMessageRequest.MessageBody);
            var result = await _sqsClient.SendMessageAsync(sendMessageRequest);
            return Ok(result);
        }

        private async Task<string> GetQueueUrl(string queueName)
        {
            try
            {
                var response = await _sqsClient.GetQueueUrlAsync(queueName);
                return response.QueueUrl;
            }
            catch (QueueDoesNotExistException)
            {
                _logger.LogInformation("Queue {queueName} doesn't exist. Creating...", queueName);
                var response = await _sqsClient.CreateQueueAsync(queueName);
                return response.QueueUrl;
            }
        }
    }
}
