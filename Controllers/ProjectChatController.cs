using Microsoft.AspNetCore.Mvc;
using QLDA.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class ProjectChatController : Controller
{
    private readonly IConfiguration _config;
    public static readonly Dictionary<string, string> predefinedAnswers = new()
{
    { "Làm sao tạo dự án mới?", "Bạn có thể vào mục 'Projects' rồi click 'Create Project' để tạo dự án mới." },
    { "Ai là admin?", "Admin là người có quyền quản lý toàn bộ dự án và user." },
    { "Cách thêm thành viên vào dự án?", "Trong chi tiết dự án, chọn 'Add Member', nhập email và vai trò, rồi gửi lời mời." },
    { "Dự án có thể có bao nhiêu task?", "Không có giới hạn cụ thể về số lượng task trong một dự án." },
    { "Ai có thể chỉnh sửa task?", "Chỉ user được phân quyền (owner hoặc assignee) mới chỉnh sửa task." },
    { "Task bị trễ hạn xử lý thế nào?", "Hệ thống sẽ hiển thị task màu đỏ và gửi thông báo cho quản lý dự án." }
};
    public ProjectChatController(IConfiguration config)
    {
        _config = config;
    }

    // Trang chat
    [HttpGet]
    public IActionResult Chat(int projectId)
    {
        ViewBag.ProjectId = projectId;
        return View();
    }

    // Gửi tin nhắn đến AI
    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] ChatBotRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return Json(new ChatBotResponse { Reply = "Vui lòng nhập tin nhắn." });

        string userMessage = request.Message.Trim();

        // 1. Kiểm tra câu hỏi có trong predefinedAnswers không
        foreach (var key in predefinedAnswers.Keys)
        {
            if (userMessage.Contains(key, StringComparison.OrdinalIgnoreCase))
            {
                return Json(new ChatBotResponse { Reply = predefinedAnswers[key] });
            }
        }
        var apiKey = _config["OpenAI:ApiKey"];
        // 2. Nếu không tìm thấy, gọi OpenAI API
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
            new { role = "user", content = $"Hướng dẫn trả lời câu hỏi về Project Management System: {userMessage}" }
        }
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
        var json = await response.Content.ReadAsStringAsync();

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("choices", out var choicesElement) &&
                choicesElement.GetArrayLength() > 0)
            {
                var reply = choicesElement[0].GetProperty("message").GetProperty("content").GetString();
                reply ??= "AI không trả lời được câu hỏi này.";
                return Json(new ChatBotResponse { Reply = reply });
            }

            return Json(new ChatBotResponse { Reply = "Unexpected response format." });
        }
        catch (Exception ex)
        {
            return Json(new ChatBotResponse { Reply = "Error parsing AI response: " + ex.Message });
        }


    }
}
