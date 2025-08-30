using CarInsurance.Api.Data;
using CarInsurance.Api.Dtos;
using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services;

public class CarService(AppDbContext db)
{
    private readonly AppDbContext _db = db;

    public async Task<List<CarDto>> ListCarsAsync()
    {
        return await _db.Cars.Include(c => c.Owner)
            .Select(c => new CarDto(c.Id, c.Vin, c.Make, c.Model, c.YearOfManufacture,
                                    c.OwnerId, c.Owner.Name, c.Owner.Email))
            .ToListAsync();
    }

    public async Task<bool> IsInsuranceValidAsync(long carId, DateOnly date)
    {
        var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
        if (!carExists) throw new KeyNotFoundException($"Car {carId} not found");

        return await _db.Policies.AnyAsync(p =>
            p.CarId == carId &&
            p.StartDate <= date &&
            (p.EndDate == null || p.EndDate >= date)
        );
    }

    public async Task<List<TimelineEventDto>> GetCarHistoryAsync(long carId)
    {
        var car = await _db.Cars.FindAsync(carId);
        if (car == null)
        {
            throw new KeyNotFoundException("Car not found.");
        }

        var policies = await _db.Policies
            .Where(p => p.CarId == carId)
            .ToListAsync();

        var claims = await _db.Claims
            .Where(c => policies.Select(p => p.Id).Contains(c.PolicyId))
            .ToListAsync();

        var timelineEvents = new List<TimelineEventDto>();

        timelineEvents.AddRange(policies.Select(p => new TimelineEventDto(
            "Policy",
            p.StartDate,
            $"Policy provided by {p.Provider}, lasting from {p.StartDate.ToShortDateString()} to {p.EndDate.ToShortDateString()}"
             )));

        timelineEvents.AddRange(claims.Select(c => new TimelineEventDto(
            "Claim",
            c.ClaimDate,
            $"Claim took for {c.Description} amounting to: {c.Amount}"
        )));

        return timelineEvents.OrderBy(e => e.Date).ToList();
    }
    public async Task<bool> RegisterClaimAsync(long carId, DateOnly claimDate, string description, decimal amount)
    {
        var car = await _db.Cars.FindAsync(carId);
        if (car == null)
        {
            throw new KeyNotFoundException("Car not found.");
        }

        var validPolicy = await _db.Policies
            .Where(p => p.CarId == carId &&
                        p.StartDate <= claimDate &&
                        p.EndDate >= claimDate)
            .FirstOrDefaultAsync();

        if (validPolicy == null)
        {
            throw new InvalidOperationException("No valid insurance policy found for the specified claim date.");
        }

        var newClaim = new Claim(validPolicy.Id, claimDate, description, amount);

        _db.Claims.Add(newClaim);
        await _db.SaveChangesAsync();

        return true;

    }

}
