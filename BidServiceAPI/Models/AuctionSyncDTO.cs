using BidServiceAPI.Models;
using System;


namespace BidServiceAPI.Models
{
    public class AuctionSyncDTO
    {
        public Guid AuctionId { get; set; }
        public AuctionStatusSync Status { get; set; }
        public decimal MinBid { get; set; }
        public decimal CurrentBid { get; set; }
        public DateTime EndDate { get; set; }
    }

    public enum AuctionStatusSync
    {
        Inactive = 0,
        Active = 1,
        Closed = 2
    }
}
