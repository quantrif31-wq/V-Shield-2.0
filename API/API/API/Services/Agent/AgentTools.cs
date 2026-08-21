using System.Text.Json;
using System.Text.Json.Nodes;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Agent;

/// <summary>ToÃ n bá»™ tool cá»§a AI agent (Skill).</summary>
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

    /// <summary>Danh sÃ¡ch tool schema theo Ä‘á»‹nh dáº¡ng OpenAI/DeepSeek.</summary>
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

    /// <summary>TÃªn gá»i (first name) tá»« há» tÃªn kiá»ƒu Viá»‡t Nam: láº¥y token cuá»‘i.</summary>
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
    public string Description => "Láº¥y há»“ sÆ¡ cá»§a ngÆ°á»i dÃ¹ng Ä‘ang Ä‘Äƒng nháº­p (ngÆ°á»i gá»­i email): mÃ£ nhÃ¢n viÃªn, há» tÃªn, chá»©c vá»¥, phÃ²ng ban, email, ngÃ y sinh, giá»›i tÃ­nh.";
    public JsonObject ParametersSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject(),
        ["additionalProperties"] = false
    };

    public async Task<string> ExecuteAsync(AgentToolContext ctx, JsonObject args, CancellationToken ct)
    {
        if (ctx.EmployeeId is null)
            return ToolHelpers.Json(new { error = "TÃ i khoáº£n hiá»‡n táº¡i khÃ´ng gáº¯n vá»›i há»“ sÆ¡ nhÃ¢n viÃªn.", employeeId = (int?)null });

        var emp = await ctx.Db.Employees
            .Include(e => e.Position).Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.EmployeeId == ctx.EmployeeId, ct);

        if (emp == null)
            return ToolHelpers.Json(new { error = "KhÃ´ng tÃ¬m tháº¥y há»“ sÆ¡ nhÃ¢n viÃªn.", employeeId = ctx.EmployeeId });

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
    public string Description => "TÃ¬m ngÆ°á»i (nhÃ¢n viÃªn hoáº·c khÃ¡ch má»i) theo tÃªn, mÃ£ nhÃ¢n viÃªn, CCCD, email hoáº·c Ä‘iá»‡n thoáº¡i. Tráº£ vá» tá»‘i Ä‘a 6 káº¿t quáº£ khá»›p â€” náº¿u cÃ³ nhiá»u á»©ng viÃªn, hÃ£y há»i ngÆ°á»i dÃ¹ng chá»n ai.";
    public JsonObject ParametersSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["query"] = new JsonObject { ["type"] = "string", ["description"] = "TÃªn / mÃ£ nhÃ¢n viÃªn / CCCD / email / Ä‘iá»‡n thoáº¡i cáº§n tÃ¬m" },
            ["kind"] = new JsonObject { ["type"] = "string", ["description"] = "employee hoáº·c visitor (tÃ¹y chá»n)" }
        },
        ["required"] = new JsonArray("query"),
        ["additionalProperties"] = false
    };

    public async Task<string> ExecuteAsync(AgentToolContext ctx, JsonObject args, CancellationToken ct)
    {
        var query = (ToolHelpers.GetString(args, "query") ?? "").Trim();
        var kind = (ToolHelpers.GetString(args, "kind") ?? "").Trim().ToLowerInvariant();
        if (query.Length < 2)
            return ToolHelpers.Json(new { error = "Tá»« khÃ³a tÃ¬m kiáº¿m quÃ¡ ngáº¯n." });

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
                    department = "KhÃ¡ch má»i",
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
    public string Description => "Láº¥y chi tiáº¿t há»“ sÆ¡ má»™t ngÆ°á»i theo personId vÃ  kind (employee/visitor). Bao gá»“m chá»©c vá»¥, phÃ²ng ban, ngÃ y sinh, giá»›i tÃ­nh, email; CCCD Ä‘áº§y Ä‘á»§ chá»‰ khi cÃ³ quyá»n.";
    public JsonObject ParametersSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["personId"] = new JsonObject { ["type"] = "integer", ["description"] = "ID ngÆ°á»i cáº§n xem" },
            ["kind"] = new JsonObject { ["type"] = "string", ["description"] = "employee hoáº·c visitor" }
        },
        ["required"] = new JsonArray("personId", "kind"),
        ["additionalProperties"] = false
    };

    public async Task<string> ExecuteAsync(AgentToolContext ctx, JsonObject args, CancellationToken ct)
    {
        var personId = args.TryGetPropertyValue("personId", out var id) && id is JsonValue jv ? jv.GetValue<int>() : 0;
        var kind = ToolHelpers.GetString(args, "kind").ToLowerInvariant();
        if (personId <= 0) return ToolHelpers.Json(new { error = "personId khÃ´ng há»£p lá»‡." });

        var sensitive = ToolAuthorizer.CanSeeSensitiveProfile(ctx);

        if (kind == "visitor")
        {
            var v = await ctx.Db.VisitorDetails.FirstOrDefaultAsync(x => x.VisitorDetailId == personId, ct);
            if (v == null) return ToolHelpers.Json(new { error = "KhÃ´ng tÃ¬m tháº¥y khÃ¡ch má»i." });
            return ToolHelpers.Json(new
            {
                personId = v.VisitorDetailId,
                kind = "visitor",
                fullName = v.FullName,
                cccd = sensitive ? v.IdCardNumber : Mask(v.IdCardNumber),
                companyEmail = v.CompanyEmail,
                department = "KhÃ¡ch má»i"
            });
        }

        var emp = await ctx.Db.Employees
            .Include(e => e.Position).Include(e => e.Department).Include(e => e.ManagerEmployee)
            .FirstOrDefaultAsync(e => e.EmployeeId == personId, ct);
        if (emp == null) return ToolHelpers.Json(new { error = "KhÃ´ng tÃ¬m tháº¥y nhÃ¢n viÃªn." });

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
    public string Description => "Tráº£ vá» quan há»‡ tá»• chá»©c giá»¯a ngÆ°á»i gá»­i (tÃ i khoáº£n hiá»‡n táº¡i) vÃ  ngÆ°á»i nháº­n: cÃ¹ng phÃ²ng ban, cÃ³ pháº£i quáº£n lÃ½ trá»±c tiáº¿p hay giÃ¡n tiáº¿p, chá»©c vá»¥ hai bÃªn.";
    public JsonObject ParametersSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["personId"] = new JsonObject { ["type"] = "integer", ["description"] = "ID ngÆ°á»i nháº­n (employee)" },
            ["kind"] = new JsonObject { ["type"] = "string", ["description"] = "employee hoáº·c visitor" }
        },
        ["required"] = new JsonArray("personId", "kind"),
        ["additionalProperties"] = false
    };

    public async Task<string> ExecuteAsync(AgentToolContext ctx, JsonObject args, CancellationToken ct)
    {
        var personId = args.TryGetPropertyValue("personId", out var id) && id is JsonValue jv ? jv.GetValue<int>() : 0;
        if (personId <= 0 || ctx.EmployeeId is null)
            return ToolHelpers.Json(new { error = "KhÃ´ng xÃ¡c Ä‘á»‹nh Ä‘Æ°á»£c quan há»‡.", note = "TÃ i khoáº£n hiá»‡n táº¡i chÆ°a gáº¯n nhÃ¢n viÃªn." });

        var sender = await ctx.Db.Employees
            .Include(e => e.Position).Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.EmployeeId == ctx.EmployeeId, ct);
        var recipient = await ctx.Db.Employees
            .Include(e => e.Position).Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.EmployeeId == personId, ct);

        if (sender == null || recipient == null)
            return ToolHelpers.Json(new { error = "KhÃ´ng tÃ¬m tháº¥y há»“ sÆ¡." });

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
    public string Description => "Chá»n cÃ¡ch xÆ°ng hÃ´ / chÃ o há»i phÃ¹ há»£p giá»¯a ngÆ°á»i gá»­i (tÃ i khoáº£n hiá»‡n táº¡i) vÃ  ngÆ°á»i nháº­n dá»±a trÃªn tuá»•i, chá»©c vá»¥, quan há»‡ vÃ  giá»›i tÃ­nh. Tráº£ vá» greeting, closing vÃ  chá»¯ kÃ½ gá»£i Ã½.";
    public JsonObject ParametersSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["personId"] = new JsonObject { ["type"] = "integer", ["description"] = "ID ngÆ°á»i nháº­n (employee)" },
            ["kind"] = new JsonObject { ["type"] = "string", ["description"] = "employee hoáº·c visitor" }
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
            "Nam" or "male" or "M" or "1" => formal ? "Ã”ng" : "Anh",
            "Ná»¯" or "female" or "F" or "0" or "2" => formal ? "BÃ " : "Chá»‹",
            _ => formal ? "Anh/Chá»‹" : "Anh/Chá»‹"
        };

        var greeting = formal ? $"KÃ­nh gá»­i {honorific} {firstName}," : $"ChÃ o {honorific} {firstName},";
        var closing = formal ? "TrÃ¢n trá»ng," : "ThÃ¢n máº¿n,";
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

// ---------- draft_email (skill soáº¡n email chuyÃªn nghiá»‡p) ----------
internal sealed class DraftEmailTool : IAgentTool
{
    private readonly AgentLlmClient _llm;
    // Model riÃªng cho BÆ¯á»šC SOáº N EMAIL (cháº¥t lÆ°á»£ng cao), agent chat váº«n dÃ¹ng flash.
    // Cáº¥u hÃ¬nh qua env VSHIELD_EMAIL_MODEL.
    private static readonly string EmailModel =
        Environment.GetEnvironmentVariable("VSHIELD_EMAIL_MODEL") ?? "deepseek-v4-pro";
    public DraftEmailTool(AgentLlmClient llm) => _llm = llm;

    public string Name => "draft_email";
    public string Description => "SOáº N EMAIL CHUáº¨N DOANH NGHIá»†P. Gá»i sau khi Ä‘Ã£ cÃ³ Ä‘á»§ thÃ´ng tin: ngÆ°á»i nháº­n (qua search_people/get_person) + cÃ¡ch xÆ°ng hÃ´ (qua resolve_greeting) + má»¥c Ä‘Ã­ch/ná»™i dung tá»« ngÆ°á»i dÃ¹ng. " +
        "Skill sáº½ tá»± viáº¿t thÃ¢n email chuyÃªn nghiá»‡p (chá»§ Ä‘á», lá»i chÃ o, thÃ¢n bÃ i, lá»i káº¿t, chá»¯ kÃ½) theo chuáº©n cÃ´ng sá»Ÿ 2026 vÃ  táº¡o báº£n nhÃ¡p (chÆ°a gá»­i). NgÆ°á»i dÃ¹ng sáº½ xem vÃ  báº¥m Gá»­i trÃªn mÃ n hÃ¬nh. " +
        "Truyá»n Ä‘áº§y Ä‘á»§: to, purpose (má»¥c Ä‘Ã­ch), content (ná»™i dung ngÆ°á»i dÃ¹ng cung cáº¥p - cÃ³ thá»ƒ rá»—ng), recipientInfo (há»“ sÆ¡/xÆ°ng hÃ´ ngÆ°á»i nháº­n), tone, contentMode (polish/verbatim).";

    public JsonObject ParametersSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["to"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" }, ["description"] = "Email ngÆ°á»i nháº­n" },
            ["purpose"] = new JsonObject { ["type"] = "string", ["description"] = "Má»¥c Ä‘Ã­ch email (vd: xin nghá»‰ phÃ©p, Ä‘á» xuáº¥t, xÃ¡c nháº­n...)" },
            ["content"] = new JsonObject { ["type"] = "string", ["description"] = "Ná»™i dung/Ã½ ngÆ°á»i dÃ¹ng cung cáº¥p (cÃ³ thá»ƒ rá»—ng; náº¿u user nÃ³i 'giá»¯ nguyÃªn' thÃ¬ báº¯t buá»™c truyá»n nguyÃªn vÄƒn + contentMode=verbatim)" },
            ["recipientInfo"] = new JsonObject { ["type"] = "string", ["description"] = "TÃ³m táº¯t ngÆ°á»i nháº­n: tÃªn, chá»©c vá»¥, phÃ²ng ban, giá»›i tÃ­nh/tuá»•i, lá»i chÃ o gá»£i Ã½ (tá»« resolve_greeting)" },
            ["recipientName"] = new JsonObject { ["type"] = "string", ["description"] = "TÃªn ngÆ°á»i nháº­n (náº¿u cÃ³) Ä‘á»ƒ dÃ¹ng trong lá»i chÃ o" },
            ["greeting"] = new JsonObject { ["type"] = "string", ["description"] = "Lá»i chÃ o gá»£i Ã½ tá»« resolve_greeting (vd 'KÃ­nh gá»­i BÃ  HÃ¹ng,') - dÃ¹ng Ä‘Ãºng" },
            ["tone"] = new JsonObject { ["type"] = "string", ["description"] = "trang-trong (máº·c Ä‘á»‹nh) / than-thien / khan-truong / trung-tinh" },
            ["contentMode"] = new JsonObject { ["type"] = "string", ["description"] = "polish (viáº¿t láº¡i cho chuáº©n) / verbatim (giá»¯ nguyÃªn ná»™i dung user)" },
            ["cc"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" }, ["description"] = "Email CC (tÃ¹y chá»n)" }
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
        var recipientName = ToolHelpers.GetString(args, "recipientName").Trim();
        var greetingSuggestion = ToolHelpers.GetString(args, "greeting").Trim();
        var tone = ToolHelpers.GetString(args, "tone").Trim();
        var contentMode = ToolHelpers.GetString(args, "contentMode").Trim().ToLowerInvariant();

        if (to.Length == 0) return ToolHelpers.Json(new { error = "ChÆ°a cÃ³ ngÆ°á»i nháº­n (to)." });
        if (string.IsNullOrWhiteSpace(purpose)) return ToolHelpers.Json(new { error = "ChÆ°a cÃ³ má»¥c Ä‘Ã­ch (purpose)." });

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

        // tone profile theo ngÆ°á»i dÃ¹ng (há»c tá»« cÃ¡c email Ä‘Ã£ gá»­i)
        var toneProfile = "";
        try
        {
            toneProfile = (await ctx.Db.SystemConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Key == $"agent.tone.{ctx.UserId}", ct))?.Value ?? "";
        }
        catch { }

        var emailType = EmailWritingGuide.DetectType(purpose);
        var typeGuidance = EmailWritingGuide.TypeGuidance(emailType);

        string subject;
        string bodyFull;

        if (contentMode == "verbatim" && !string.IsNullOrWhiteSpace(content))
        {
            // giá»¯ nguyÃªn ná»™i dung ngÆ°á»i dÃ¹ng
            subject = string.IsNullOrWhiteSpace(purpose) ? "Email cÃ´ng viá»‡c" : purpose;
            bodyFull = content;
        }
        else
        {
            var compose = await ComposeAsync(purpose, content, recipientInfo, recipientName, greetingSuggestion, tone, toneProfile, typeGuidance, senderName, senderPosition, senderDept, ct);
            if (compose == null)
            {
                // fallback: váº«n táº¡o nhÃ¡p cÃ³ Cáº¤U TRÃšC Ä‘áº§y Ä‘á»§ (chÃ o + ná»™i dung + káº¿t + chá»¯ kÃ½)
                subject = purpose;
                var fbGreeting = DefaultGreeting(greetingSuggestion, recipientName, recipientInfo);
                var fbBody = string.IsNullOrWhiteSpace(content) ? $"[Cáº¦N Bá»” SUNG Ná»˜I DUNG] - {purpose}" : content;
                bodyFull = string.Join("\n\n", new[] { fbGreeting, fbBody, "TrÃ¢n trá»ng,", BuildSignature(senderName, senderPosition, senderDept) }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
            }
            else
            {
                var body = compose.Body;
                // náº¿u body quÃ¡ ngáº¯n so vá»›i ná»™i dung ngÆ°á»i dÃ¹ng -> thá»­ viáº¿t láº¡i Ä‘áº§y Ä‘á»§ hÆ¡n 1 láº§n
                if ((string.IsNullOrWhiteSpace(body) || body.Length < 100) && !string.IsNullOrWhiteSpace(content))
                {
                    var retry = await ComposeAsync(purpose, content, recipientInfo, recipientName, greetingSuggestion, tone + ", viet day du hon, dai hon, nhieu doan hon", toneProfile, typeGuidance, senderName, senderPosition, senderDept, ct);
                    if (retry != null && !string.IsNullOrWhiteSpace(retry.Body) && retry.Body.Length > (body?.Length ?? 0))
                    {
                        compose = retry;
                        body = retry.Body;
                    }
                }
                // Äáº£m báº£o Ä‘á»§ 5 pháº§n â€” náº¿u model bá» sÃ³t thÃ¬ Ä‘iá»n default phÃ­a server
                var greeting = !string.IsNullOrWhiteSpace(compose.Greeting) ? compose.Greeting : DefaultGreeting(greetingSuggestion, recipientName, recipientInfo);
                var closing = !string.IsNullOrWhiteSpace(compose.Closing) ? compose.Closing : "TrÃ¢n trá»ng,";
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
        string purpose, string content, string recipientInfo, string recipientName, string greetingSuggestion,
        string tone, string toneProfile, string typeGuidance,
        string senderName, string senderPosition, string senderDept, CancellationToken ct)
    {
        var userPrompt =
            $"Nhiá»‡m vá»¥: viáº¿t má»™t email doanh nghiá»‡p Äáº¦Y Äá»¦ 5 PHáº¦N theo Ä‘Ãºng cáº¥u trÃºc máº«u.\n" +
            $"- {typeGuidance}\n" +
            $"- Má»¥c Ä‘Ã­ch: {purpose}\n" +
            $"- NgÆ°á»i gá»­i: {senderName} ({(string.IsNullOrWhiteSpace(senderPosition) ? "chÆ°a rÃµ chá»©c vá»¥" : senderPosition)})" +
            (string.IsNullOrWhiteSpace(senderDept) ? "" : $" - {senderDept}") + "\n" +
            (string.IsNullOrWhiteSpace(recipientName) ? "" : $"- TÃªn ngÆ°á»i nháº­n: {recipientName}\n") +
            (string.IsNullOrWhiteSpace(greetingSuggestion) ? "" : $"- Lá»i chÃ o Báº®T BUá»˜C DÃ™NG ÄÃšNG: \"{greetingSuggestion}\"\n") +
            (string.IsNullOrWhiteSpace(recipientInfo) ? "" : $"- ThÃ´ng tin ngÆ°á»i nháº­n: {recipientInfo}\n") +
            (string.IsNullOrWhiteSpace(content) ? "" : $"- Ná»™i dung ngÆ°á»i dÃ¹ng cung cáº¥p: {content}\n") +
            $"- Giá»ng vÄƒn: {tone}.\n" +
            (string.IsNullOrWhiteSpace(toneProfile) ? "" : $"- Giá»ng vÄƒn Ä‘Ã£ ghi nháº­n cá»§a ngÆ°á»i gá»­i: {toneProfile}\n") +
            "\nQUAN TRá»ŒNG: cáº£ 5 trÆ°á»ng subject, greeting, body, closing, signature Äá»€U Báº®T BUá»˜C KHÃ”NG ÄÆ¯á»¢C Rá»–NG. " +
            "Náº¿u cÃ³ 'Lá»i chÃ o Báº®T BUá»˜C DÃ™NG ÄÃšNG' thÃ¬ greeting pháº£i khá»›p chÃ­nh xÃ¡c. " +
            "body pháº£i lÃ  thÃ¢n bÃ i Ã­t nháº¥t 2-3 Ä‘oáº¡n ngáº¯n, cÃ³ lá»i chÃ o má»Ÿ Ä‘áº§u rÃµ rÃ ng. " +
            "Chá»‰ tráº£ vá» JSON Ä‘Ãºng Ä‘á»‹nh dáº¡ng: {\"subject\":\"...\",\"greeting\":\"...\",\"body\":\"...\",\"closing\":\"...\",\"signature\":\"...\"}. KhÃ´ng thÃªm gÃ¬ khÃ¡c.";

        var messages = new List<object>
        {
            new { role = "system", content = EmailWritingGuide.Playbook + "\n\n" + EmailWritingGuide.FewShot },
            new { role = "user", content = userPrompt }
        };

        var resp = await _llm.CompleteAsync(messages, null, maxTokens: 1400, model: EmailModel, cancellationToken: ct);
        if (resp.IsError || string.IsNullOrWhiteSpace(resp.Content)) return null;

        var parsed = TryParseJson(resp.Content);
        if (parsed == null)
        {
            // thá»­ láº¡i 1 láº§n vá»›i yÃªu cáº§u nghiÃªm ngáº·t hÆ¡n
            messages.Add(new { role = "assistant", content = resp.Content });
            messages.Add(new { role = "user", content = "Pháº£n há»“i trÆ°á»›c khÃ´ng Ä‘Ãºng Ä‘á»‹nh dáº¡ng JSON. Chá»‰ tráº£ vá» JSON Ä‘Ãºng Ä‘á»‹nh dáº¡ng nhÆ° Ä‘Ã£ yÃªu cáº§u." });
            resp = await _llm.CompleteAsync(messages, null, maxTokens: 1400, model: EmailModel, cancellationToken: ct);
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

    private static string DefaultGreeting(string greetingSuggestion, string recipientName, string recipientInfo)
    {
        if (!string.IsNullOrWhiteSpace(greetingSuggestion))
            return greetingSuggestion.EndsWith(",") ? greetingSuggestion : greetingSuggestion + ",";
        if (!string.IsNullOrWhiteSpace(recipientName))
            return $"KÃ­nh gá»­i {recipientName},";
        if (!string.IsNullOrWhiteSpace(recipientInfo))
        {
            // Æ°u tiÃªn lá»i chÃ o Ä‘Ã£ resolve (vd "KÃ­nh gá»­i BÃ  HÃ¹ng,") náº¿u cÃ³ trong recipientInfo
            var m = System.Text.RegularExpressions.Regex.Match(recipientInfo, @"(KÃ­nh gá»­i|ChÃ o|ThÆ°a)[^,\n]*");
            if (m.Success) return m.Value.TrimEnd() + ",";
        }
        return "KÃ­nh gá»­i,";
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

// ---------- notes (bá»™ nhá»› ngáº¯n háº¡n) ----------
internal sealed class SaveNoteTool : IAgentTool
{
    private readonly MemoryService _memory;
    public SaveNoteTool(MemoryService memory) => _memory = memory;

    public string Name => "save_note";
    public string Description => "Ghi láº¡i má»™t ghi chÃº ngáº¯n vÃ o bá»™ nhá»› cá»§a phiÃªn (key + ná»™i dung), dÃ¹ng Ä‘á»ƒ nhá»› thÃ´ng tin trong suá»‘t cuá»™c há»™i thoáº¡i.";
    public JsonObject ParametersSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["key"] = new JsonObject { ["type"] = "string", ["description"] = "TÃªn ghi chÃº" },
            ["content"] = new JsonObject { ["type"] = "string", ["description"] = "Ná»™i dung ghi chÃº" }
        },
        ["required"] = new JsonArray("key", "content"),
        ["additionalProperties"] = false
    };

    public async Task<string> ExecuteAsync(AgentToolContext ctx, JsonObject args, CancellationToken ct)
    {
        var key = ToolHelpers.GetString(args, "key").Trim();
        var content = ToolHelpers.GetString(args, "content").Trim();
        if (string.IsNullOrWhiteSpace(key)) return ToolHelpers.Json(new { error = "Thiáº¿u key." });
        await _memory.WriteFactAsync(ctx.ThreadId, key, content, ct);
        return ToolHelpers.Json(new { ok = true, key });
    }
}

internal sealed class GetNoteTool : IAgentTool
{
    private readonly MemoryService _memory;
    public GetNoteTool(MemoryService memory) => _memory = memory;

    public string Name => "get_note";
    public string Description => "Äá»c ghi chÃº Ä‘Ã£ lÆ°u trong bá»™ nhá»› phiÃªn theo key (hoáº·c toÃ n bá»™ náº¿u bá» trá»‘ng).";
    public JsonObject ParametersSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["key"] = new JsonObject { ["type"] = "string", ["description"] = "TÃªn ghi chÃº (bá» trá»‘ng Ä‘á»ƒ xem táº¥t cáº£)" }
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
