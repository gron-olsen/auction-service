using StackExchange.Redis;
using Newtonsoft.Json;
using auctionServiceAPI.Models;

namespace auctionServiceAPI.Services;

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
            throw new Exception("no connection to redis");
        }
        RedisClient = redisConnection.GetDatabase();
    }
    public int GetAuctionPrice(int id)
    {

        int bidPrice = (int)RedisClient.StringGet(id.ToString());

        return bidPrice > 0 ? bidPrice : -1;
    }
    public bool AuctionExists(int id)
    {
        return (int?)RedisClient.StringGet(id.ToString()) != null;
    }
    public bool SetAuctionPrice(int id, int bidPrice)
    {

        var checkAuctionExist = AuctionExists(id);
        if (checkAuctionExist)
        {
            RedisClient.StringSet(id.ToString(), bidPrice);
        }
        return checkAuctionExist;
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
            ProductEndDate = expireDate
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

