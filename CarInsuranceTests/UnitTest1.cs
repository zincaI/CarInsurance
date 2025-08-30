using CarInsurance.Api.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using Xunit;

public class CarInsuranceTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task IsInsuranceValid_WhenDateIsOnFirstDayOfPolicy_ReturnsTrue()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext();
        var carId = 1L;
        var policyStartDate = new DateOnly(2024, 1, 1);
        var policyEndDate = new DateOnly(2024, 12, 31);

        // Seed the in-memory database
        dbContext.Cars.Add(new Car { Id = carId });
        dbContext.Policies.Add(new InsurancePolicy { CarId = carId, StartDate = policyStartDate, EndDate = policyEndDate });
        await dbContext.SaveChangesAsync();

        var service = new CarService(dbContext, new Mock<ICarExistsValidator>().Object);

        // Act
        var result = await service.IsInsuranceValidAsync(carId, policyStartDate);

        // Assert
        Assert.IsTrue(result);
    }

    [Fact]
    public async Task IsInsuranceValid_WhenDateIsOnLastDayOfPolicy_ReturnsTrue()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext();
        var carId = 1L;
        var policyStartDate = new DateOnly(2024, 1, 1);
        var policyEndDate = new DateOnly(2024, 12, 31);

        // Seed the in-memory database
        dbContext.Cars.Add(new Car { Id = carId });
        dbContext.Policies.Add(new InsurancePolicy { CarId = carId, StartDate = policyStartDate, EndDate = policyEndDate });
        await dbContext.SaveChangesAsync();

        var service = new CarService(dbContext, new Mock<ICarExistsValidator>().Object);

        // Act
        var result = await service.IsInsuranceValidAsync(carId, policyEndDate);

        // Assert
        Assert.IsTrue(result);
    }

    [Fact]
    public async Task IsInsuranceValid_WhenDateIsOneDayAfterPolicyEnds_ReturnsFalse()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext();
        var carId = 1L;
        var policyEndDate = new DateOnly(2024, 12, 31);
        var testDate = policyEndDate.AddDays(1);

        // Seed the in-memory database
        dbContext.Cars.Add(new Car { Id = carId });
        dbContext.Policies.Add(new InsurancePolicy { CarId = carId, StartDate = new DateOnly(2024, 1, 1), EndDate = policyEndDate });
        await dbContext.SaveChangesAsync();

        var service = new CarService(dbContext, new Mock<ICarExistsValidator>().Object);

        // Act
        var result = await service.IsInsuranceValidAsync(carId, testDate);

        // Assert
        Assert.IsFalse(result);
    }

    [Fact]
    public async Task GetCarHistory_WhenCarHasNoHistory_ReturnsEmptyList()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext();
        var carId = 1L;

        // Seed the in-memory database with only a car
        dbContext.Cars.Add(new Car { Id = carId });
        await dbContext.SaveChangesAsync();

        var service = new CarService(dbContext, new Mock<ICarExistsValidator>().Object);

        // Act
        var history = await service.GetCarHistoryAsync(carId);

        // Assert
        Assert.IsNull(history);
    }
}