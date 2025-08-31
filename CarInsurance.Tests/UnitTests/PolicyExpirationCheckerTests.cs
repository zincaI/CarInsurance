using CarInsurance.Api.Data;
using CarInsurance.Api.Models;
using CarInsurance.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.Extensions.Hosting;

public class PolicyExpirationCheckerTests
{
    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new AppDbContext(options);
        dbContext.Database.EnsureCreated();
        return dbContext;
    }

    [Fact]
    public async Task ExecuteAsync_LogsExpiredPoliciesAndSetsLoggedFlag()
    {
        var dbContext = CreateDbContext();

        var car = new Car { Vin = "TEST-VIN", Make = "Test", Model = "Model", YearOfManufacture = 2020, OwnerId = 1 };
        dbContext.Cars.Add(car);
        await dbContext.SaveChangesAsync();

        var expiredPolicy = new InsurancePolicy
        {
            CarId = car.Id,
            Provider = "Test Provider",
            StartDate = new DateOnly(2023, 1, 1),
            EndDate = new DateOnly(2023, 12, 31),
            IsExpirationLogged = false
        };
        dbContext.Policies.Add(expiredPolicy);
        await dbContext.SaveChangesAsync();

        var mockLogger = new Mock<ILogger<PolicyExpirationChecker>>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        mockServiceProvider.Setup(s => s.GetService(typeof(AppDbContext))).Returns(dbContext);

        var mockScope = new Mock<IServiceScope>();
        mockScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);

        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

        var service = new PolicyExpirationChecker(mockLogger.Object, mockScopeFactory.Object);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(500));

        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await service.StopAsync(cts.Token);

        var updatedPolicy = await dbContext.Policies.FindAsync(expiredPolicy.Id);

        Assert.NotNull(updatedPolicy);
        Assert.True(updatedPolicy.IsExpirationLogged);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("has expired")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoExpiredPolicies_DoesNotLogAndDoesNotUpdateDatabase()
    {
        var dbContext = CreateDbContext();

        var car = new Car { Vin = "TEST-VIN", Make = "Test", Model = "Model", YearOfManufacture = 2024, OwnerId = 1 };
        dbContext.Cars.Add(car);
        await dbContext.SaveChangesAsync();

        var currentPolicy = new InsurancePolicy
        {
            CarId = car.Id,
            Provider = "Test Provider",
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 12, 31),
            IsExpirationLogged = false
        };
        dbContext.Policies.Add(currentPolicy);
        await dbContext.SaveChangesAsync();

        var mockLogger = new Mock<ILogger<PolicyExpirationChecker>>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        mockServiceProvider.Setup(s => s.GetService(typeof(AppDbContext))).Returns(dbContext);

        var mockScope = new Mock<IServiceScope>();
        mockScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);

        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

        var service = new PolicyExpirationChecker(mockLogger.Object, mockScopeFactory.Object);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        await service.StopAsync(cts.Token);

        var updatedPolicy = await dbContext.Policies.FindAsync(currentPolicy.Id);

        Assert.NotNull(updatedPolicy);
        Assert.False(updatedPolicy.IsExpirationLogged);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("has expired")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Never);
    }
}
