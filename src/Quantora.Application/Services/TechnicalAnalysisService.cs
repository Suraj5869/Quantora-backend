using Quantora.Application.DTOs.MarketData;

namespace Quantora.Application.Services;

public sealed class TechnicalAnalysisResponse
{
    public string InstrumentKey { get; init; } = string.Empty;
    public int CandleCount { get; init; }
    public DateTimeOffset CalculatedAt { get; init; }
    public decimal? Sma20 { get; init; }
    public decimal? Sma50 { get; init; }
    public decimal? Ema20 { get; init; }
    public decimal? Rsi14 { get; init; }
    public decimal? Macd { get; init; }
    public decimal? MacdSignal { get; init; }
    public decimal? Atr14 { get; init; }
    public decimal? AtrPercent { get; init; }
    public decimal? AverageVolume20 { get; init; }
    public string Trend { get; init; } = "Insufficient data";
    public string Momentum { get; init; } = "Insufficient data";
    public string Volatility { get; init; } = "Insufficient data";
}

public interface ITechnicalAnalysisService
{
    TechnicalAnalysisResponse Analyze(string instrumentKey, IReadOnlyList<MarketCandleDto> candles);
}

/// <summary>Pure indicator calculations with no broker or database dependencies.</summary>
public sealed class TechnicalAnalysisService : ITechnicalAnalysisService
{
    public TechnicalAnalysisResponse Analyze(string instrumentKey, IReadOnlyList<MarketCandleDto> candles)
    {
        ArgumentNullException.ThrowIfNull(candles);
        var data = candles.OrderBy(c => c.Timestamp).ToArray();
        var close = data.Select(c => c.Close).ToArray();
        var sma20 = Sma(close, 20);
        var sma50 = Sma(close, 50);
        var ema20 = Ema(close, 20);
        var ema12 = Ema(close, 12);
        var ema26 = Ema(close, 26);
        var macd = new decimal?[data.Length];
        for (var i = 0; i < data.Length; i++)
            if (ema12[i].HasValue && ema26[i].HasValue) macd[i] = ema12[i]!.Value - ema26[i]!.Value;
        var macdValues = macd.Select(x => x ?? 0m).ToArray();
        var signalRaw = Ema(macdValues, 9);
        var rsi = Rsi(close, 14);
        var atr = Atr(data, 14);
        var volumeAvg = Sma(data.Select(c => (decimal)c.Volume).ToArray(), 20);
        var last = data.Length - 1;
        decimal? lastClose = last >= 0 ? close[last] : null;
        decimal? lastMacd = last >= 0 ? macd[last] : null;
        decimal? lastSignal = last >= 33 && macd[last].HasValue ? signalRaw[last] : null;
        decimal? lastRsi = last >= 0 ? rsi[last] : null;
        decimal? lastAtr = last >= 0 ? atr[last] : null;
        decimal? atrPct = lastAtr.HasValue && lastClose > 0 ? lastAtr.Value / lastClose!.Value * 100m : null;
        var s20 = last >= 0 ? sma20[last] : null;
        var s50 = last >= 0 ? sma50[last] : null;
        return new TechnicalAnalysisResponse
        {
            InstrumentKey = instrumentKey, CandleCount = data.Length, CalculatedAt = DateTimeOffset.UtcNow,
            Sma20 = s20, Sma50 = s50, Ema20 = last >= 0 ? ema20[last] : null, Rsi14 = lastRsi,
            Macd = lastMacd, MacdSignal = lastSignal, Atr14 = lastAtr, AtrPercent = atrPct,
            AverageVolume20 = last >= 0 ? volumeAvg[last] : null,
            Trend = !lastClose.HasValue || !s20.HasValue ? "Insufficient data" :
                s50.HasValue && lastClose > s20 && s20 > s50 ? "Bullish" :
                s50.HasValue && lastClose < s20 && s20 < s50 ? "Bearish" :
                lastClose > s20 ? "Price above SMA20" : lastClose < s20 ? "Price below SMA20" : "Sideways",
            Momentum = !lastRsi.HasValue ? "Insufficient data" : lastRsi >= 70 ? "Overbought" :
                lastRsi <= 30 ? "Oversold" : lastMacd.HasValue && lastSignal.HasValue && lastMacd > lastSignal ? "Positive" :
                lastMacd.HasValue && lastSignal.HasValue && lastMacd < lastSignal ? "Negative" : "Neutral",
            Volatility = !atrPct.HasValue ? "Insufficient data" : atrPct < 1m ? "Low" : atrPct < 2.5m ? "Moderate" : "High"
        };
    }

    private static decimal?[] Sma(decimal[] values, int period)
    {
        var result = new decimal?[values.Length]; decimal sum = 0;
        for (var i = 0; i < values.Length; i++)
        {
            sum += values[i]; if (i >= period) sum -= values[i - period];
            if (i >= period - 1) result[i] = sum / period;
        }
        return result;
    }

    private static decimal?[] Ema(decimal[] values, int period)
    {
        var result = new decimal?[values.Length]; if (values.Length < period) return result;
        decimal seed = 0; for (var i = 0; i < period; i++) seed += values[i];
        var current = seed / period; result[period - 1] = current; var alpha = 2m / (period + 1);
        for (var i = period; i < values.Length; i++) { current += (values[i] - current) * alpha; result[i] = current; }
        return result;
    }

    private static decimal?[] Rsi(decimal[] values, int period)
    {
        var result = new decimal?[values.Length]; if (values.Length <= period) return result;
        decimal gain = 0, loss = 0;
        for (var i = 1; i <= period; i++) { var d = values[i] - values[i - 1]; if (d > 0) gain += d; else loss -= d; }
        gain /= period; loss /= period; result[period] = RsiValue(gain, loss);
        for (var i = period + 1; i < values.Length; i++)
        {
            var d = values[i] - values[i - 1]; gain = (gain * (period - 1) + Math.Max(d, 0m)) / period;
            loss = (loss * (period - 1) + Math.Max(-d, 0m)) / period; result[i] = RsiValue(gain, loss);
        }
        return result;
    }

    private static decimal RsiValue(decimal gain, decimal loss) =>
        loss == 0 ? (gain == 0 ? 50m : 100m) : 100m - 100m / (1m + gain / loss);

    private static decimal?[] Atr(IReadOnlyList<MarketCandleDto> data, int period)
    {
        var result = new decimal?[data.Count]; if (data.Count < period) return result;
        var ranges = new decimal[data.Count];
        for (var i = 0; i < data.Count; i++)
        {
            var previous = i == 0 ? data[i].Close : data[i - 1].Close;
            ranges[i] = Math.Max(data[i].High - data[i].Low,
                Math.Max(Math.Abs(data[i].High - previous), Math.Abs(data[i].Low - previous)));
        }
        var current = ranges.Take(period).Sum() / period; result[period - 1] = current;
        for (var i = period; i < ranges.Length; i++) { current = (current * (period - 1) + ranges[i]) / period; result[i] = current; }
        return result;
    }
}
