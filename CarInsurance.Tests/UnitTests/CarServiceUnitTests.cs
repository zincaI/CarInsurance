
using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using CarInsurance.Api.Data;
using CarInsurance.Api.Models;
using CarInsurance.Api.Services;
using CarInsurance.Api.Validators;

namespace CarInsurance.Tests.UnitTests
{
    public class CarServiceTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private CarService CreateServiceWithValidator(AppDbContext dbContext, bool carExists = true)
        {
            var mockValidator = new Mock<ICarExistsValidator>();
            mockValidator.Setup(v => v.IsValid(It.IsAny<long>())).ReturnsAsync(carExists);
            return new CarService(dbContext, mockValidator.Object);
        }

        [Fact]
        public async Task IsInsuranceValid_WhenDateIsWithinPolicy_ReturnsTrue()
        {
            var dbContext = GetInMemoryDbContext();
            var carId = 1L;
            dbContext.Cars.Add(new Car { Id = carId, Vin = "VIN1" });
            dbContext.Policies.Add(new InsurancePolicy { CarId = carId, StartDate = new DateOnly(2024, 1, 1), EndDate = new DateOnly(2024, 12, 31) });
            await dbContext.SaveChangesAsync();

            var service = CreateServiceWithValidator(dbContext);

            var result = await service.IsInsuranceValidAsync(carId, new DateOnly(2024, 6, 1));

            Xunit.Assert.True(result);
        }

        [Fact]
        public async Task IsInsuranceValid_WhenDateIsOnLastDayOfPolicy_ReturnsTrue()
        {
            var dbContext = GetInMemoryDbContext();
            var carId = 1L;
            var policyEndDate = new DateOnly(2024, 12, 31);
            dbContext.Cars.Add(new Car { Id = carId, Vin = "VIN2" });
            dbContext.Policies.Add(new InsurancePolicy { CarId = carId, StartDate = new DateOnly(2024, 1, 1), EndDate = policyEndDate });
            await dbContext.SaveChangesAsync();

            var service = CreateServiceWithValidator(dbContext);

            var result = await service.IsInsuranceValidAsync(carId, policyEndDate);

            Xunit.Assert.True(result);
        }

        [Fact]
        public async Task IsInsuranceValid_WhenDateIsOneDayAfterPolicyEnds_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var carId = 1L;
            var policyEndDate = new DateOnly(2024, 12, 31);
            dbContext.Cars.Add(new Car { Id = carId, Vin = "VIN3" });
            dbContext.Policies.Add(new InsurancePolicy { CarId = carId, StartDate = new DateOnly(2024, 1, 1), EndDate = policyEndDate });
            await dbContext.SaveChangesAsync();

            var service = CreateServiceWithValidator(dbContext);

            var result = await service.IsInsuranceValidAsync(carId, policyEndDate.AddDays(1));

            Xunit.Assert.False(result);
        }

        [Fact]
        public async Task GetCarHistory_WhenCarHasPoliciesAndClaims_ReturnsCorrectTimeline()
        {
            var dbContext = GetInMemoryDbContext();
            var carId = 1L;
            var policyId = 1L;
            dbContext.Cars.Add(new Car { Id = carId, Vin = "VIN4" });
            dbContext.Policies.Add(new InsurancePolicy { Id = policyId, CarId = carId, StartDate = new DateOnly(2023, 5, 1), EndDate = new DateOnly(2024, 5, 1), Provider = "Provider A" });
            dbContext.Claims.Add(new Claim(policyId, new DateOnly(2023, 10, 15), "Broken windshield", 500));
            await dbContext.SaveChangesAsync();

            var service = CreateServiceWithValidator(dbContext);

            var history = await service.GetCarHistoryAsync(carId);

            Xunit.Assert.Equal(2, history.Count);
            Xunit.Assert.Equal("Policy", history[0].Type);
            Xunit.Assert.Equal("Claim", history[1].Type);
        }

        [Fact]
        public async Task GetCarHistory_WhenCarHasNoHistory_ReturnsEmptyList()
        {
            var dbContext = GetInMemoryDbContext();
            var carId = 1L;
            dbContext.Cars.Add(new Car { Id = carId, Vin = "VIN5" });
            await dbContext.SaveChangesAsync();

            var service = CreateServiceWithValidator(dbContext);

            var history = await service.GetCarHistoryAsync(carId);

            Xunit.Assert.Empty(history);
        }

        [Fact]
        public async Task GetCarHistory_WhenCarHasOnlyPolicies_ReturnsOnlyPolicies()
        {
            var dbContext = GetInMemoryDbContext();
            var carId = 1L;
            dbContext.Cars.Add(new Car { Id = carId, Vin = "VIN9" });
            dbContext.Policies.Add(new InsurancePolicy { Id = 1L, CarId = carId, StartDate = new DateOnly(2024, 1, 1), EndDate = new DateOnly(2024, 12, 31) });
            await dbContext.SaveChangesAsync();

            var service = CreateServiceWithValidator(dbContext);
            var history = await service.GetCarHistoryAsync(carId);

            Xunit.Assert.Single(history);
            Xunit.Assert.Equal("Policy", history.First().Type);
            Xunit.Assert.NotNull(history.First().Description);
        }

        [Fact]
        public async Task RegisterClaim_WhenValidPolicyExists_Succeeds()
        {
            var dbContext = GetInMemoryDbContext();
            var carId = 1L;
            var policyId = 1L;
            dbContext.Cars.Add(new Car { Id = carId, Vin = "VIN6" });
            dbContext.Policies.Add(new InsurancePolicy { Id = policyId, CarId = carId, StartDate = new DateOnly(2024, 1, 1), EndDate = new DateOnly(2024, 12, 31) });
            await dbContext.SaveChangesAsync();

            var service = CreateServiceWithValidator(dbContext);
            var claimDate = new DateOnly(2024, 6, 15);

            var result = await service.RegisterClaimAsync(carId, claimDate, "Minor fender bender", 1500);

            Xunit.Assert.True(result);
            Xunit.Assert.Single(dbContext.Claims.Where(c => c.PolicyId == policyId));
        }

        [Fact]
        public async Task RegisterClaim_WhenNoValidPolicyExists_ThrowsInvalidOperationException()
        {
            var dbContext = GetInMemoryDbContext();
            var carId = 1L;
            dbContext.Cars.Add(new Car { Id = carId, Vin = "VIN7" });
            dbContext.Policies.Add(new InsurancePolicy { CarId = carId, StartDate = new DateOnly(2023, 1, 1), EndDate = new DateOnly(2023, 12, 31) }); // Expired policy
            await dbContext.SaveChangesAsync();

            var service = CreateServiceWithValidator(dbContext);
            var claimDate = new DateOnly(2024, 6, 15);

            await Xunit.Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.RegisterClaimAsync(carId, claimDate, "Claim on expired policy", 1000)
            );
        }

        [Fact]
        public async Task IsInsuranceValid_WhenCarDoesNotExist_ThrowsKeyNotFoundException()
        {
            var dbContext = GetInMemoryDbContext();
            var nonExistentCarId = 999L;

            var mockValidator = new Mock<ICarExistsValidator>();
            mockValidator.Setup(v => v.IsValid(It.IsAny<long>())).ReturnsAsync(false);
            var service = new CarService(dbContext, mockValidator.Object);

            await Xunit.Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.IsInsuranceValidAsync(nonExistentCarId, new DateOnly(2024, 6, 1))
            );
        }

        [Fact]
        public async Task GetCarHistory_WhenCarDoesNotExist_ThrowsKeyNotFoundException()
        {
            var dbContext = GetInMemoryDbContext();
            var nonExistentCarId = 999L;

            var mockValidator = new Mock<ICarExistsValidator>();
            mockValidator.Setup(v => v.IsValid(It.IsAny<long>())).ReturnsAsync(false);
            var service = new CarService(dbContext, mockValidator.Object);

            await Xunit.Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.GetCarHistoryAsync(nonExistentCarId)
            );
        }

        [Fact]
        public async Task RegisterClaim_WhenCarDoesNotExist_ThrowsKeyNotFoundException()
        {
            var dbContext = GetInMemoryDbContext();
            var nonExistentCarId = 999L;

            var mockValidator = new Mock<ICarExistsValidator>();
            mockValidator.Setup(v => v.IsValid(It.IsAny<long>())).ReturnsAsync(false);
            var service = new CarService(dbContext, mockValidator.Object);

            await Xunit.Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.RegisterClaimAsync(nonExistentCarId, new DateOnly(2024, 6, 1), "Non-existent car claim", 1000)
            );
        }
        [Fact]
        public async Task ListCarsAsync_WhenCarsExist_ReturnsAllCars()
        {
            var dbContext = GetInMemoryDbContext();

            var owner1 = new Owner { Id = 1L, Name = "John Smith", Email = "john.smith@example.com" };
            var owner2 = new Owner { Id = 2L, Name = "Jane Doe", Email = "jane.doe@example.com" };

            dbContext.Owners.Add(owner1);
            dbContext.Owners.Add(owner2);

            dbContext.Cars.Add(new Car
            {
                Id = 1L,
                Vin = "VIN-A",
                Make = "Ford",
                Model = "Focus",
                YearOfManufacture = 2018,
                OwnerId = owner1.Id,
                Owner = owner1
            });
            dbContext.Cars.Add(new Car
            {
                Id = 2L,
                Vin = "VIN-B",
                Make = "Honda",
                Model = "Civic",
                YearOfManufacture = 2020,
                OwnerId = owner2.Id,
                Owner = owner2
            });

            await dbContext.SaveChangesAsync();

            var service = CreateServiceWithValidator(dbContext);

            var result = await service.ListCarsAsync();

            Xunit.Assert.Equal(2, result.Count());
            var carA = result.First(c => c.Id == 1L);
            Xunit.Assert.Equal("Ford", carA.Make);
            Xunit.Assert.Equal("John Smith", carA.OwnerName);
        }
    }
}