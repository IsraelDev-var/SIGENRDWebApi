using SIGENRD.Core.Application.Features.ConnectionRequests.Commands.CreateConnectionRequest;

namespace SIGENRD.UnitTests.Application;

/// <summary>A28-05: el nombre del titular ya registrado se compara sin distinguir mayúsculas, tildes ni espacios.</summary>
public class HolderNamesTests
{
    [Theory]
    [InlineData("José Núñez Peña", "jose nunez pena")]
    [InlineData("José Núñez Peña", "  JOSÉ   Núñez\tPeña ")]
    [InlineData("Solar Caribe, S.R.L.", "SOLAR CARIBE, S.R.L.")]
    public void Coinciden(string registered, string typed) => Assert.True(HolderNames.Match(registered, typed));

    [Theory]
    [InlineData("José Núñez Peña", "José Núñez")]
    [InlineData("José Núñez Peña", "Josefa Núñez Peña")]
    [InlineData("Solar Caribe, S.R.L.", "Solar Caribe SRL")]
    public void No_coinciden(string registered, string typed) => Assert.False(HolderNames.Match(registered, typed));
}
