using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CarInsurance.Api.Dtos;
using Xunit;

namespace CarInsurance.Tests.IntegrationTests
{
    public class CarsControllerExternalIntegrationTests
    {
        private const string ApiBaseUrl = "http://localhost:61342";
        private readonly HttpClient _client;

        public CarsControllerExternalIntegrationTests()
        {
            _client = new HttpClient();
        }

        [Fact]
        public async Task GetCars_ReturnsSuccessAndListOfCars()
        {
            var response = await _client.GetAsync($"{ApiBaseUrl}/api/cars");

            response.EnsureSuccessStatusCode();
            Xunit.Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var cars = await response.Content.ReadFromJsonAsync<List<CarDto>>();
            Xunit.Assert.NotNull(cars);
            Xunit.Assert.NotEmpty(cars);
        }

        [Fact]
        public async Task GetInsuranceValid_ReturnsSuccessAndValidStatus()
        {
            var carId = 1;
            var date = "2024-01-01";
            var response = await _client.GetAsync($"{ApiBaseUrl}/api/cars/{carId}/insurance-valid?date={date}");

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<InsuranceValidityResponse>();
            Xunit.Assert.NotNull(result);
            Xunit.Assert.True(result.Valid);
        }

        [Fact]
        public async Task GetInsuranceValid_ForNonExistentCar_ReturnsNotFound()
        {
            var carId = 999;
            var date = "2024-01-01";
            var response = await _client.GetAsync($"{ApiBaseUrl}/api/cars/{carId}/insurance-valid?date={date}");

            Xunit.Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetCarHistory_WithExistingCarId_ReturnsSuccessAndHistory()
        {
            var carId = 1;
            var response = await _client.GetAsync($"{ApiBaseUrl}/api/cars/{carId}/history");

            response.EnsureSuccessStatusCode();
            var history = await response.Content.ReadFromJsonAsync<List<TimelineEventDto>>();
            Xunit.Assert.NotNull(history);
            Xunit.Assert.NotEmpty(history);
        }

        [Fact]
        public async Task GetCarHistory_WithNonExistentCarId_ReturnsNotFound()
        {
            var carId = 999;
            var response = await _client.GetAsync($"{ApiBaseUrl}/api/cars/{carId}/history");

            Xunit.Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PostNewClaim_WithValidData_ReturnsSuccess()
        {
            var carId = 1;
            var claimDate = "2024-06-01";
            var description = "Broken windshield";
            var amount = 500.00m;

            var response = await _client.PostAsync(
                $"{ApiBaseUrl}/api/cars/{carId}/claims?claimDate={claimDate}&description={description}&amount={amount}",
                null);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ClaimValidityResponse>();
            Xunit.Assert.NotNull(result);
            Xunit.Assert.Equal(carId, result.CarId);
        }

        [Fact]
        public async Task PostNewClaim_ForNonExistentCar_ReturnsNotFound()
        {
            var carId = 999;
            var claimDate = "2024-06-01";
            var description = "Broken windshield";
            var amount = 500.00m;

            var response = await _client.PostAsync(
                $"{ApiBaseUrl}/api/cars/{carId}/claims?claimDate={claimDate}&description={description}&amount={amount}",
                null);

            Xunit.Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PostNewClaim_WithInvalidDate_ReturnsBadRequest()
        {
            var carId = 1;
            var claimDate = "invalid-date";
            var description = "Broken windshield";
            var amount = 500.00m;

            var response = await _client.PostAsync(
                $"{ApiBaseUrl}/api/cars/{carId}/claims?claimDate={claimDate}&description={description}&amount={amount}",
                null);

            Xunit.Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}