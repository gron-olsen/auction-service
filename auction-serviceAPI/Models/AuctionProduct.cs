namespace auctionServiceAPI.Models;

public class AuctionProduct
{
    public int ProductID { get; set; }
    public int ProductStartPrice  { get; set; }

    public int ProductPrice { get; set; }
    public DateTime ProductEndDate { get; set; }
}