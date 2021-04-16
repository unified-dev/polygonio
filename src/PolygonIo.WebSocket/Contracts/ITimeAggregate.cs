using System;

namespace PolygonIo.WebSocket.Contracts
{
    public interface ITimeAggregate
    {
        decimal Close { get; set; }
        DateTimeOffset EndDateTime { get; set; }
        decimal High { get; set; }
        decimal Low { get; set; }
        decimal Open { get; set; }
        DateTimeOffset StartDateTime { get; set; }
        decimal Volume { get; set; }
        string Symbol { get; set; }
        decimal AverageTradeSize { get; set; }
        decimal TodaysVolumeWeightedAveragePrice { get; set; }
        decimal TicksVolumeWeightedAveragePrice { get; set; }
        decimal TodaysOpeningPrice { get; set; }        
    }
}