
namespace PolygonIo.WebApi.Contracts
{
    public enum Timespan { Minute, Hour, Day, Week, Month, Quarter, Year }

    public enum SortTickersBy { Ticker, Name, Market, Locale, Currency, Active, PrimarExchange, Type }

    public enum Sort { asc, desc }

    public enum PrimaryExchange
    {
        XASE,
        XNAS,
        XNYS,
        ARCX,
        BATS
    }

    public enum TickerType
    {
        CS, // Common Stock
        BOND, // Bond
        BASKET, // Basket
        ADRC, // American Depository Receipt Common
        ADRP, // American Depository Receipt Preferred
        ADRW, // American Depository Receipt Warrant
        ADRR, // American Depository Receipt Right
        NVDR, // Non-Voting Depository Receipt
        GDR, // Global Depositary Receipt
        SDR, // Special Drawing Right
        CEF, // Closed-End Fund
        ETP, // Exchange Traded Product/Fund
        REIT, // Real Estate Investment Trust
        MLP, // Master Limited Partnership
        WRT, // Equity WRT
        PUB, // Public
        NYRS, // New York Registry Shares
        UNIT, // Unit
        RIGHT, // Right
        TRAK, // Tracking stock or targeted stock
        LTDP, // Limited Partnership
        RYLT, // Royalty Trust
        MF, // Mutual Fund
        PFD, // Preferred Stock
        FDR, // Foreign Ordinary Shares
        OST, // Other Security Type
        FUND, // Fund
        SP, // Structured Product
        SI, // Secondary Issue
        WARRANT, // Warrant"
        INDEX, // Index
        ETF, // Exchange Traded Fund (ETF)
        ETN, // Exchange Traded Note (ETN)
        ETMF, // Exchange Traded Managed Fund (ETMF)
        SETTLEMENT, // Settlement
        SPOT, // Spot
        SUBPROD, // Subordinated product
        WC, // World Currency
        ALPHAINDEX, // Alpha Index
    }

    public enum Market { Stocks, Indices, Crypto, FX, Bonds, MF, MMF }

    public enum Locale
    {
        G,
        US,
        GB,
        CA,
        NL,
        GR,
        SP,
        DE,
        BE,
        DK,
        FI,
        IE,
        PT,
        IN,
        MX,
        FR,
        CN,
        CH,
        SE
    }

    public enum CurrencyCode
    {
        AED,
        AFN,
        ALL,
        AMD,
        ARS,
        AUD,
        AWG,
        ANG,
        AZN,
        BAM,
        BBD,
        BDT,
        BGN,
        BHD,
        BIF,
        BND,
        BMD,
        BOB,
        BRL,
        BSD,
        BTC,
        BTN,
        BWP,
        BYR,
        BZD,
        CAD,
        CDF,
        CHF,
        CLP,
        CNY,
        CNH,
        COP,
        CRC,
        CUP,
        CVE,
        CYP,
        CZK,
        DKK,
        DOP,
        DJF,
        DZD,
        EEK,
        EGP,
        ETB,
        EUR,
        FRF,
        FIM,
        FJD,
        GBX,
        GBP,
        GEL,
        GHS,
        GNF,
        GYD,
        GMD,
        GTQ,
        HKD,
        HNL,
        HTG,
        HRK,
        HUF,
        IDR,
        ILS,
        INR,
        IQD,
        IRR,
        ISK,
        JMD,
        JOD,
        JPY,
        KES,
        KGS,
        KHR,
        KRW,
        KWD,
        KWF,
        KMF,
        KYD,
        KZT,
        LAK,
        LBP,
        LKR,
        LSL,
        LTL,
        LVL,
        LYD,
        LRD,
        MAD,
        MDL,
        MGA,
        MMK,
        MRO,
        MUR,
        MKD,
        MWK,
        MZN,
        MNT,
        MOP,
        MVR,
        MXN,
        MYR,
        NAD,
        NGN,
        NIO,
        NOK,
        NPR,
        NZD,
        OMR,
        PAB,
        PEN,
        PGK,
        PHP,
        PKR,
        PLN,
        PYG,
        QAR,
        RON,
        RSD,
        RUB,
        RWF,
        SCR,
        SDD,
        SDG,
        SAR,
        SEK,
        SGD,
        SHP,
        SIT,
        SLL,
        SKK,
        SOS,
        STD,
        STN,
        SVC,
        SYP,
        SZL,
        THB,
        TJS,
        TZS,
        TND,
        TRY,
        TTD,
        TWD,
        TMT,
        UAH,
        USD,
        UYU,
        UGX,
        UZS,
        VEF,
        VND,
        XOF,
        YER,
        ZAR,
        ZWL,
        ZMW,
        XCD,
        XPF
    }
}
