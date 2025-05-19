using Amazon.Lambda.SNSEvents;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using System.Text;
using System.Threading.Tasks;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PaymentReceiptLambda;

public class Function
{
    private readonly IAmazonS3 _s3 = new AmazonS3Client();
    private readonly IAmazonSimpleEmailService _ses = new AmazonSimpleEmailServiceClient();
    private const string BucketName = "Invoice-receipt-bucket";
    private const string FromEmail = "your-verified-email@domain.com";

    public async Task FunctionHandler(SNSEvent snsEvent, ILambdaContext context)
    {
        foreach (var record in snsEvent.Records)
        {
            var message = record.Sns.Message;
            var receiptId = Guid.NewGuid().ToString();
            var fileName = $"receipts/{receiptId}.txt";

            // Generate simple receipt content
            var receiptContent = $"Receipt ID: {receiptId}\nDetails: {message}";

            // Save to S3
            var putRequest = new PutObjectRequest
            {
                BucketName = BucketName,
                Key = fileName,
                ContentBody = receiptContent
            };
            await _s3.PutObjectAsync(putRequest);

            // Send via SES
            var emailRequest = new SendEmailRequest
            {
                Source = FromEmail,
                Destination = new Destination { ToAddresses = new List<string> { "customer@domain.com" } },
                Message = new Message
                {
                    Subject = new Content("Your Payment Receipt"),
                    Body = new Body
                    {
                        Text = new Content($"Thank you for your payment. Receipt ID: {receiptId}")
                    }
                }
            };
            await _ses.SendEmailAsync(emailRequest);

            context.Logger.LogInformation($"Receipt processed and emailed for payment: {receiptId}");
        }
    }
}
