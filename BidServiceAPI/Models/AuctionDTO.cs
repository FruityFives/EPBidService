namespace BidServiceAPI.Models
{
    public class AuctionDTO
    {
        public Guid AuctionId { get; set; }
        public string Status { get; set; }
        public decimal MinBid { get; set; }
        public decimal CurrentBid {get; set;}
    }
}
