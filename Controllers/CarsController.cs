using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class CarsController : Controller
{
    private readonly CarApiClient _carApiClient;
    private readonly IConfiguration _configuration;
    public CarsController(CarApiClient carApiClient, IConfiguration configuration)
    {
        _carApiClient = carApiClient;
        _configuration = configuration;
    }


    private bool IsAdmin() => HttpContext.Session.GetString("IsAdmin") == "true";

    private string? GetUserIdentifier()
    {
        return User.Identity?.IsAuthenticated == true ? User.Identity.Name : null;
    }


  
    // 📌 LIST ALL CARS
    public async Task<IActionResult> Index()
    {
        var apiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? _configuration["ApiSettings:BaseUrl"];
        ViewBag.ApiBaseUrl = apiBaseUrl;

        var result = await _carApiClient.GetAllCarsAsync();

        if (!result.IsSuccess)
        {
            ViewBag.Error = result.ErrorMessage ?? "Failed to load cars.";
            return View(new List<CarDto>());
        }

        return View(result.Data);
    }

    // 📌 CREATE CAR - GET
    [HttpGet]
    public IActionResult Create() => View();

    // 📌 CREATE CAR - POST
    [HttpPost]
    public async Task<IActionResult> Create(CreateCarRequest car)
    {
        if (!ModelState.IsValid)
            return View(car);

        var result = await _carApiClient.CreateCarAsync(car);

        if (result.IsSuccess)
        {
            TempData["Message"] = "Car added successfully!";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Error = result.ErrorMessage ?? "Failed to add car.";
        return View(car);
    }

    // 📌 EDIT CAR - GET
    public async Task<IActionResult> Edit(Guid id)
    {
        var apiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? _configuration["ApiSettings:BaseUrl"];
        ViewBag.ApiBaseUrl = apiBaseUrl;

        var result = await _carApiClient.GetCarByIdAsync(id);
        if (!result.IsSuccess || result.Data == null)
        {
            TempData["Error"] = result.ErrorMessage ?? "Car not found.";
            return RedirectToAction(nameof(Index));
        }

        // Map CarDto -> UpdateCarRequest for the form
        var updateModel = new UpdateCarRequest
        {
            Make = result.Data.Make,
            Model = result.Data.Model,
            Year = result.Data.Year,
            Price = result.Data.Price,
            Description = result.Data.Description
        };

        ViewBag.CarDto = result.Data; // Send original DTO for images
        return View(updateModel);
    }

    // POST: Edit Car
    // POST: Edit Car
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UpdateCarRequest model)
    {
        var apiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? _configuration["ApiSettings:BaseUrl"];
        ViewBag.ApiBaseUrl = apiBaseUrl;


        if (!ModelState.IsValid)
        {
            ViewBag.Error = "Please fix the errors in the form.";
            var carResult = await _carApiClient.GetCarByIdAsync(id);
            ViewBag.CarDto = carResult.Data;
            return View(model);
        }

        var result = await _carApiClient.UpdateCarAsync(id, model);

        if (result.IsSuccess)
        {
            ViewBag.Message = "Car updated successfully! Redirecting to details...";
            ViewBag.RedirectUrl = Url.Action("Details", new { id });
            // Fetch latest DTO only if you need images updated
            var carResult = await _carApiClient.GetCarByIdAsync(id);
            ViewBag.CarDto = carResult.Data;
            return View(model);
        }

        ViewBag.Error = result.ErrorMessage ?? "Failed to update car.";
        var carDtoResult = await _carApiClient.GetCarByIdAsync(id);
        ViewBag.CarDto = carDtoResult.Data;
        return View(model);
    }

    // 📌 DELETE CAR
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _carApiClient.DeleteCarAsync(id);

        TempData["Message"] = result.IsSuccess
            ? "Car deleted successfully!"
            : result.ErrorMessage ?? "Failed to delete car.";

        return RedirectToAction(nameof(Index));
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCarImage(Guid carId, Guid imageId)
    {
        // Call API client
        var result = await _carApiClient.DeleteCarImageAsync(carId, imageId);

        if (result.IsSuccess)
        {
            TempData["Message"] = "Image deleted successfully!";
        }
        else
        {
            TempData["Error"] = result.ErrorMessage ?? "Failed to delete image.";
        }

        // Redirect back to the car details page
        return RedirectToAction("Details", new { id = carId });
    }


    // 📌 CAR DETAILS
    public async Task<IActionResult> Details(Guid id)
    {
        var apiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? _configuration["ApiSettings:BaseUrl"];
        ViewBag.ApiBaseUrl = apiBaseUrl;

        var result = await _carApiClient.GetCarByIdAsync(id);

        if (!result.IsSuccess || result.Data == null)
        {
            TempData["Error"] = result.ErrorMessage ?? "Car not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data); // Strongly typed to CarDto
    }
}
