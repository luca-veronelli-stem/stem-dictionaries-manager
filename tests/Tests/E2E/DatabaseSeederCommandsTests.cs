using Infrastructure;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Tests.Integration;

namespace Tests.E2E;

/// <summary>
/// Seeder coverage for protocol reply commands (see #79).
/// The legacy DatabaseSeederTests suite is fully commented out, so reply-command
/// regressions live here as active tests.
/// </summary>
public class DatabaseSeederCommandsTests : IntegrationTestBase
{
    [Fact]
    public async Task SeedAsync_StartCalibrazioneImuReply_IsSeededAs0x802CWithEsitoParam()
    {
        await DatabaseSeeder.SeedAsync(Context);

        CommandEntity reply = await Context.Commands
            .SingleAsync(c => c.CodeHigh == 0x80 && c.CodeLow == 0x2C);

        Assert.Equal("Start Calibrazione IMU risposta", reply.Name);
        Assert.True(reply.IsResponse);

        Assert.Single(reply.Parameters);
        Assert.StartsWith("1|Esito", reply.Parameters[0]);
    }
}
