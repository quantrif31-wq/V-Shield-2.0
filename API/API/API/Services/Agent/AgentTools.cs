using System.Text.Json;
using System.Text.Json.Nodes;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Agent;

/// <summary>Toàn bộ tool của AI agent (Skill).</summary>
public sealed class AgentTools : IReadOnlyCollection<IAgentTool>
{
    private readonly Dictionary<string, IAgentTool> _tools;

    public AgentTools(MemoryService memory, AgentLlmClient llm)
    {
        _tools = new Dictionary<string, IAgentTool>(StringComparer.OrdinalIgnoreCase);
        var all = new IAgentTool[]
        {
            new GetMeTool(),
            new SearchPeopleTool(),
            new GetPersonTool(),
            new GetOrgRelationTool(),
            new ResolveGreetingTool(),
            new DraftEmailTool(llm),
            new SaveNoteTool(memory),
            new GetNoteTool(memory)
        };
        foreach (var t in all) _tools[t.Name] = t;
    }

    public IAgentTool? Get(string name) => _tools.TryGetValue(name, out var t) ? t : null;
    public int Count => _tools.Count;
    public IEnumerator<IAgentTool> GetEnumerator() => _tools.Values.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Danh sách tool schema theo định dạng OpenAI/DeepSeek.</summary>
    public List<object> ToOpenAiSchemas()
    {
        var list = new List<object>();
        foreach (var t in _tools.Values)
        {
            list.Add(new
            {
                type = "function",
                function = new
                {
                    name = t.Name,
                    description = t.Description,
                    parameters = t.ParametersSchema
                }
            });
        }
        return list;
    }
}

// ---------- helpers ----------
internal static class ToolHelpers
{
    public static string Json(object obj) => JsonSerializer.Serialize(obj);

    public static string GetString(JsonObject args, string key)
        => args.TryGetPropertyValue(key, out var v) && v is JsonValue jv ? (jv.GetValue<string>() ?? "") : "";

    public static string? GetNullableString(JsonObject args, string key)
        => args.TryGetPropertyValue(key, out var v) && v is JsonValue jv ? jv.GetValue<string>() : null;

    public static string[] GetStringArray(JsonObject args, string key)
    {
        if (args.TryGetPropertyValue(key, out var v) && v is JsonArray arr)
        {
            return arr.Where(x => x is JsonValue).Select(x => x!.GetValue<string>()).ToArray();
        }
        return Array.Empty<string>();
    }

    /// <summary>Tên gọi (first name) từ họ tên kiểu Việt Nam: lấy token cuối.</summary>
    public static string FirstName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "";
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[^1] : fullName;
    }

    public static int? AgeFrom(DateOnly? dob)
    {
        if (dob is null) return null;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - dob.Value.Year;
        if (dob.Value > today.AddYears(-age)) age--;
        return age;
    }
}

// ---------- get_me ----------
internal sealed class GetMeTool : IAgentTool
{
    public string Name => "get_me";
    public string Description => "Lấy hồ sơ của người dùng đang đăng nhập (người gửi email): mã nhân viên, họ tên, chức vụ, phòng ban, email, ngày sinh, giới tính.";
    public JsonObject ParametersSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject(),
        ["additionalProperties"] = false
    };

    public async Task<string> ExecuteAsync(AgentToolContext ctx, JsonObject args, CancellationToken ct)
    {
        if (ctx.EmployeeId is null)
            return ToolHelpers.Json(new { error = "Tài khoản hiện tại không gắn với hồ sơ nhân viên.", employeeId = (int?)null });

        var emp = await ctx.Db.Employees
            .Include(e => e.Position).Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.EmployeeId == ctx.EmployeeId, ct);

        if (emp == null)
            return ToolHelpers.Json(new { error = "Không tìm thấy hồ sơ nhân viên.", employeeId = ctx.EmployeeId });

        return ToolHelpers.Json(new
        {
            employeeId = emp.EmployeeId,
            employeeCode = emp.EmployeeCode,
            fullName = emp.FullName,
            firstName = ToolHelpers.FirstName(emp.FullName),
            gender = emp.Gender,
            dateOfBirth = emp.DateOfBirth?.ToString("yyyy-MM-dd"),
            age = ToolHelpers.AgeFrom(emp.DateOfBirth.HasValue ? DateOnly.FromDateTime(emp.DateOfBirth.Value) : null),
            position = emp.Position?.Name,
            department = emp.Department?.Name,
            email = emp.Email,
            companyEmail = emp.CompanyEmail,
            phone = emp.Phone
        });
    }
}

// ---------- search_people ----------
internal sealed class SearchPeopleTool : IAgentTool
{
    public string Name => "search_people";
    public string Description => "Tìm người (nhân viên hoặc khách mời) theo tên, mã nhân viên, CCCD, email hoặc điện thoại. Trả về tối đa 6 kết quả khớp — nếu có nhiều ứng viên, hãy hỏi người dùng chọn ai.";
    public JsonObject ParametersSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["query"] = new JsonObject { ["type"] = "string", ["description"] = "Tên / mã nhân viên / CCCD / email / điện thoại cần tìm" },
            ["kind"] = new JsonObject { ["type"] = "string", ["description"] = "employee hoặc visitor (tùy chọn)" }
        },
        ["required"] = new JsonArray("query"),
        ["additionalProperties"] = false
    };

    public async Task<string> ExecuteAsync(AgentToolContext ctx, JsonObject args, CancellationToken ct)
    {
        var query = (ToolHelpers.GetString(args, "query") ?? "").Trim();
        var kind = (ToolHelpers.GetString(args, "kind") ?? "").Trim().ToLowerInvariant();
        if (query.Length < 2)
            return ToolHelpers.Json(new { error = "Từ khóa tìm kiếm quá ngắn." });

        var like = $"%{query}%";
        var results = new List<object>();

        if (kind is "" or "employee")
        {
            var emps = await ctx.Db.Employees
                .Where(e => e.Status != false)
                .Include(e => e.Position).Include(e => e.Department)
                .Where(e =>
                    EF.Functions.Like(EF.Functions.Collate(e.FullName, "Latin1_General_CI_AI"), like)
                    || EF.Functions.Like(EF.Functions.Collate(e.EmployeeCode ?? "", "Latin1_General_CI_AI"), like)
                    || EF.Functions.Like(EF.Functions.Collate(e.CCCD ?? "", "Latin1_General_CI_AI"), like)
                    || EF.Functions.Like(EF.Functions.Collate(e.Email ?? "", "Latin1_General_CI_AI"), like)
                    || EF.Functions.Like(EF.Functions.Collate(e.Phone ?? "", "Latin1_General_CI_AI"), like))
                .OrderBy(e => e.FullName)
                .Take(6)
                .ToListAsync(ct);

            foreach (var e in emps)
            {
                results.Add(new
                {
                    personId = e.EmployeeId,
                    kind = "employee",
                    fullName = e.FullName,
                    employeeCode = e.EmployeeCode,
                    position = e.Position?.Name,
                    department = e.Department?.Name,
                    email = e.Email,
                    companyEmail = e.CompanyEmail
                });
            }
        }

        if (kind is "" or "visitor")
        {
            var visitors = await ctx.Db.VisitorDetails
                .Where(v => v.FullName != null &&
                    (EF.Functions.Like(EF.Functions.Collate(v.FullName, "Latin1_General_CI_AI"), like)
                     || EF.Functions.Like(EF.Functions.Collate(v.IdCardNumber ?? "", "Latin1_General_CI_AI"), like)))
                .Take(6)
                .ToListAsync(ct);

            foreach (var v in visitors)
            {
                results.Add(new
                {
                    personId = v.VisitorDetailId,
                    kind = "visitor",
                    fullName = v.FullName,
                    employeeCode = (string?)null,
                    position = (string?)null,
                    department = "Khách mời",
                    email = (string?)null
                });
            }
        }

        return ToolHelpers.Json(new { query, count = results.Count, results });
    }
}

// ---------- get_person ----------
internal sealed class GetPersonTool : IAgentTool
{
    public string Name => "get_person";
    public string Description => "Lấy chi tiết hồ sơ một người theo personId và kind (employee/visitor). Bao gồm chức vụ, phòng ban, ngày sinh, giới tính, email; CCCD đầy đủ chỉ khi có quyền.";
    public JsonObject ParametersSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["personId"] = new JsonObject { ["type"] = "integer", ["description"] = "ID người cần xem" },
            ["kind"] = new JsonObject { ["type"] = "string", ["description"] = "employee hoặc visitor" }
        },
        ["required"] = new JsonArray("personId", "kind"),
        ["additionalProperties"] = false
    };

    public async Task<string> ExecuteAsync(AgentToolContext ctx, JsonObject args, CancellationToken ct)
    {
        var personId = args.TryGetPropertyValue("personId", out var id) && id is JsonValue jv ? jv.GetValue<int>() : 0;
        var kind = ToolHelpers.GetString(args, "kind").ToLowerInvariant();
        if (personId <= 0) return ToolHelpers.Json(new { error = "personId không hợp lệ." });

        var sensitive = ToolAuthorizer.CanSeeSensitiveProfile(ctx);

        if (kind == "visitor")
        {
            var v = await ctx.Db.VisitorDetails.FirstOrDefaultAsync(x => x.VisitorDetailId == personId, ct);
            if (v == null) return ToolHelpers.Json(new { error = "Không tìm thấy khách mời." });
            return ToolHelpers.Json(new
            {
                personId = v.VisitorDetailId,
                kind = "visitor",
                fullName = v.FullName,
                cccd = sensitive ? v.IdCardNumber : Mask(v.IdCardNumber),
                companyEmail = v.CompanyEmail,
                department = "Khách mời"
            });
        }

        var emp = await ctx.Db.Employees
            .Include(e => e.Position).Include(e => e.Department).Include(e => e.ManagerEmployee)
            .FirstOrDefaultAsync(e => e.EmployeeId == personId, ct);
        if (emp == null) return ToolHelpers.Json(new { error = "Không tìm thấy nhân viên." });

        return ToolHelpers.Json(new
        {
            personId = emp.EmployeeId,
            kind = "employee",
            employeeCode = emp.EmployeeCode,
            fullName = emp.FullName,
            firstName = ToolHelpers.FirstName(emp.FullName),
            gender = emp.Gender,
            dateOfBirth = emp.DateOfBirth?.ToString("yyyy-MM-dd"),
            age = ToolHelpers.AgeFrom(emp.DateOfBirth.HasValue ? DateOnly.FromDateTime(emp.DateOfBirth.Value) : null),
            position = emp.Position?.Name,
            department = emp.Department?.Name,
            email = emp.Email,
            companyEmail = emp.CompanyEmail,
            phone = emp.Phone,
            cccd = sensitive ? emp.CCCD : Mask(emp.CCCD),
            manager = emp.ManagerEmployee?.FullName
        });
    }

    private static string? Mask(string? v)
        => string.IsNullOrWhiteSpace(v) ? null : (v.Length > 4 ? $"{new string('*', v.Length - 4)}{v[^4..]}" : "****");
}

// ---------- get_org_relation ----------
internal sealed class GetOrgRelationTool : IAgentTool
{
    public string Name => "get_org_relation";
    public string Description => "Trả về quan hệ tổ chức giữa người gửi (tài khoản hiện tại) và người nhận: cùng phòng ban, có phải quản lý trực tiếp hay gián tiếp, chức vụ hai bên.";
    public JsonObject ParametersSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["personId"] = new JsonObject { ["type"] = "integer", ["description"] = "ID người nhận (employee)" },
            ["kind"] = new JsonObject { ["type"] = "string", ["description"] = "employee hoặc visitor" }
        },
        ["required"] = new JsonArray("personId", "kind"),
        ["additionalProperties"] = false
    };

    public async Task<string> ExecuteAsync(AgentToolContext ctx, JsonObject args, CancellationToken ct)
    {
        var personId = args.TryGetPropertyValue("personId", out var id) && id is JsonValue jv ? jv.GetValue<int>() : 0;
        if (personId <= 0 || ctx.EmployeeId is null)
            return ToolHelpers.Json(new { error = "Không xác định được quan hệ.", note = "Tài khoản hiện tại chưa gắn nhân viên." });

        var sender = await ctx.Db.Employees
            .Include(e => e.Position).Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.EmployeeId == ctx.EmployeeId, ct);
        var recipient = await ctx.Db.Employees
            .Include(e => e.Position).Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.EmployeeId == personId, ct);

        if (sender == null || recipient == null)
            return ToolHelpers.Json(new { error = "Không tìm thấy hồ sơ." });

        var sameDept = sender.DepartmentId == recipient.DepartmentId && sender.DepartmentId != null;
        var isManager = await IsManagerInChainAsync(ctx, recipient.EmployeeId, sender.EmployeeId, ct);

        return ToolHelpers.Json(new
        {
            sender = new { sender.EmployeeId, sender.FullName, position = sender.Position?.Name, department = sender.Department?.Name },
            recipient = new { recipient.EmployeeId, recipient.FullName, position = recipient.Position?.Name, department = recipient.Department?.Name },
            sameDepartment = sameDept,
            recipientIsManagerOfSender = isManager,
            relation = isManager ? "recipient_is_manager" : sameDept ? "same_department" : "other"
        });
    }

    private static async Task<bool> IsManagerInChainAsync(AgentToolContext ctx, int managerId, int employeeId, CancellationToken ct, int depth = 0)
    {
        if (depth > 8) return false;
        var emp = await ctx.Db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeId == employeeId, ct);
        if (emp?.ManagerEmployeeId is null) return false;
        if (emp.ManagerEmployeeId == managerId) return true;
        return await IsManagerInChainAsync(ctx, managerId, emp.ManagerEmployeeId.Value, ct, depth + 1);
    }
}

// ---------- resolve_greeting ----------
internal sealed class ResolveGreetingTool : IAgentTool
{
    public string Name => "resolve_greeting";
    public string Description => "Chọn cách xưng hô / chào hỏi phù hợp giữa người gửi (tài khoản hiện tại) và người nhận dựa trên tuổi, chức vụ, quan hệ và giới tính. Trả về greeting, closing và chữ ký gợi ý.";
    public JsonObject ParametersSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["personId"] = new JsonObject { ["type"] = "integer", ["description"] = "ID người nhận (employee)" },
            ["kind"] = new JsonObject { ["type"] = "string", ["description"] = "employee hoặc visitor" }
        },
        ["required"] = new JsonArray("personId", "kind"),
        ["additionalProperties"] = false
    };

    public async Task<string> ExecuteAsync(AgentToolContext ctx, JsonObject args, CancellationToken ct)
    {
        var personId = args.TryGetPropertyValue("personId", out var id) && id is JsonValue jv ? jv.GetValue<int>() : 0;
        var kind = ToolHelpers.GetString(args, "kind").ToLowerInvariant();

        string? recipientName = null, firstName = "", gender = null, position = null;
        DateOnly? dob = null;
        bool isManager = false;
        var sender = await ctx.Db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeId == ctx.EmployeeId, ct);

        if (kind == "visitor")
        {
            var v = await ctx.Db.VisitorDetails.FirstOrDefaultAsync(x => x.VisitorDetailId == personId, ct);
            if (v != null) { recipientName = v.FullName; firstName = ToolHelpers.FirstName(v.FullName); }
        }
        else
        {
            var e = await ctx.Db.Employees
                .Include(x => x.Position)
                .FirstOrDefaultAsync(x => x.EmployeeId == personId, ct);
            if (e != null)
            {
                recipientName = e.FullName;
                firstName = ToolHelpers.FirstName(e.FullName);
                gender = e.Gender;
                position = e.Position?.Name;
                dob = e.DateOfBirth.HasValue ? DateOnly.FromDateTime(e.DateOfBirth.Value) : null;
                if (sender != null)
                    isManager = await IsManagerInChainAsync(ctx, e.EmployeeId, sender.EmployeeId, ct);
            }
        }

        var recipientAge = ToolHelpers.AgeFrom(dob);
        var formal = isManager || string.IsNullOrEmpty(firstName)
            || (recipientAge.HasValue && (recipientAge.Value >= 45 || (sender != null && IsOlderThanSender(recipientAge, sender))));

        var honorific = gender switch
        {
            "Nam" or "male" or "M" or "1" => formal ? "Ông" : "Anh",
            "Nữ" or "female" or "F" or "0" or "2" => formal ? "Bà" : "Chị",
            _ => formal ? "Anh/Chị" : "Anh/Chị"
        };

        var greeting = formal ? $"Kính gửi {honorific} {firstName}," : $"Chào {honorific} {firstName},";
        var closing = formal ? "Trân trọng," : "Thân mến,";
        var signOff = sender != null ? $"{sender.FullName}" : "";

        return ToolHelpers.Json(new
        {
            recipientName,
            firstName,
            gender,
            recipientAge,
            isManager,
            formal,
            honorific,
            greeting,
            closing,
            signOff
        });
    }

    private static bool IsOlderThanSender(int? recipientAge, Employee sender)
    {
        if (sender.DateOfBirth is null) return false;
        var senderAge = ToolHelpers.AgeFrom(DateOnly.FromDateTime(sender.DateOfBirth.Value));
        return recipientAge.HasValue && senderAge.HasValue && recipientAge.Value >= senderAge.Value + 8;
    }

    private static async Task<bool> IsManagerInChainAsync(AgentToolContext ctx, int managerId, int employeeId, CancellationToken ct, int depth = 0)
    {
        if (depth > 8) return false;
        var emp = await ctx.Db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeId == employeeId, ct);
        if (emp?.ManagerEmployeeId is null) return false;
        if (emp.ManagerEmployeeId == managerId) return true;
        return await IsManagerInChainAsync(ctx, managerId, emp.ManagerEmployeeId.Value, ct, depth + 1);
    }
}

// ---------- draft_email (skill soạn email chuyên nghiệp) ----------
internal sealed class DraftEmailTool : IAgentTool
{
    private readonly AgentLlmClient _llm;
    public DraftEmailTool(AgentLlmClient llm) => _llm = llm;

    public string Name => "draft_email";
    public string Description => "SOẠN EMAIL CHUẨN DOANH NGHIỆP. Gọi sau khi đã có đủ thông tin: người nhận (qua search_people/get_person) + cách xưng hô (qua resolve_greeting) + mục đích/nội dung từ người dùng. " +
        "Skill sẽ tự viết thân email chuyên nghiệp (chủ đề, lời chào, thân bài, lời kết, chữ ký) theo chuẩn công sở 2026 và tạo bản nháp (chưa gửi). Người dùng sẽ xem và bấm Gửi trên màn hình. " +
        "Truyền đầy đủ: to, purpose (mục đích), content (nội dung người dùng cung cấp - có thể rỗng), recipientInfo (hồ sơ/xưng hô người nhận), tone, contentMode (polish/verbatim).";

    public JsonObject ParametersSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["to"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" }, ["description"] = "Email người nhận" },
            ["purpose"] = new JsonObject { ["type"] = "string", ["description"] = "Mục đích email (vd: xin nghỉ phép, đề xuất, xác nhận...)" },
            ["content"] = new JsonObject { ["type"] = "string", ["description"] = "Nội dung/ý người dùng cung cấp (có thể rỗng; nếu user nói 'giữ nguyên' thì bắt buộc truyền nguyên văn + contentMode=verbatim)" },
            ["recipientInfo"] = new JsonObject { ["type"] = "string", ["description"] = "Tóm tắt người nhận: tên, chức vụ, phòng ban, giới tính/tuổi, lời chào gợi ý (từ resolve_greeting)" },
            ["tone"] = new JsonObject { ["type"] = "string", ["description"] = "trang-trong (mặc định) / than-thien / khan-truong / trung-tinh" },
            ["contentMode"] = new JsonObject { ["type"] = "string", ["description"] = "polish (viết lại cho chuẩn) / verbatim (giữ nguyên nội dung user)" },
            ["cc"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" }, ["description"] = "Email CC (tùy chọn)" }
        },
        ["required"] = new JsonArray("to", "purpose"),
        ["additionalProperties"] = false
    };

    public async Task<string> ExecuteAsync(AgentToolContext ctx, JsonObject args, CancellationToken ct)
    {
        var to = ToolHelpers.GetStringArray(args, "to").Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        var cc = ToolHelpers.GetStringArray(args, "cc").Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        var purpose = ToolHelpers.GetString(args, "purpose").Trim();
        var content = ToolHelpers.GetString(args, "content").Trim();
        var recipientInfo = ToolHelpers.GetString(args, "recipientInfo").Trim();
        var tone = ToolHelpers.GetString(args, "tone").Trim();
        var contentMode = ToolHelpers.GetString(args, "contentMode").Trim().ToLowerInvariant();

        if (to.Length == 0) return ToolHelpers.Json(new { error = "Chưa có người nhận (to)." });
        if (string.IsNullOrWhiteSpace(purpose)) return ToolHelpers.Json(new { error = "Chưa có mục đích (purpose)." });

        var senderName = "";
        var senderPosition = "";
        var senderDept = "";
        if (ctx.EmployeeId is int empId)
        {
            var emp = await ctx.Db.Employees
                .Include(e => e.Position).Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.EmployeeId == empId, ct);
            if (emp != null)
            {
                senderName = emp.FullName ?? "";
                senderPosition = emp.Position?.Name ?? "";
                senderDept = emp.Department?.Name ?? "";
            }
        }

        string subject;
        string bodyFull;

        if (contentMode == "verbatim" && !string.IsNullOrWhiteSpace(content))
        {
            // giữ nguyên nội dung người dùng
            subject = string.IsNullOrWhiteSpace(purpose) ? "Email công việc" : purpose;
            bodyFull = content;
        }
        else
        {
            var compose = await ComposeAsync(purpose, content, recipientInfo, tone, senderName, senderPosition, senderDept, ct);
            if (compose == null)
            {
                // fallback: vẫn tạo nháp có CẤU TRÚC đầy đủ (chào + nội dung + kết + chữ ký)
                subject = purpose;
                var fbGreeting = DefaultGreeting(recipientInfo);
                var fbBody = string.IsNullOrWhiteSpace(content) ? $"[CẦN BỔ SUNG NỘI DUNG] - {purpose}" : content;
                bodyFull = string.Join("\n\n", new[] { fbGreeting, fbBody, "Trân trọng,", BuildSignature(senderName, senderPosition, senderDept) }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
            }
            else
            {
                var body = compose.Body;
                // nếu body quá ngắn so với nội dung người dùng -> thử viết lại đầy đủ hơn 1 lần
                if ((string.IsNullOrWhiteSpace(body) || body.Length < 100) && !string.IsNullOrWhiteSpace(content))
                {
                    var retry = await ComposeAsync(purpose, content, recipientInfo, tone + ", viet day du hon, dai hon, nhieu doan hon", senderName, senderPosition, senderDept, ct);
                    if (retry != null && !string.IsNullOrWhiteSpace(retry.Body) && retry.Body.Length > (body?.Length ?? 0))
                    {
                        compose = retry;
                        body = retry.Body;
                    }
                }
                // Đảm bảo đủ 5 phần — nếu model bỏ sót thì điền default phía server
                var greeting = !string.IsNullOrWhiteSpace(compose.Greeting) ? compose.Greeting : DefaultGreeting(recipientInfo);
                var closing = !string.IsNullOrWhiteSpace(compose.Closing) ? compose.Closing : "Trân trọng,";
                var signature = !string.IsNullOrWhiteSpace(compose.Signature) ? compose.Signature : BuildSignature(senderName, senderPosition, senderDept);
                if (string.IsNullOrWhiteSpace(body) || body.Length < 40)
                {
                    body = string.IsNullOrWhiteSpace(content) ? body : content;
                }
                subject = !string.IsNullOrWhiteSpace(compose.Subject) ? compose.Subject : purpose;
                bodyFull = string.Join("\n\n", new[] { greeting, body, closing, signature }.Where(s => !string.IsNullOrWhiteSpace(s)));
            }
        }

        var draft = new AgentDraft
        {
            AgentThreadId = ctx.ThreadId,
            UserId = ctx.UserId,
            EmployeeId = ctx.EmployeeId,
            Status = "Draft",
            To = string.Join(";", to),
            Subject = subject,
            Body = bodyFull,
            CreatedAt = DateTime.Now
        };
        ctx.Db.AgentDrafts.Add(draft);
        await ctx.Db.SaveChangesAsync(ct);

        return ToolHelpers.Json(new { ok = true, draftId = draft.AgentDraftId, to, subject, body = bodyFull });
    }

    private sealed record ComposeResult(string Subject, string Greeting, string Body, string Closing, string Signature);

    private async Task<ComposeResult?> ComposeAsync(
        string purpose, string content, string recipientInfo, string tone,
        string senderName, string senderPosition, string senderDept, CancellationToken ct)
    {
        var userPrompt =
            $"Nhiệm vụ: viết một email doanh nghiệp ĐẦY ĐỦ 5 PHẦN theo đúng cấu trúc mẫu.\n" +
            $"- Mục đích: {purpose}\n" +
            $"- Người gửi: {senderName} ({(string.IsNullOrWhiteSpace(senderPosition) ? "chưa rõ chức vụ" : senderPosition)})" +
            (string.IsNullOrWhiteSpace(senderDept) ? "" : $" - {senderDept}") + "\n" +
            (string.IsNullOrWhiteSpace(recipientInfo) ? "" : $"- Người nhận: {recipientInfo}\n") +
            (string.IsNullOrWhiteSpace(content) ? "" : $"- Nội dung người dùng cung cấp: {content}\n") +
            $"- Giọng văn: {tone}.\n\n" +
            "QUAN TRỌNG: cả 5 trường subject, greeting, body, closing, signature ĐỀU BẮT BUỘC KHÔNG ĐƯỢC RỖNG. " +
            "body phải là thân bài ít nhất 2-3 đoạn ngắn, có lời chào mở đầu rõ ràng. " +
            "Chỉ trả về JSON đúng định dạng: {\"subject\":\"...\",\"greeting\":\"...\",\"body\":\"...\",\"closing\":\"...\",\"signature\":\"...\"}. Không thêm gì khác.";

        var messages = new List<object>
        {
            new { role = "system", content = EmailWritingGuide.Playbook + "\n\n" + EmailWritingGuide.FewShot },
            new { role = "user", content = userPrompt }
        };

        var resp = await _llm.CompleteAsync(messages, null, maxTokens: 1400, cancellationToken: ct);
        if (resp.IsError || string.IsNullOrWhiteSpace(resp.Content)) return null;

        var parsed = TryParseJson(resp.Content);
        if (parsed == null)
        {
            // thử lại 1 lần với yêu cầu nghiêm ngặt hơn
            messages.Add(new { role = "assistant", content = resp.Content });
            messages.Add(new { role = "user", content = "Phản hồi trước không đúng định dạng JSON. Chỉ trả về JSON đúng định dạng như đã yêu cầu." });
            resp = await _llm.CompleteAsync(messages, null, maxTokens: 1400, cancellationToken: ct);
            if (resp.IsError || string.IsNullOrWhiteSpace(resp.Content)) return null;
            parsed = TryParseJson(resp.Content);
        }

        if (parsed == null) return null;

        return new ComposeResult(
            parsed.Subject ?? purpose,
            parsed.Greeting ?? "",
            parsed.Body ?? content,
            parsed.Closing ?? "",
            parsed.Signature ?? "");
    }

    private static ComposeResult? TryParseJson(string text)
    {
        var t = text.Trim();
        if (t.StartsWith("```")) t = t.Trim('`', '\n', ' ');
        var start = t.IndexOf('{');
        var end = t.LastIndexOf('}');
        if (start < 0 || end <= start) return null;
        t = t.Substring(start, end - start + 1);
        try
        {
            using var doc = JsonDocument.Parse(t);
            var root = doc.RootElement;
            return new ComposeResult(
                Get(root, "subject"),
                Get(root, "greeting"),
                Get(root, "body"),
                Get(root, "closing"),
                Get(root, "signature"));
        }
        catch
        {
            return null;
        }
    }

    private static string Get(JsonElement root, string key)
        => root.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    private static string DefaultGreeting(string recipientInfo)
    {
        if (!string.IsNullOrWhiteSpace(recipientInfo))
        {
            // ưu tiên lời chào đã resolve (vd "Kính gửi Bà Hùng,") nếu có trong recipientInfo
            var m = System.Text.RegularExpressions.Regex.Match(recipientInfo, @"(Kính gửi|Chào|Thưa)[^,\n]*");
            if (m.Success) return m.Value.TrimEnd() + ",";
        }
        return "Kính gửi,";
    }

    private static string BuildSignature(string senderName, string senderPosition, string senderDept)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(senderName)) parts.Add(senderName);
        if (!string.IsNullOrWhiteSpace(senderPosition)) parts.Add(senderPosition);
        if (!string.IsNullOrWhiteSpace(senderDept)) parts.Add(senderDept);
        return string.Join("\n", parts);
    }
}

// ---------- notes (bộ nhớ ngắn hạn) ----------
internal sealed class SaveNoteTool : IAgentTool
{
    private readonly MemoryService _memory;
    public SaveNoteTool(MemoryService memory) => _memory = memory;

    public string Name => "save_note";
    public string Description => "Ghi lại một ghi chú ngắn vào bộ nhớ của phiên (key + nội dung), dùng để nhớ thông tin trong suốt cuộc hội thoại.";
    public JsonObject ParametersSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["key"] = new JsonObject { ["type"] = "string", ["description"] = "Tên ghi chú" },
            ["content"] = new JsonObject { ["type"] = "string", ["description"] = "Nội dung ghi chú" }
        },
        ["required"] = new JsonArray("key", "content"),
        ["additionalProperties"] = false
    };

    public async Task<string> ExecuteAsync(AgentToolContext ctx, JsonObject args, CancellationToken ct)
    {
        var key = ToolHelpers.GetString(args, "key").Trim();
        var content = ToolHelpers.GetString(args, "content").Trim();
        if (string.IsNullOrWhiteSpace(key)) return ToolHelpers.Json(new { error = "Thiếu key." });
        await _memory.WriteFactAsync(ctx.ThreadId, key, content, ct);
        return ToolHelpers.Json(new { ok = true, key });
    }
}

internal sealed class GetNoteTool : IAgentTool
{
    private readonly MemoryService _memory;
    public GetNoteTool(MemoryService memory) => _memory = memory;

    public string Name => "get_note";
    public string Description => "Đọc ghi chú đã lưu trong bộ nhớ phiên theo key (hoặc toàn bộ nếu bỏ trống).";
    public JsonObject ParametersSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["key"] = new JsonObject { ["type"] = "string", ["description"] = "Tên ghi chú (bỏ trống để xem tất cả)" }
        },
        ["additionalProperties"] = false
    };

    public async Task<string> ExecuteAsync(AgentToolContext ctx, JsonObject args, CancellationToken ct)
    {
        var blob = await _memory.ReadFactBlobAsync(ctx.ThreadId, ct);
        var key = ToolHelpers.GetString(args, "key").Trim();
        if (string.IsNullOrWhiteSpace(key))
            return ToolHelpers.Json(new { notes = blob });

        if (string.IsNullOrWhiteSpace(blob)) return ToolHelpers.Json(new { note = (string?)null, key });
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(blob) ?? new();
            return ToolHelpers.Json(new { note = dict.TryGetValue(key, out var v) ? v : null, key });
        }
        catch
        {
            return ToolHelpers.Json(new { note = (string?)null, key });
        }
    }
}