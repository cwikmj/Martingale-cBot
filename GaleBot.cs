using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class GaleBot : Robot
    {
        [Parameter("First Order Size", Group = "Setup", DefaultValue = 0.1, MinValue = 0.05, Step = 0.05)]
        public double FirstVol { get; set; }

        [Parameter("Accepted Loss", Group = "Setup", DefaultValue = 15, MinValue = 5)]
        public double StopL { get; set; }

        [Parameter("Target TP", Group = "Setup", DefaultValue = 20, MinValue = 5)]
        public double TakeP { get; set; }

        [Parameter("Volume Multiplier", Group = "Risk", DefaultValue = 2, MinValue = 1.5, MaxValue = 3.0)]
        public double Multiplier { get; set; }

        [Parameter("Max % Drawdown", Group = "Risk", DefaultValue = 25, MinValue = 1, Step = 1)]
        public double MaxDrawdown { get; set; }

        private string MartinBot;
        private double positionSize = 0;
        private bool _startCount;
        private int _barCount = 0;
        private McGinley _mcg;

        protected override void OnStart()
        {
            MartinBot = this.GetType().Name;
            _mcg = Indicators.GetIndicator<McGinley>(34);
        }

        protected override void OnError(Error error)
        {
            Print("Error occured, error code: ", error.Code);
        }

        protected override void OnBar()
        {
            if (Positions.Count(x => x.Label == MartinBot) > 0)
            {
                // EQUITY PROTECTION - CLOSE ALL DRAWDOWN REACHED
                double dd = Account.Balance - Account.Equity;
                double max = Account.Balance * (MaxDrawdown / 100);
                if (dd > max)
                {
                    foreach (var position in Positions)
                    {
                        if (position.Label == MartinBot)
                            ClosePosition(position);
                    }
                }
            }

            // DAY OF THE WEEK CHECK
            bool trade = true;
            var tradeDay = Bars.OpenTimes.Last(1).DayOfWeek;
            if ((tradeDay == DayOfWeek.Friday && Bars.OpenTimes.LastValue.TimeOfDay.TotalHours > 9) || (tradeDay == DayOfWeek.Monday && Bars.OpenTimes.LastValue.TimeOfDay.TotalHours < 10) || tradeDay == DayOfWeek.Sunday)
            {
                trade = false;
            }
            // SPIKES CHECK
            bool nospikes = true;
            if (!(Bars.ClosePrices.Last(1) > Bars.OpenPrices.Last(1) && Bars.ClosePrices.Last(2) > Bars.OpenPrices.Last(2) && Bars.ClosePrices.Last(3) > Bars.OpenPrices.Last(3)) && !(Bars.ClosePrices.Last(1) < Bars.OpenPrices.Last(1) && Bars.ClosePrices.Last(2) < Bars.OpenPrices.Last(2) && Bars.ClosePrices.Last(3) < Bars.OpenPrices.Last(3)))
            {
                // last candle bigger than previous two
                if (Math.Abs(Bars.HighPrices.Last(1) - Bars.LowPrices.Last(1)) / Math.Abs(Bars.HighPrices.Last(2) - Bars.LowPrices.Last(2)) > 1.75 && Math.Abs(Bars.HighPrices.Last(1) - Bars.LowPrices.Last(1)) / Math.Abs(Bars.HighPrices.Last(3) - Bars.LowPrices.Last(3)) > 1.35)
                {
                    // hammer exception
                    if ((Math.Abs(Bars.ClosePrices.Last(1) - Bars.OpenPrices.Last(1)) / (Math.Abs(Bars.HighPrices.Last(1) - Bars.LowPrices.Last(1))) > 0.25))
                    {
                        nospikes = false;
                    }
                }
            }

            // BAR CHECK TO MAKE SLACK BETWEEN TRADES OPENINGS
            if (_barCount > 1)
            {
                _barCount = 0;
                _startCount = false;
            }
            if (_startCount)
            {
                _barCount++;
            }

            // CHECK BAR & SPIKES & DAY OF THE WEEK
            if (trade && nospikes && _barCount == 0)
            {
                ProcessTrades();
            }
        }

        private void ProcessTrades()
        {
            ///////////////
            // SIGNAL SETUP
            bool buy = false;
            bool sell = false;

            if (_mcg.mcginley.Last(1) > _mcg.mcginley.Last(2) && Bars.ClosePrices.Last(1) > Bars.OpenPrices.Last(1) && Bars.ClosePrices.Last(1) > _mcg.mcginley.Last(1))
                buy = true;
            if (_mcg.mcginley.Last(1) < _mcg.mcginley.Last(2) && Bars.ClosePrices.Last(1) < Bars.OpenPrices.Last(1) && Bars.ClosePrices.Last(1) < _mcg.mcginley.Last(1))
                sell = true;

            ///////////////
            // FIRST TRADE            
            if (History.Count(x => x.Label == MartinBot) == 0)
            {
                positionSize = Symbol.QuantityToVolumeInUnits(FirstVol);
                if (buy)
                {
                    ExecuteMarketOrder(TradeType.Buy, SymbolName, positionSize, MartinBot, StopL, TakeP);
                    _startCount = true;
                }
                if (sell)
                {
                    ExecuteMarketOrder(TradeType.Sell, SymbolName, positionSize, MartinBot, StopL, TakeP);
                    _startCount = true;
                }
            }

            else
            {

                ///////////////
                // NEXT TRADES
                if (Positions.Count(x => x.Label == MartinBot) == 0)
                {
                    HistoricalTrade lastClosed = History.FindLast(MartinBot, SymbolName);
                    if (lastClosed.NetProfit > 0)
                        positionSize = Symbol.QuantityToVolumeInUnits(FirstVol);
                    if (lastClosed.NetProfit < 0)
                        positionSize = Symbol.NormalizeVolumeInUnits(lastClosed.VolumeInUnits * Multiplier);

                    if (buy)
                    {
                        ExecuteMarketOrder(TradeType.Buy, SymbolName, positionSize, MartinBot, StopL, TakeP);
                        _startCount = true;
                    }
                    if (sell)
                    {
                        ExecuteMarketOrder(TradeType.Sell, SymbolName, positionSize, MartinBot, StopL, TakeP);
                        _startCount = true;
                    }
                }
            }
        }

        protected override void OnStop()
        {
            if (this.IsBacktesting)
            {
                foreach (var position in Positions)
                {
                    if (position.Label == MartinBot)
                        ClosePosition(position);
                }
            }
        }
    }
}
