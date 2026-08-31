using FluentAssertions;
using Seven.Domain.Wcs;
using Seven.Domain.Wms;

namespace Seven.Tests.Wms;

public class LesMigrationCodesTests
{
    [Theory]
    [InlineData("ASRS01", "stacker", "Stk.Z-ASRS01")]
    [InlineData("Stk.Z-ASRS01", "stacker", "Stk.Z-ASRS01")]
    [InlineData("ASRS", "fourway", "Fw.Z-ASRS")]
    public void ToZoneCode_MatchesDesign(string les, string pack, string expected) =>
        LesMigrationCodes.ToZoneCode(les, pack).Should().Be(expected);

    [Theory]
    [InlineData("A01", "stacker", "Stk.A-A01")]
    [InlineData("L02-03", "fourway", "Fw.A-L02-03")]
    public void ToAisleCode_MatchesDesign(string les, string pack, string expected) =>
        LesMigrationCodes.ToAisleCode(les, pack).Should().Be(expected);

    [Theory]
    [InlineData("0102031", "stacker", "Stk.B-0102031")]
    [InlineData("1205", "fourway", "Fw.N-1205")]
    [InlineData("N1205", "fourway", "Fw.N-1205")]
    public void ToLocationCode_MatchesDesign(string les, string pack, string expected) =>
        LesMigrationCodes.ToLocationCode(les, pack).Should().Be(expected);

    [Theory]
    [InlineData("L2", "fourway", "Fw.L02")]
    [InlineData("2", "fourway", "Fw.L02")]
    [InlineData("L02", "fourway", "Fw.L02")]
    public void ToLayerCode_PadsNumeric(string les, string pack, string expected) =>
        LesMigrationCodes.ToLayerCode(les, pack).Should().Be(expected);

    [Fact]
    public void MapPackId_DenseCanSwitchToFourWay()
    {
        LesMigrationCodes.MapPackIdFromZoneType(10).Should().Be(WcsPackIds.Stacker);
        LesMigrationCodes.MapPackIdFromZoneType(40).Should().Be(WcsPackIds.Stacker);
        LesMigrationCodes.MapPackIdFromZoneType(30).Should().Be(WcsPackIds.Stacker);
        LesMigrationCodes.MapPackIdFromZoneType(30, WcsPackIds.FourWay).Should().Be(WcsPackIds.FourWay);
        LesMigrationCodes.MapPackIdFromZoneType(31, WcsPackIds.FourWay).Should().Be(WcsPackIds.FourWay);
    }

    [Fact]
    public void BuildEnabledPackIds_Dedupes()
    {
        LesMigrationCodes.BuildEnabledPackIds([10, 40, 30], WcsPackIds.FourWay)
            .Should().Be("stacker,fourway");
        LesMigrationCodes.BuildEnabledPackIds([10, 40]).Should().Be("stacker");
    }
}
