using CarInsurance.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Validators
{
    public interface ICarExistsValidator
    {
        Task<bool> IsValid(long carId);
    }

    public class CarExistsValidator : ICarExistsValidator
    {
        private readonly AppDbContext _db;

        public CarExistsValidator(AppDbContext db)
        {
            _db = db;
        }

        public async Task<bool> IsValid(long carId)
        {
            return await _db.Cars.AnyAsync(c => c.Id == carId);
        }
    }
}
