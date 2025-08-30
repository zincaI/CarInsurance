using CarInsurance.Api.Dtos;
using CarInsurance.Api.Models;
using CarInsurance.Api.Services;
using Microsoft.AspNetCore.Mvc;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CarInsurance.Api.Controllers;

[ApiController]
[Route("api")]
public class CarsController(CarService service) : ControllerBase
{
    private readonly CarService _service = service;

    [HttpGet("cars")]
    public async Task<ActionResult<List<CarDto>>> GetCars()
        => Ok(await _service.ListCarsAsync());

    [HttpGet("cars/{carId:long}/insurance-valid")]
    public async Task<ActionResult<InsuranceValidityResponse>> IsInsuranceValid(long carId, [FromQuery] string date)
    {
        if (!DateOnly.TryParse(date, out var parsed))
            return BadRequest("Invalid date format. Use YYYY-MM-DD.");

        try
        {
            var valid = await _service.IsInsuranceValidAsync(carId, parsed);
            return Ok(new InsuranceValidityResponse(carId, parsed.ToString("yyyy-MM-dd"), valid));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("/api/cars/{carId:long}/claims")]

    public async Task<ActionResult<ClaimValidityResponse>> RegisterClaim(long carId, [FromQuery] string claimDate, [FromQuery] string description, [FromQuery] decimal amount)
    {
        if (!DateOnly.TryParse(claimDate, out var parsed))
            return BadRequest("Invalid date format. Use YYYY-MM-DD.");

        try
        {
            await _service.RegisterClaimAsync(carId, parsed, description, amount);

            return Ok(new ClaimValidityResponse(carId, parsed.ToString("yyyy-MM-dd"), description, amount));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(500, "An internal server error occurred.");
        }
    }
}
