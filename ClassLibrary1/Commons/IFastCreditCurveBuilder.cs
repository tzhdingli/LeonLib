namespace ClassLibrary1.Commons
{
    public interface IFastCreditCurveBuilder
    {
        PiecewiseconstantHazardRate calibrateCreditCurve(CDS[] cds, double[] premiums, YieldTermStructure yieldCurve, double[] pointsUpfront);
    }
}