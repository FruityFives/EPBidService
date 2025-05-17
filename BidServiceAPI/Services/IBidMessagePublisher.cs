using BidServiceAPI.Models;

public interface IBidMessagePublisher
{
    Task PublishBidAsync(Bid bid);
}

