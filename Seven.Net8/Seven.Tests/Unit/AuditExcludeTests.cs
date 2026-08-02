using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Seven.Application.Interfaces;
using Seven.Domain.Entities.System;
using Seven.Infrastructure.Configuration;
using Seven.Infrastructure.Persistence;

namespace Seven.Tests.Unit;

public class AuditExcludeTests
{
    private sealed class FakeCurrentUser : ICurrentUserService
    {
        public int? UserId => 1;
        public string? UserName => "tester";
        public int? RoleId => 1;
    }

    // Minimal entity stand-in matching exclude name
    private sealed class TrafficOccupancy
    {
        public int Id { get; set; }
        public string NodeId { get; set; } = "";
    }

    private sealed class AuditTestDbContext : DbContext
    {
        public AuditTestDbContext(DbContextOptions<AuditTestDbContext> options) : base(options) { }
        public DbSet<Sys_Log> Logs => Set<Sys_Log>();
        public DbSet<TrafficOccupancy> Occupancies => Set<TrafficOccupancy>();
    }

    [Fact]
    public async Task AuditInterceptor_SkipsExcludedEntityNames()
    {
        var interceptor = new AuditSaveChangesInterceptor(
            new FakeCurrentUser(),
            Options.Create(new HotStoreOptions
            {
                AuditExcludeEntities = ["TrafficOccupancy"]
            }));

        var options = new DbContextOptionsBuilder<AuditTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var db = new AuditTestDbContext(options);
        db.Occupancies.Add(new TrafficOccupancy { NodeId = "N1" });
        await db.SaveChangesAsync();

        db.Logs.Should().BeEmpty();
    }
}
