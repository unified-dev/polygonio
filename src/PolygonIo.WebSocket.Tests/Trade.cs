﻿using PolygonIo.WebSocket.Contracts;
using System;

namespace PolygonIo.WebSocket.Tests
{
    class Trade : ITrade
    {
        public int ExchangeId { get; set; }
        public decimal Price { get; set; }
        public string Symbol { get; set; }
        public int Tape { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public TradeCondition[] TradeConditions { get; set; }
        public string TradeId { get; set; }
        public decimal TradeSize { get; set; }
    }
}