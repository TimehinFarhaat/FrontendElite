using System.Text.Json;

public class AdminApiClient
{
    private readonly HttpClient _httpClient;

    public AdminApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(bool IsSuccess, string Message)> LoginAsync(string username, string password)
    {
        var form = new MultipartFormDataContent
    {
        { new StringContent(username ?? ""), "Username" },
        { new StringContent(password ?? ""), "Password" }
    };

        var response = await _httpClient.PostAsync("login", form);
        var raw = await response.Content.ReadAsStringAsync();

       

        var message = await ExtractMessageAsync(raw, response.IsSuccessStatusCode);

        return (response.IsSuccessStatusCode, message);
    }

    public async Task<bool> LogoutAsync()
    {
        var response = await _httpClient.PostAsync("logout", null);
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        return false;
    }

    private Task<string> ExtractMessageAsync(string raw, bool success)
    {
        if (success)
        {
            // ✅ Always return a default success message if API didn't send anything
            if (string.IsNullOrWhiteSpace(raw))
                return Task.FromResult("✅ Operation completed successfully.");

            // Try parsing the raw as JSON for any message
            try
            {
                var json = JsonSerializer.Deserialize<Dictionary<string, object>>(raw);
                if (json != null && json.ContainsKey("message"))
                    return Task.FromResult(json["message"]?.ToString());
                if (json != null && json.ContainsKey("title"))
                    return Task.FromResult(json["title"]?.ToString());
                if (json != null && json.ContainsKey("success"))
                    return Task.FromResult(json["success"]?.ToString());
            }
            catch
            {
                // Not JSON, just return text
            }

            return Task.FromResult(raw);
        }
        else
        {
            // ❌ Failure path
            if (string.IsNullOrWhiteSpace(raw))
                return Task.FromResult("❌ Unknown error occurred. Please try again.");

            try
            {
                var json = JsonSerializer.Deserialize<Dictionary<string, object>>(raw);
                if (json != null && json.ContainsKey("message"))
                    return Task.FromResult(json["message"]?.ToString());
                if (json != null && json.ContainsKey("title"))
                    return Task.FromResult(json["title"]?.ToString());
                if (json != null && json.ContainsKey("errors"))
                    return Task.FromResult(string.Join(" ", json["errors"]));
            }
            catch
            {
                // Not JSON, just return text
            }

            return Task.FromResult(raw);
        }
    }


}
