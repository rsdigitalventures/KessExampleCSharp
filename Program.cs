using Newtonsoft.Json;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace KessExampleCSharp
{
    class Program
    {
        static KessSettings Settings = new KessSettings
        {
            ApiSecretKey = "<YOUR SECRET KEY>",
            ClientId = "<YOUR CLIENT ID>",
            ClientSecret = "<YOUR CLIENT SECRET>",
            Username = "<YOUR USERNAME>",
            Password = "<YOUR PASSWORD>",
            SellerCode = "<YOUR SELLER CODE>",
            WebPayApiUrl = "https://devwebpayment.kesspay.io"
        };

        static async Task Main(string[] args)
        {
            //set up the api and client and set basic auth header
            var authHeader = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes($"{Settings.Username}:{Settings.Password}"));
            var apiClient = RestService.For<IWebPayApi>(Settings.WebPayApiUrl, new RefitSettings
            {
                AuthorizationHeaderValueGetter = () => Task.FromResult(authHeader)
            });

            //construct order request
            var createOrderRequest = new CreateOrderRequest
            {
                SellerCode = Settings.SellerCode, //958689692346
                OutTradeNo = "TR5673455626",
                TotalAmount = 16.5,
                Currency = "USD",
                Detail = new List<OrderDetail>
                {
                    new OrderDetail
                    {
                        No = "03232",
                        Name = "OLAY 77",
                        Price = (10.05).ToString(),
                        Qty = "2",
                        Discount = 0.05
                    }
                }
            };

            //sign order request
            createOrderRequest = SignObject(createOrderRequest);

            //call api
            var apiResponse = await apiClient.CreateOrder(createOrderRequest);

            if (apiResponse.Success == "true")
            {
                Console.WriteLine($"Order created successfully: {apiResponse.Data.PaymentLink}");
            }
            
            Console.WriteLine("Complete. Press <Enter> to exit...");
            Console.ReadLine();
        }

        static bool IsCollection(object obj)
        {
            return obj is System.Collections.ICollection;
        }

        static string CreateMd5(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert to hex string and lowercase it
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString().ToLower();
            }
        }

        static string MakeSign(object obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            var valuesSortedByKey = values
                .Where(e => e.Key != "sign" && !string.IsNullOrWhiteSpace(e.Key) && !e.Value.GetType().IsArray && !IsCollection(e.Value))
                .OrderBy(e => e.Key);

            StringBuilder sb = new StringBuilder();
            foreach (var item in valuesSortedByKey)
            {
                sb.Append($"{item.Key}={item.Value}&");
            }

            var output = sb.ToString().Trim('&');
            output = output + $"&key={Settings.ApiSecretKey}";

            var md5 = CreateMd5(output);
            return md5;
        }

        static T SignObject<T>(T obj) where T : ISignableObject
        {
            var signature = MakeSign(obj);
            obj.Sign = signature;
            return obj;
        }
    }

    [Headers("Authorization: Basic")]
    public interface IWebPayApi
    {
        /// <summary>
        /// Use this service to create a preorder for your seller and deliver the payment link to a buyer to process the payment.
        /// </summary>
        /// <param name="createOrderRequest"></param>
        /// <returns></returns>
        [Post("/api/mch/v1/gateway")]
        Task<KessApiResponse<OrderData>> CreateOrder(CreateOrderRequest createOrderRequest);

        /// <summary>
        /// Use this service to check your order’s ulayment status. We used three simulle status such as WAITING, SUCCESS, CLOSED.
        /// WAITING: after created preorder.
        /// SUCCESS: buyer paid for the order.
        /// CLOSED: order is closed without payment.
        /// </summary>
        /// <param name="queryOrderRequest"></param>
        /// <returns></returns>
        [Post("/api/mch/v1/gateway")]
        Task<KessApiResponse<OrderData>> QueryOrder(QueryOrderRequest queryOrderRequest);
    }

    public class QueryOrderRequest : ISignableObject
    {
        /// <summary>
        /// Gateway service name.
        /// </summary>
        [JsonProperty("service")]
        public string Service { get; } = "webpay.acquire.queryorder";

        /// <summary>
        /// Generated signature based on sign_type and API secret key.
        /// </summary>
        [JsonProperty("sign")]
        public string Sign { get; set; }

        /// <summary>
        /// Encrypt type is used to encrypt params. Ex: MD5 or HMAC-SHA256)
        /// </summary>
        [JsonProperty("sign_type")]
        public string SignType { get; } = "MD5";


        /// <summary>
        /// Unique order ID.
        /// </summary>
        [JsonProperty("out_trade_no")]
        public string OutTradeNo { get; set; }
    }

    public class OrderDetail
    {
        /// <summary>
        /// Product ID.
        /// </summary>
        [JsonProperty("no")]
        public string No { get; set; }

        /// <summary>
        /// Product Name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Unit price
        /// </summary>
        [JsonProperty("price")]
        public string Price { get; set; }

        /// <summary>
        /// Unit quantity
        /// </summary>
        [JsonProperty("qty")]
        public string Qty { get; set; }

        /// <summary>
        /// Unit discount
        /// </summary>
        [JsonProperty("discount")]
        public double Discount { get; set; }
    }

    public class KessApiResponse<T>
    {

        [JsonProperty("success")]
        public string Success { get; set; }

        [JsonProperty("data")]
        public T Data { get; set; }
    }

    public class OrderData
    {

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("mch_id")]
        public string MchId { get; set; }

        [JsonProperty("user_id")]
        public string UserId { get; set; }

        [JsonProperty("out_trade_no")]
        public string OutTradeNo { get; set; }

        [JsonProperty("transaction_id")]
        public object TransactionId { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("total_amount")]
        public string TotalAmount { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("seller_id")]
        public string SellerId { get; set; }

        [JsonProperty("notify_url")]
        public string NotifyUrl { get; set; }

        [JsonProperty("detail")]
        public IList<OrderDetail> Detail { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("payment_link")]
        public string PaymentLink { get; set; }
    }

    public interface ISignableObject
    {
        public string SignType { get; }

        public string Sign { get; set; }
    }

    public class CreateOrderRequest : ISignableObject
    {
        /// <summary>
        /// Gateway service name.
        /// </summary>
        [JsonProperty("service")]
        public string Service { get; } = "webpay.acquire.createorder";

        /// <summary>
        /// Generated signature based on sign_type and API secret key.
        /// </summary>
        [JsonProperty("sign")]
        public string Sign { get; set; }

        /// <summary>
        /// Encrypt type is used to encrypt params. Ex: MD5 or HMAC-SHA256)
        /// </summary>
        [JsonProperty("sign_type")]
        public string SignType { get; } = "MD5";

        [JsonProperty("seller_code")]
        public string SellerCode { get; set; }

        /// <summary>
        /// Unique order ID.
        /// </summary>
        [JsonProperty("out_trade_no")]
        public string OutTradeNo { get; set; }

        /// <summary>
        /// Order title.
        /// </summary>
        [JsonProperty("body")]
        public string Body { get; set; }

        /// <summary>
        /// Total amount with two decimal.
        /// </summary>
        [JsonProperty("total_amount")]
        public double TotalAmount { get; set; }

        /// <summary>
        /// Currency code. Ex: USD or KHR.
        /// </summary>
        [JsonProperty("currency")]
        public string Currency { get; set; }

        /// <summary>
        /// Product detail.
        /// </summary>
        [JsonProperty("detail")]
        public IList<OrderDetail> Detail { get; set; }
    }

    public class KessSettings
    {
        public string Username { get; set; }

        public string Password { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string ApiSecretKey { get; set; }

        public string SellerCode { get; set; }

        public string WebPayApiUrl { get; set; }
    }
}
