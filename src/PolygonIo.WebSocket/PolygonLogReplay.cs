using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.Configuration;
using System.Threading;
using PolygonIo.WebSocket.Deserializers;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Buffers;

namespace PolygonIo.WebSocket
{
    public class PolygonLogReplay
    {
        private readonly ILogger<PolygonLogReplay> logger;
        private readonly DeserializerQueue deserializerQueue;
        private long count = 0;
        private readonly int pauseEveryFrameCount;
        private readonly int pauseDurationInMilliseconds;

        public PolygonLogReplay(IConfiguration configuration, DeserializerQueue deserializerQueue, ILogger<PolygonLogReplay> logger)
        {
            this.logger = logger;
            this.deserializerQueue = deserializerQueue;
            var section = configuration.GetSection("PolygonStream");
            this.pauseEveryFrameCount = section.GetValue<int>("PauseEveryFrameCount", 5000);
            this.pauseDurationInMilliseconds = section.GetValue<int>("PauseLengthInMilliseconds", 200);
        }

        public async Task ReplayFile(string file, CancellationToken stoppingToken, Func<DeserializedData, Task> receiveMessages)
        {
            this.deserializerQueue.Start(receiveMessages, false);
            long count = 0;
            using var reader = new StreamReader(file);
            string line;
            while (stoppingToken.IsCancellationRequested == false && (line = ReadFrameFromFileStream(reader)) != null)
            {
                if(count % this.pauseEveryFrameCount == 0)
                    await Task.Delay(this.pauseDurationInMilliseconds);
                
                count++;                
                var bytes = Encoding.UTF8.GetBytes(line);
                await this.deserializerQueue.AddFrame(new ReadOnlySequence<byte>(bytes));
            }
            await this.deserializerQueue.StopAsync();
        }

        enum State { LookingForOpenBracket, LookingForOpenBrace, ReadingContents, LookingForClosingBracketOrComma }

        string ReadFrameFromFileStream(StreamReader sr)
        {
            var state = State.LookingForOpenBracket;

            char nextChar;
            var line = new StringBuilder();

            if (sr.Peek() == -1)
                return null;

            while ((nextChar = (char)sr.Read()) > 0)
            {
                line.Append(nextChar);

                // state transitions                                           
                if (state == State.LookingForOpenBracket && nextChar == '[')
                {
                    state = State.LookingForOpenBrace;
                }
                else if (state == State.LookingForOpenBrace && nextChar == '{')
                {
                    state = State.ReadingContents;
                }
                else if (state == State.ReadingContents && nextChar == '}')
                {
                    state = State.LookingForClosingBracketOrComma;
                }
                else if (state == State.LookingForClosingBracketOrComma && nextChar == ',')
                {
                    state = State.LookingForOpenBrace;
                }
                else if (state == State.LookingForClosingBracketOrComma && nextChar == ']')
                {
                    count++;
                    if(count%100000==0)
                        this.logger.LogInformation($"PolygonFileReplay:ReadFrameFromFileStream() running total update {this.count:n0}");
                    
                    return line.ToString();
                }
                else if (state == State.ReadingContents && nextChar == '{')
                {
                    // reset
                    this.logger.LogError("framing error");
                    line.Clear();
                    state = State.LookingForOpenBracket;
                }
            }

            return null; 
        }
    }
}