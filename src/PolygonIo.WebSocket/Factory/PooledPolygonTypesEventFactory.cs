namespace PolygonIo.WebSocket.Factory
{
    /*
    public class PooledPolygonTypesEventFactory : IPooledEventFactory
    {
        public PooledPolygonTypesEventFactory()
        {
            this.quotePool = new DefaultObjectPool<Quote>(new DefaultPooledObjectPolicy<Quote>());
            this.tradePool = new DefaultObjectPool<Trade>(new DefaultPooledObjectPolicy<Trade>());
            this.aggregatePool = new DefaultObjectPool<TimeAggregate>(new DefaultPooledObjectPolicy<TimeAggregate>());
            this.statusPool = new DefaultObjectPool<Status>(new DefaultPooledObjectPolicy<Status>());            
        }

        readonly ObjectPool<Quote> quotePool;
        readonly ObjectPool<Trade> tradePool;
        readonly ObjectPool<TimeAggregate> aggregatePool;
        readonly ObjectPool<Status> statusPool;
        
        public IQuote NewQuote() => this.quotePool.Get();
        public IStatus NewStatus() => this.statusPool.Get();
        public ITimeAggregate NewTimeAggregate() => this.aggregatePool.Get();
        public ITrade NewTrade() => this.tradePool.Get();
        public void ReturnQuote(object quote) => this.quotePool.Return((Quote)quote);
        public void ReturnTrade(object trade) => this.tradePool.Return((Trade)trade);
        public void ReturntimeAggregate(object aggregate) => this.aggregatePool.Return((TimeAggregate)aggregate);
        public void ReturnStatus(object status) => this.statusPool.Return((Status)status);
    }
    */
}