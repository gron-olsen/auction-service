using StackExchange.Redis;
using Newtonsoft.Json;
using auctionServiceAPI.Models;

namespace auctionServiceAPI.Services
{
    public class AuctionService
    {
        private string connectionString = string.Empty;
        private ConnectionMultiplexer redisConnection;
        IDatabase RedisClient;

        public AuctionService(IConfiguration configuration)
        {
            string connection = configuration["redisConnection"] ?? "localhost";
            redisConnection = ConnectionMultiplexer.Connect(connection);

            if (!redisConnection.IsConnected)
            {
                throw new Exception("No connection to Redis");
            }
            RedisClient = redisConnection.GetDatabase();
        }

        public int GetAuctionPrice(int id)
        {
            string jsonAuctionInfo = RedisClient.StringGet(id.ToString());
            if (string.IsNullOrEmpty(jsonAuctionInfo))
            {
                return -1; // Auction not found
            }

            var auctionProduct = JsonConvert.DeserializeObject<AuctionProduct>(jsonAuctionInfo);
            return auctionProduct.ProductPrice;
        }

        public bool AuctionExists(int id)
        {
            string jsonAuctionInfo = RedisClient.StringGet(id.ToString());
            return !string.IsNullOrEmpty(jsonAuctionInfo);
        }

        public bool UpdateAuctionPrice(int id, int bidPrice)
        {
            string jsonAuctionInfo = RedisClient.StringGet(id.ToString());
            if (string.IsNullOrEmpty(jsonAuctionInfo))
            {
                return false; // Auction not found
            }

            var auctionProduct = JsonConvert.DeserializeObject<AuctionProduct>(jsonAuctionInfo);
            auctionProduct.ProductPrice = bidPrice;
            jsonAuctionInfo = JsonConvert.SerializeObject(auctionProduct);
            RedisClient.StringSet(id.ToString(), jsonAuctionInfo);
            return true;
        }

        public bool AddToAuction(int id, int bidPrice, DateTime expireDate)
        {
            bool checkAuctionExist = AuctionExists(id);
            if (!checkAuctionExist)
            {
                var auctionProduct = new AuctionProduct
                {
                    ProductID = id,
                    ProductStartPrice = bidPrice,
                    ProductEndDate = expireDate,
                    ProductPrice = bidPrice // Set initial ProductPrice to start price
                };

                // Convert auctionInfo object to JSON
                string jsonAuctionInfo = JsonConvert.SerializeObject(auctionProduct);

                var expiryTimeSpan = expireDate.Subtract(DateTime.UtcNow);

                // Store JSON object in Redis
                RedisClient.StringSet(id.ToString(), jsonAuctionInfo, expiryTimeSpan);
            }
            return !checkAuctionExist;
        }

        public List<AuctionProduct> GetAllAuctions()
        {
            var auctions = new List<AuctionProduct>();

            foreach (var key in redisConnection.GetServer(redisConnection.GetEndPoints()[0]).Keys())
            {
                string jsonAuctionInfo = RedisClient.StringGet(key);
                var auctionProduct = JsonConvert.DeserializeObject<AuctionProduct>(jsonAuctionInfo);
                auctions.Add(auctionProduct);
            }

            return auctions;
        }
    }
}