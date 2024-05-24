using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using bookseller_api.Models;
using bookseller_api.user_models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Razorpay.Api;

namespace YOURNAMESPACE.Controllers
{
    public class RazorpayOrderResponse
    {
        public string Id { get; set; }
        public string Entity { get; set; }
        public int Amount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public string OrderId { get; set; }
        public string Description { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly string _apiKey = YOUR RAZOR_PAY_API_KEY;
        private readonly string _apiSecret = your RAZOR_PAY_API_SECRET;
        private readonly string _baseUrl = "Razorpay url";
        private readonly yourdb_context _dbcontext;




        private readonly IWebHostEnvironment _webHostEnvironment;

        public OrderController(bookseller_dbContext dbcontext, IWebHostEnvironment webHostEnvironment)
        {
            _dbcontext = dbcontext;
            _webHostEnvironment = webHostEnvironment;
        }

       [HttpPost]
[Route("create-order")]
public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest createOrderRequest)
{
    try
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_apiKey}:{_apiSecret}")));

        var orderRequest = new
        {
            amount = createOrderRequest.Amount, // Amount in the smallest unit (e.g., paise)
            currency = createOrderRequest.Currency,
            receipt = createOrderRequest.Receipt,
            payment_capture = createOrderRequest.PaymentCapture,
 notes = new
            {
                name = createOrderRequest.Name,
                email = createOrderRequest.Email,
                phone = createOrderRequest.Phone,
                address = createOrderRequest.Address,
                city = createOrderRequest.City,
                state = createOrderRequest.State,
                pin_code = createOrderRequest.PinCode
            }

        };

        var content = new StringContent(JsonConvert.SerializeObject(orderRequest), Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"{_baseUrl}orders", content);
        response.EnsureSuccessStatusCode();

        string responseBody = await response.Content.ReadAsStringAsync();
        var orderResponse = JsonConvert.DeserializeObject<RazorpayOrderResponse>(responseBody);

        return Ok(new { orderId = orderResponse.Id, gateway = "Razorpay" });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { error = ex.Message });
    }
}
[HttpPost]
[Route("capture-payment")]
public async Task<IActionResult> SaveResponse([FromBody] SaveResponse responseData)
{
    try
    {
        if (string.IsNullOrEmpty(responseData.OrderId) ||
            string.IsNullOrEmpty(responseData.PaymentId) ||
            string.IsNullOrEmpty(responseData.Signature) ||
            responseData.Amount <= 0 ||
            string.IsNullOrEmpty(responseData.Currency) ||
            string.IsNullOrEmpty(responseData.Status))
        {
            return BadRequest(new { error = "Invalid or missing data in the request" });
        }

        var table name = new SaveResponse
        {
            OrderId = responseData.OrderId,
            PaymentId = responseData.PaymentId,
            Signature = responseData.Signature,
            Amount = responseData.Amount,
            Currency = responseData.Currency,
            Status = responseData.Status,
            CreatedAt = DateTime.UtcNow
        };

        _dbcontext.tablename.Add(tablename);
        await _dbcontext.SaveChangesAsync();

        return Ok(new { message = "Response saved successfully" });
    }
    catch (DbUpdateException ex)
    {
        // Handle database exceptions and log the inner exception details
        var innerExceptionMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
        Console.WriteLine($"Database update error: {innerExceptionMessage}");
        return StatusCode(500, new { error = "Failed to save response to the database", details = innerExceptionMessage });
    }
    catch (Exception ex)
    {
        // Log the exception for further investigation
        var innerExceptionMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
        Console.WriteLine($"An error occurred: {innerExceptionMessage}");
        return StatusCode(500, new { error = "An unexpected error occurred", details = innerExceptionMessage });
    }
}




        [HttpPost]
        [Route("verify-signature")]
        public async Task<IActionResult> VerifySignature([FromBody] VerifySignatureRequest request)
        {
            try
            {
                // Fetch the specific save response from the database using the provided orderId and paymentId
                var saveResponse = await _dbcontext.SaveResponses
                    .FirstOrDefaultAsync(sr => sr.OrderId == request.OrderId && sr.PaymentId == request.PaymentId);

                if (saveResponse == null)
                {
                    return NotFound(new { message = "Payment record not found" });
                }

                string orderId = modelclassname.OrderId;
                string paymentId = modelclasname.PaymentId;
                string storedSignature = modelclasname.Signature;

                string payloadData = $"{orderId}|{paymentId}";
                string expectedSignature = CalculateSHA256(payloadData, _apiSecret);

                if (expectedSignature.Equals(request.Signature))
                {
                    return Ok(new { message = "Signature is valid" });
                }
                else
                {
                    return BadRequest(new { message = "Invalid signature" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private string CalculateSHA256(string text, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] textBytes = Encoding.UTF8.GetBytes(text);

            using (var hmacsha256 = new HMACSHA256(keyBytes))
            {
                byte[] hashBytes = hmacsha256.ComputeHash(textBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}

public class VerifySignatureRequest
{
    public string OrderId { get; set; }
    public string PaymentId { get; set; }
    public string Signature { get; set; }
}




public class CreateOrderRequest
{
    public int Amount { get; set; }
    public string Currency { get; set; }
    public string Receipt { get; set; }
    public int PaymentCapture { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string PinCode { get; set; }
}

