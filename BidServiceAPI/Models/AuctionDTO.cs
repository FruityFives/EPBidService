using BidServiceAPI.Models;
using System;


namespace BidServiceAPI.Models
{
    public class AuctionDTO
    {
        public Guid AuctionId { get; set; }
        public AuctionStatus Status { get; set; }
        public decimal MinBid { get; set; }
        public decimal CurrentBid { get; set; }
        public DateTime EndDate { get; set; }
    }

    public enum AuctionStatus
    {
        Inactive = 0,
        Active = 1,
        Closed = 2
    }
}
