using CarInsurance.Api.Data;
using CarInsurance.Api.Models;
using CarInsurance.Api.Validators;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace CarInsurance.Tests.UnitTests
{
    public class CarExistsValidatorTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) 
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task IsValid_WhenCarExists_ReturnsTrue()
        {
            var dbContext = GetInMemoryDbContext();
            var carId = 1L;
            dbContext.Cars.Add(new Car { Id = carId, Vin = "VIN-123", Make = "Toyota", Model = "Corolla", YearOfManufacture = 2020 });
            await dbContext.SaveChangesAsync();

            var validator = new CarExistsValidator(dbContext);

            var result = await validator.IsValid(carId);

            Assert.True(result);
        }

        [Fact]
        public async Task IsValid_WhenCarDoesNotExist_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var nonExistentCarId = 999L;

            var validator = new CarExistsValidator(dbContext);

            var result = await validator.IsValid(nonExistentCarId);

            Assert.False(result);
        }

        [Fact]
        public async Task IsValid_WhenMultipleCarsExist_StillFindsCorrectCar()
        {
            var dbContext = GetInMemoryDbContext();
            dbContext.Cars.Add(new Car { Id = 1L, Vin = "VIN-AAA", Make = "Ford", Model = "Focus", YearOfManufacture = 2018 });
            dbContext.Cars.Add(new Car { Id = 2L, Vin = "VIN-BBB", Make = "Honda", Model = "Civic", YearOfManufacture = 2020 });
            await dbContext.SaveChangesAsync();

            var validator = new CarExistsValidator(dbContext);
            
            var result1 = await validator.IsValid(1L);
            var result2 = await validator.IsValid(2L);

            Assert.True(result1);
            Assert.True(result2);
        }
    }
}
