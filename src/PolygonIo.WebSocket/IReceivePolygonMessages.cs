using PolygonIo.Model.Contracts;
using System.Threading.Tasks;

namespace PolygonIo.WebSocket
{


    public interface IReceivePolygonMessages
    {
        Task OnTimeAggregatePerSecondReceivedAsync(ITimeAggregate timeAggregate);
        Task OnTimeAggregatePerMinuteReceivedAsync(ITimeAggregate timeAggregate);
        Task OnQuoteReceivedAsync(IQuote quote);
        Task OnTradeReceivedAsync(ITrade trade);
        Task OnStatusReceivedAsync(IStatus status);
    }
}