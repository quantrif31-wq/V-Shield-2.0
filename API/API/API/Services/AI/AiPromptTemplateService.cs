using System.Text;

namespace API.Services.AI;

public interface IAiPromptTemplateService
{
    /// <summary>
    /// Render prompt từ template key và parameters.
    /// Template có thể chứa {ParamName} để thay thế.
    /// </summary>
    string Render(string templateKey, int version, Dictionary<string, string> parameters);

    /// <summary>
    /// Lấy system instruction cho một loại phân tích cụ thể.
    /// </summary>
    string GetSystemInstruction(string analysisType);
}

public class AiPromptTemplateService : IAiPromptTemplateService
{
    private static readonly Dictionary<string, string> DefaultTemplates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["soc-incident-briefing"] = @"Phan tich su co bao mat dua tren du lieu sau:
- Alarm: {alarm_summary}
- Muc do: {severity}
- Thoi gian: {timestamp}
- Access logs lien quan: {access_logs}
- Thiet bi: {device_info}
- Khach/Phuong tien: {visitor_vehicle_info}

Yeu cau:
1. Tom tat 3-6 dong.
2. Timeline co timestamp.
3. Danh gia rui ro.
4. 3 hanh dong tiep theo de xuat.
5. Thieu bang chung/checklist.
6. Nguon du lieu.",
        ["evidence-analysis"] = @"Phan tich bang chung bao mat:
- Loai: {evidence_type}
- Nguon: {source}
- Thoi gian: {timestamp}
- Custody log: {custody_info}
- Legal hold: {legal_hold_info}
- Su co lien quan: {incident_info}

Yeu cau:
1. Timeline su co.
2. Danh sach bang chung quan trong.
3. Canh bao thieu chain-of-custody.
4. De xuat redaction.
5. Export risk checklist.",
        ["ueba-risk-explanation"] = @"Giai thich rui ro hanh vi nhan su:
- Nhan vien: {employee_info}
- Diem rui ro: {risk_score}
- Cac yeu to: {risk_factors}
- Duong co so dong nghiep: {peer_baseline}
- Lich su truy cap: {access_history}
- Thiet bi/Camera: {device_info}

Yeu cau:
1. Top risk factors.
2. So sanh voi dong nghiep.
3. De xuat hanh dong.",
        ["device-health-diagnosis"] = @"Chan doan suc khoe thiet bi:
- Ten: {device_name}
- Loai: {device_type}
- Trang thai: {status}
- Lan cuoi thay: {last_seen}
- Do tre: {latency}
- So lan restart: {restart_count}
- Ty le loi: {failure_rate}

Yeu cau:
1. Trang thai (Online/Degraded/Offline/Stale/AtRisk).
2. Van de du doan.
3. Hanh dong de xuat.",
        ["visitor-screening"] = @"Kiem tra rui ro khach/phuong tien:
- Ten: {visitor_name}
- Loai: {visitor_type}
- Muc dich: {purpose}
- Host: {host_info}
- Watchlist: {watchlist_info}
- Lich su: {history_info}
- Phuong tien: {vehicle_info}

Yeu cau:
1. Rui ro (thap/trung_binh/cao).
2. Ly do.
3. Dieu kien phe duyet.",
        ["policy-explanation"] = @"Giai thich chinh sach truy cap:
- Chinh sach: {policy_name}
- Trang thai: {policy_status}
- Mo ta: {change_summary}
- So luong rules: {total_rules} ({allow_rules} allow, {deny_rules} deny, {scheduled_rules} theo lich)
- Nguoi tao: {created_by}
- Thoi gian: tao {created_at}, submit {submitted_at}, duyet {approved_at}, kich hoat {activated_at}, retired {retired_at}

Yeu cau:
1. Muc dich chinh sach.
2. Ai bi anh huong.
3. Hanh dong tiep theo de xuat.
4. Giai thich bang ngon ngu tu nhien.",
        ["policy-simulation"] = @"Mo phong chinh sach truy cap truoc khi kich hoat:
- Chinh sach: {policy_name}
- Trang thai: {policy_status}
- Mo ta: {change_summary}
- So rules moi: {rule_count}
- So rules hien tai: {active_rule_count}
- Zone bi anh huong: {affected_zones}
- Nguoi dung bi anh huong: {affected_users}
- Xung duoc phat hien: {conflicts}
- Tong nhan vien: {total_employees}

Yeu cau:
1. Danh gia tac dong.
2. Canh bao conflict/xung dot.
3. Rui ro (thap/trung_binh/cao).
4. Khuyen nghi truoc khi kich hoat."
    };

    public string Render(string templateKey, int version, Dictionary<string, string> parameters)
    {
        if (!DefaultTemplates.TryGetValue(templateKey, out var template))
            template = "Phan tich du lieu bao mat: {input}";

        var result = new StringBuilder(template);
        foreach (var param in parameters)
        {
            result.Replace($"{{{param.Key}}}", param.Value ?? string.Empty);
        }

        return result.ToString();
    }

    public string GetSystemInstruction(string analysisType)
    {
        return analysisType switch
        {
            "soc" => "Ban la tro ly SOC. Phan tich su co bao mat dua tren du lieu he thong. Chi de xuat, khong tu quyet dinh hanh dong vat ly. Luon dan nguon bang chung.",
            "evidence" => "Ban la chuyen gia bang chung so. Phan tich tinh toan ven, xich custody, va tuan thu. Khong xoa/purge/export bang chung.",
            "ueba" => "Ban la chuyen gia UEBA. Giai thich rui ro hanh vi dua tren du lieu truy cap. Khong goi y ky luat nhan su.",
            "device" => "Ban la ky su thiet bi. Chan doan suc khoe thiet bi an ninh. Khong tu dong restart hoac thay doi cau hinh.",
            "visitor" => "Ban la nhan vien an ninh. Kiem tra rui ro khach ra vao. Chi blocking neu watchlist match hoac duplicate plate.",
            "policy" => "Ban la chuyen gia chinh sach. Giai thich tac dong cua chinh sach truy cap.",
            _ => "Ban la tro ly an ninh. Phan tich du lieu he thong va de xuat. Khong tu dong thuc thi hanh dong."
        };
    }
}
