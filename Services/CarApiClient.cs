using System.Text.Json;

public class CarApiClient
{
    private readonly HttpClient _httpClient;

    public CarApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(bool IsSuccess, string ErrorMessage, List<CarDto> Data)> GetAllCarsAsync()
    {
        var response = await _httpClient.GetAsync("getAll"); 

        if (response.IsSuccessStatusCode)
        {
            var cars = await response.Content.ReadFromJsonAsync<List<CarDto>>() ?? new List<CarDto>();
            return (true, null, cars);
        }

        return (false, await ExtractErrorAsync(response), new List<CarDto>());
    }

    public async Task<(bool IsSuccess, string ErrorMessage, CarDto Data)> GetCarByIdAsync(Guid id)
    {
        var response = await _httpClient.GetAsync($"{id}");
        if (response.IsSuccessStatusCode)
        {
            var car = await response.Content.ReadFromJsonAsync<CarDto>();
            return (true, null, car);
        }

        return (false, await ExtractErrorAsync(response), null);
    }

    public async Task<(bool IsSuccess, string ErrorMessage)> CreateCarAsync(CreateCarRequest car)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(car.Make ?? ""), "Make");
        form.Add(new StringContent(car.Model ?? ""), "Model");
        form.Add(new StringContent(car.Year.ToString()), "Year");
        form.Add(new StringContent(car.Price.ToString()), "Price");
        form.Add(new StringContent(car.Description ?? ""), "Description");

        if (car.Images != null)
        {
            foreach (var image in car.Images)
            {
                form.Add(new StreamContent(image.OpenReadStream()), "Images", image.FileName);
            }
        }

        var response = await _httpClient.PostAsync("CreateCar", form);

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await ExtractErrorAsync(response));
    }

    public async Task<(bool IsSuccess, string ErrorMessage)> UpdateCarAsync(Guid id, UpdateCarRequest car)
    {
        using var form = new MultipartFormDataContent();

        form.Add(new StringContent(car.Make ?? ""), "Make");
        form.Add(new StringContent(car.Model ?? ""), "Model");
        form.Add(new StringContent(car.Year.ToString()), "Year");
        form.Add(new StringContent(car.Price.ToString()), "Price");
        form.Add(new StringContent(car.Description ?? ""), "Description");

        if (car.Images != null && car.Images.Any())
        {
            foreach (var image in car.Images)
            {
                if (image != null)
                {
                    var streamContent = new StreamContent(image.OpenReadStream());
                    streamContent.Headers.ContentType =
                        new System.Net.Http.Headers.MediaTypeHeaderValue(image.ContentType);
                    form.Add(streamContent, "Images", image.FileName);
                }
            }
        }

        var response = await _httpClient.PutAsync($"{id}/updateCar", form);

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await ExtractErrorAsync(response));
    }

    public async Task<(bool IsSuccess, string ErrorMessage)> DeleteCarAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"{id}/deleteCar");

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await ExtractErrorAsync(response));
    }

    public async Task<(bool IsSuccess, CarDto? UpdatedCar, string ErrorMessage)> DeleteCarImageAsync(Guid carId, Guid imageId)
    {
        var response = await _httpClient.DeleteAsync($"{carId}/carImage/{imageId}");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var updatedCar = JsonSerializer.Deserialize<CarDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return (true, updatedCar, null);
        }

        var errorMessage = await ExtractErrorAsync(response);
        return (false, null, errorMessage);
    }



    private async Task<string> ExtractErrorAsync(HttpResponseMessage response)
    {
        var errorContent = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(errorContent))
            return null; // No error text — let UI handle this

        try
        {
            var problem = JsonSerializer.Deserialize<Dictionary<string, object>>(errorContent);
            if (problem != null)
            {
                if (problem.TryGetValue("message", out var msg) && !string.IsNullOrWhiteSpace(msg?.ToString()))
                    return msg.ToString();
                if (problem.TryGetValue("title", out var title) && !string.IsNullOrWhiteSpace(title?.ToString()))
                    return title.ToString();
                if (problem.TryGetValue("errors", out var errs) && errs != null)
                    return string.Join(" ", errs as IEnumerable<string> ?? new[] { errs.ToString() });
            }
        }
        catch
        {
            // If it's not JSON, just return raw text
        }

        return errorContent; // Raw text from server
    }
}