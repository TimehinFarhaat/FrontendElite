using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

public class InquiriesController : Controller
{
    private readonly InquiryApiClient _apiClient;
    private readonly CarApiClient _carApiClient;
    private readonly IConfiguration _configuration;

    public InquiriesController(InquiryApiClient apiClient, CarApiClient carApiClient, IConfiguration configuration)
    {
        _apiClient = apiClient;
        _carApiClient = carApiClient;
        _configuration = configuration;
    }


   
    private bool IsAdmin() => HttpContext.Session.GetString("IsAdmin") == "true";

    private string? GetUserIdentifier()
    {
        return User.Identity?.IsAuthenticated == true ? User.Identity.Name : null;
    }


    private async Task<IActionResult> RedirectToUserInquiries(string? userIdentifier)
    {
        if (string.IsNullOrWhiteSpace(userIdentifier))
        {
            TempData["Error"] = "Unable to identify user.";
            return RedirectToAction("Index", "Home");
        }

        var inquiries = await _apiClient.GetInquiriesByEmailAsync(userIdentifier); 
        return View("UserIndex", inquiries);
    }




    public async Task<IActionResult> Index()
    {
        if (IsAdmin())
        {
            var inquiries = await _apiClient.GetAllInquiriesAsync();
            return View("Index", inquiries);
        }

        return await RedirectToUserInquiries(GetUserIdentifier());
    }

    [HttpGet]
    public  IActionResult UserIndex()
    {
        var apiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? _configuration["ApiSettings:BaseUrl"];
        ViewBag.ApiBaseUrl = apiBaseUrl;


        return View(); 
    }

    // Edit user inquiry
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        if (IsAdmin()) return RedirectToAction(nameof(Index));

        try
        {
            var inquiry = await _apiClient.GetInquiryByIdAsync(id);
            return View(inquiry);
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(UserIndex));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UpdateInquiryDto dto)
    {
        if (IsAdmin())
        {
            TempData["Error"] = "Admins cannot update user inquiries.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _apiClient.UpdateUserInquiryAsync(id, dto);
            TempData["Success"] = "Inquiry updated successfully!";
            return RedirectToAction(nameof(UserIndex));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return View(dto);
        }
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUserInquiry(Guid id, string email)
    {
        if (IsAdmin())
        {
            TempData["Error"] = "Admins cannot delete user inquiries using this action.";
            // Stay on the same page
            return  UserIndex();
        }

        try
        {
            var inquiry = await _apiClient.GetInquiryByIdAsync(id);

            // Only allow delete if no response or "No response yet"
            if (!string.IsNullOrWhiteSpace(inquiry.Response) && inquiry.Response != "No response yet")
                throw new InvalidOperationException("You cannot delete an inquiry that already has a response.");

            await _apiClient.DeleteUserInquiryAsync(id, email);
            TempData["Success"] = "Inquiry deleted successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        // Stay on the same page with updated inquiries
        return  UserIndex();
    }


    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateInquiry(CreateInquiryDto dto)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please fill in all required fields correctly.";
            TempData["ReopenInquiryCarId"] = dto.CarId;
            TempData["InquiryName"] = dto.Name;
            TempData["InquiryEmail"] = dto.Email;
            TempData["InquiryMessage"] = dto.Message;

            return RedirectToAction("Details", "Cars", new { id = dto.CarId });
        }

        try
        {
            await _apiClient.CreateInquiryAsync(dto);
            TempData["Message"] = "Inquiry submitted successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Failed to submit inquiry: " + ex.Message;
            TempData["ReopenInquiryCarId"] = dto.CarId;
            TempData["InquiryName"] = dto.Name;
            TempData["InquiryEmail"] = dto.Email;
            TempData["InquiryMessage"] = dto.Message;
        }

        return RedirectToAction("Details", "Cars", new { id = dto.CarId });
    }


   

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(Guid id, string response)
    {
        if (!IsAdmin())
        {
            TempData["Error"] = "Only admins can reply to inquiries.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _apiClient.ReplyToInquiryAsync(id, response);
            TempData["Success"] = "Response saved successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!IsAdmin())
        {
            TempData["Error"] = "Only admins can delete inquiries.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var inquiry = await _apiClient.GetInquiryByIdAsync(id);
            if (string.IsNullOrWhiteSpace(inquiry.Response))
                throw new InvalidOperationException("Admin can only delete inquiries that already have a response.");

            await _apiClient.DeleteInquiryAsync(id);
            TempData["Success"] = "Inquiry deleted successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteResponse(Guid id)
    {
        if (!IsAdmin())
        {
            TempData["Error"] = "Only admins can delete responses.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _apiClient.DeleteInquiryResponseAsync(id);
            TempData["Success"] = "Response deleted successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }


}
