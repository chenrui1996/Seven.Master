using FluentAssertions;
using Seven.Infrastructure.Configuration;
using Xunit;

namespace Seven.Tests.Wcs;

public class FeatureOptionsWcsTests
{
    [Fact]
    public void IsEnabled_ShouldRead_NestedPack_ViaFlatName_OrExplicit()
    {
        var f = new FeatureOptions
        {
            Wms = true,
            OrchestrationBus = true,
            WcsPacks = new WcsPackFeatureOptions { Stacker = true, FourWay = false }
        };
        f.Wms.Should().BeTrue();
        f.WcsPacks.Stacker.Should().BeTrue();
        f.WcsPacks.FourWay.Should().BeFalse();
    }

    [Fact]
    public void IsWcsPackEnabled_ShouldResolve_PackName()
    {
        var f = new FeatureOptions
        {
            WcsPacks = new WcsPackFeatureOptions { Stacker = true, FourWay = false }
        };
        f.IsWcsPackEnabled("Stacker").Should().BeTrue();
        f.IsWcsPackEnabled("FourWay").Should().BeFalse();
        f.IsWcsPackEnabled("Unknown").Should().BeFalse();
    }
}
