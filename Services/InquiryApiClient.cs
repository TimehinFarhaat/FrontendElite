using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class InquiryApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CookieContainer _cookieContainer;

    public InquiryApiClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, CookieContainer cookieContainer)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _cookieContainer = cookieContainer;
    }

    // Copy current ASP.NET session cookie to HttpClient for admin-only API calls
    private void CopySessionCookie()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.Request.Cookies.TryGetValue(".AspNetCore.Session", out var sessionValue) ?? false)
        {
            _cookieContainer.Add(_httpClient.BaseAddress!, new Cookie(".AspNetCore.Session", sessionValue));
        }
    }

    // GET all inquiries
    public async Task<List<InquiryDto>> GetAllInquiriesAsync()
    {
        CopySessionCookie();
        var response = await _httpClient.GetAsync("getAllInquiry");

        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<InquiryDto>>() ?? new List<InquiryDto>();

        // Return empty list instead of throwing
        return new List<InquiryDto>();
    }

    // GET inquiry by Id
    public async Task<InquiryDto> GetInquiryByIdAsync(Guid id)
    {
        CopySessionCookie();
        var response = await _httpClient.GetAsync($"{id}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<InquiryDto>();

        throw new Exception(await ExtractErrorAsync(response));
    }

    // POST create inquiry
    public async Task<InquiryDto> CreateInquiryAsync(CreateInquiryDto dto)
    {
        // Prepare form data instead of JSON
        using var formData = new MultipartFormDataContent
    {
        { new StringContent(dto.CarId.ToString()), nameof(dto.CarId) },
        { new StringContent(dto.Name), nameof(dto.Name) },
        { new StringContent(dto.Email), nameof(dto.Email) },
        { new StringContent(dto.Message), nameof(dto.Message) },
        { new StringContent(dto.CreatedAt.ToString("o")), nameof(dto.CreatedAt) } // optional
    };

        var response = await _httpClient.PostAsync("createInquiry", formData);

        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<InquiryDto>();

        throw new Exception(await ExtractErrorAsync(response));
    }


    // PUT update admin reply
    public async Task<InquiryDto> ReplyToInquiryAsync(Guid id, string responseText)
    {
        CopySessionCookie();
        using var content = new MultipartFormDataContent
        {
            { new StringContent(responseText ?? ""), "response" }
        };
        var response = await _httpClient.PutAsync($"{id}/replyInquiry", content);

        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<InquiryDto>();

        throw new Exception(await ExtractErrorAsync(response));
    }

    // PUT delete inquiry response
    public async Task<InquiryDto> DeleteInquiryResponseAsync(Guid id)
    {
        CopySessionCookie();
        var response = await _httpClient.PutAsync($"{id}/deleteInquiryResponse", null);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<InquiryDto>();

        throw new Exception(await ExtractErrorAsync(response));
    }

    // DELETE inquiry (admin)
    public async Task DeleteInquiryAsync(Guid id)
    {
        CopySessionCookie();
        var response = await _httpClient.DeleteAsync($"{id}/deleteInquiry");
        if (!response.IsSuccessStatusCode)
            throw new Exception(await ExtractErrorAsync(response));
    }

    // PUT update user inquiry
    public async Task<InquiryDto> UpdateUserInquiryAsync(Guid id, UpdateInquiryDto dto)
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent(dto.Name ?? ""), "Name" },
            { new StringContent(dto.Email ?? ""), "Email" },
            { new StringContent(dto.Message ?? ""), "Message" }
        };

        var response = await _httpClient.PutAsync($"{id}/updateUserInquiry", content);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<InquiryDto>();

        throw new Exception(await ExtractErrorAsync(response));
    }

    // DELETE user inquiry
    public async Task DeleteUserInquiryAsync(Guid id, string email)
    {
        var response = await _httpClient.DeleteAsync($"{id}/deleteUserInquiry?userEmail={Uri.EscapeDataString(email)}");
        if (!response.IsSuccessStatusCode)
            throw new Exception(await ExtractErrorAsync(response));
    }

    // GET inquiries by email
    public async Task<List<InquiryDto>> GetInquiriesByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return new List<InquiryDto>();

        var response = await _httpClient.GetAsync($"byEmail?email={Uri.EscapeDataString(email)}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<InquiryDto>>() ?? new List<InquiryDto>();

        throw new Exception(await ExtractErrorAsync(response));
    }

    // Helper: extract server error
    private async Task<string> ExtractErrorAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        try
        {
            var problemDetails = JsonSerializer.Deserialize<ValidationProblemDetails>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (problemDetails?.Errors != null && problemDetails.Errors.Any())
            {
                return string.Join(" | ", problemDetails.Errors
                    .SelectMany(e => e.Value)
                    .Distinct());
            }

            return problemDetails?.Title ?? content;
        }
        catch
        {
            return content;
        }
    }

}
