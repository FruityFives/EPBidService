namespace BidServiceAPI.Models
{
    public class Bid
    {
        public Guid BidId { get; set; }
        public Guid AuctionId { get; set; }        // Reference til Auction
        public Guid UserId { get; set; }           // Reference til den bruger der byder
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
