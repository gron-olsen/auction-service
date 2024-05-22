using StackExchange.Redis;
using auctionServiceAPI.Models;

namespace auctionServiceAPI.Services;

public class AuctionService
{
    private string connectionString = string.Empty;
    private ConnectionMultiplexer redisConnection;
    IDatabase AuctionDatabase;

    public AuctionService(IConfiguration configuration)
    {
        string connection = configuration["redisConnection"] ?? "localhost";
        redisConnection = ConnectionMultiplexer.Connect(connection);

        if (!redisConnection.IsConnected)
        {
            throw new Exception("no connection to redis");
        }
        AuctionDatabase = redisConnection.GetDatabase();
    }
    public int GetAuctionPrice(int id)
    {

        int bidPrice = (int)AuctionDatabase.StringGet(id.ToString());

        return bidPrice > 0 ? bidPrice : -1;
    }
    public bool AuctionExists(int id)
    {
        return (int?)AuctionDatabase.StringGet(id.ToString()) != null;
    }
    public bool SetAuctionPrice(int id, int bidPrice)
    {

        var checkAuctionExist = AuctionExists(id);
        if (checkAuctionExist)
        {
            AuctionDatabase.StringSet(id.ToString(), bidPrice);
        }
        return checkAuctionExist;
    }

    public bool AddToAuction(int id, int bidPrice, DateTime expireDate)
    {
        bool checkAuctionExist = AuctionExists(id);
        if (checkAuctionExist == false)
        {
            var expiryTimeSpan = expireDate.Subtract(DateTime.UtcNow);
            AuctionDatabase.StringSet(id.ToString(), bidPrice, expiryTimeSpan);
        }
        //Hvis auction allerede findes returnere den falsk, fordi den må ikke overskrive en auction.
        return !checkAuctionExist;
    }
}