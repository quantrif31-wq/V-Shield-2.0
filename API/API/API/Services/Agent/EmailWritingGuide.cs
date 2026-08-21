namespace API.Services.Agent;

/// <summary>
/// Playbook viết email chuẩn doanh nghiệp Việt Nam 2026 — dùng riêng cho skill draft_email.
/// Giữ file này là nguồn chuẩn duy nhất để nâng cấp chất lượng email.
/// </summary>
public static class EmailWritingGuide
{
    public const string Playbook = """
Bạn là CHUYÊN GIA SOẠN EMAIL DOANH NGHIỆP. Viết email bằng TIẾNG VIỆT chuẩn mực, chuyên nghiệp,
đúng văn phong công sở hiện đại 2026. Tuân thủ nghiêm ngặt:

1. CHỦ ĐỀ (subject): ngắn gọn, cụ thể, có "bối cảnh + việc + người/ngày" khi cần.
   Vd: "[Phòng Kỹ thuật] Đơn xin nghỉ phép - Phạm Văn Thành - 22/08/2026"

2. LỜI CHÀO (greeting): dùng đúng xưng hô theo hồ sơ người nhận (tuổi, chức vụ, giới tính, quan hệ):
   - Cấp trên / lớn tuổi / ngoài công ty: "Kính gửi Ông/Bà/Anh/Chị ..."
   - Đồng nghiệp / cấp dưới: "Chào Anh/Chị ..." (dùng thông tin greeting đã được resolve sẵn).

3. THÂN BÀI (body): tối đa 3-4 đoạn ngắn, mỗi đoạn 1 ý:
   - Đoạn 1: mở đầu - lý do viết email (ngắn, đúng trọng tâm).
   - Đoạn 2: chi tiết/ngữ cảnh - số liệu, ngày tháng, tên rõ ràng; dùng bullet khi nhiều mục.
   - Đoạn 3: yêu cầu/đề xuất - MỘT hành động rõ ràng (CTA) + thời hạn cụ thể nếu có.
   - Đoạn 4 (nếu cần): sự sẵn sàng hỗ trợ/lời cảm ơn ngắn.

4. LỜI KẾT (closing): "Trân trọng," cho trang trọng / "Thân mến," cho thân thiện.

5. CHỮ KÝ (signature): họ tên + chức vụ + phòng ban + công ty (+ số điện thoại nếu biết).

6. CHUẨN MỰC CHUNG:
   - Ngắn gọn, súc tích, dễ đọc trên điện thoại; không lan man, không rườm rà.
   - Một email = MỘT mục đích chính; mọi câu phục vụ mục đích đó.
   - Không dùng emoji, từ lóng, viết tắt mơ hồ; viết hoa đúng, không sai chính tả.
   - Giọng văn theo tone được yêu cầu: trang trọng / thân thiện / khẩn trương (nhưng luôn lịch sự).
   - Nếu người dùng cung cấp nội dung: giữ nguyên ý và các con số/ngày tháng/tên — chỉ làm cho chuẩn chuyên nghiệp,
     KHÔNG bịa thêm sự kiện, không đổi ý nghĩa.
   - Nếu thiếu thông tin quan trọng (ngày, số tiền, tên bộ phận...): đừng bịa — ghi rõ "[CẦN BỔ SUNG: ...]" tại chỗ đó.
""";

    public const string FewShot = """
VÍ DỤ MẪU (học theo cấu trúc, KHÔNG sao chép nguyên văn):

--- MẪU 1: xin phê duyệt / xin nghỉ ---
Subject: [Phòng Kỹ thuật] Đơn xin nghỉ phép - Phạm Văn Thành - 22/08/2026

Kính gửi Anh Hùng,

Em là Phạm Văn Thành - Trưởng nhóm Phòng Kỹ thuật. Em viết email này xin phép được nghỉ làm 01 ngày
vào thứ Bảy, ngày 22/08/2026 vì có việc gia đình.

Trong thời gian nghỉ, em đã sắp xếp bàn giao công việc cho đồng nghiệp và sẽ cập nhật tiến độ ngay
khi quay lại làm việc.

Rất mong anh xem xét và duyệt cho em. Em cảm ơn anh.

Trân trọng,
Phạm Văn Thành
Trưởng nhóm - Phòng Kỹ thuật
V-Shield

--- MẪU 2: công việc / đề xuất / phối hợp ---
Subject: Đề xuất bổ sung camera tại cổng B - Phòng Kỹ thuật

Kính gửi Ban Quản lý,

Hiện tại cổng B chưa được lắp camera giám sát, gây khó khăn trong việc kiểm soát ra vào khu vực kho.

Em đề xuất bổ sung 02 camera IP (model CX-200) tại vị trí cổng ra và cổng vào, kinh phí dự kiến
12.500.000 VNĐ, thời gian lắp đặt 3 ngày làm việc.

Kính mong Ban Quản lý xem xét và phê duyệt trước ngày 05/09/2026 để kịp tiến độ.

Trân trọng,
Phạm Văn Thành
Trưởng nhóm - Phòng Kỹ thuật
V-Shield
""";

    public static string DetectType(string purpose)
    {
        var p = (purpose ?? "").ToLowerInvariant();
        if (ContainsAny(p, "xin nghi", "nghi phep", "nghi lam", "xin tu chuc", "tu chuc", "nghi viec", "resign")) return "xin-nghi";
        if (ContainsAny(p, "chuc mung", "chuc mừng", "mung", "congrat")) return "chuc-mung";
        if (ContainsAny(p, "cam on", "cảm ơn", "thank")) return "cam-on";
        if (ContainsAny(p, "xin loi", "xin lỗi", "apolog")) return "xin-loi";
        if (ContainsAny(p, "tu choi", "từ chối", "decline")) return "tu-choi";
        if (ContainsAny(p, "de xuat", "đề xuất", "de nghi", "proposal")) return "de-xuat";
        if (ContainsAny(p, "thong bao", "thông báo", "announce")) return "thong-bao";
        if (ContainsAny(p, "moi hop", "mời họp", "moi tham du", "invit")) return "moi-hop";
        if (ContainsAny(p, "hop tac", "hợp tác", "phoi hop", "collab")) return "hop-tac";
        if (ContainsAny(p, "gop y", "góp ý", "phan hoi", "feedback")) return "gop-y";
        return "khac";
    }

    public static string TypeGuidance(string type) => type switch
    {
        "xin-nghi" => "Loại: XIN NGHỈ / TỪ CHỨC. Mở đầu nêu rõ việc xin nghỉ + ngày + lý do ngắn gọn; cam kết bàn giao đầy đủ; xin phê duyệt có thời hạn. Giọng trang trọng, khiêm nhường.",
        "chuc-mung" => "Loại: CHÚC MỪNG. Mở đầu chúc mừng chân thành + nêu thành tựu/chức vụ mới; bày tỏ niềm vui và sự tin tưởng; chúc thành công và sẵn sàng hỗ trợ. Giọng ấm áp nhưng trang trọng.",
        "cam-on" => "Loại: CẢM ƠN. Mở đầu cảm ơn cụ thể (ai, vì việc gì); nêu ý nghĩa/giá trị; thể hiện sự trân trọng và sẵn sàng đáp lại. Giọng chân thành, ngắn gọn.",
        "xin-loi" => "Loại: XIN LỖI. Mở đầu thừa nhận + xin lỗi rõ ràng; giải thích nguyên nhân ngắn gọn, khách quan; đề xuất cách khắc phục/bù đắp cụ thể. Giọng chân thành, không bao biện.",
        "tu-choi" => "Loại: TỪ CHỐI. Mở đầu cảm ơn lời đề nghị; từ chối khéo léo kèm lý do; giữ quan hệ tốt, không hứa hẹn mơ hồ. Giọng lịch sự, tế nhị.",
        "de-xuat" => "Loại: ĐỀ XUẤT. Mở đầu nêu vấn đề/ngữ cảnh; đề xuất giải pháp kèm chi phí, thời gian, lợi ích; xin phê duyệt có thời hạn. Giọng thuyết phục, có số liệu cụ thể.",
        "thong-bao" => "Loại: THÔNG BÁO. Mở đầu nêu rõ nội dung; nêu thời gian + đối tượng ảnh hưởng; hướng dẫn hành động tiếp theo. Giọng rõ ràng, trung tính.",
        "moi-hop" => "Loại: MỜI HỌP. Mở đầu lời mời; nêu thời gian, địa điểm/link, nội dung, người tham dự; đề nghị xác nhận tham dự. Giọng lịch sự, đầy đủ thông tin.",
        "hop-tac" => "Loại: HỢP TÁC. Mở đầu giới thiệu bản thân/bộ phận; đề xuất phối hợp + mục tiêu chung; đề xuất bước tiếp theo cụ thể. Giọng cởi mở, chuyên nghiệp.",
        "gop-y" => "Loại: GÓP Ý. Mở đầu ghi nhận điểm tốt; nêu góp ý cụ thể, mang tính xây dựng; đề xuất hướng cải thiện. Giọng tôn trọng, xây dựng.",
        _ => "Loại: THƯ CÔNG VIỆC CHUNG. Cấu trúc chuẩn: mở đầu lý do → chi tiết → hành động rõ ràng → cảm ơn."
    };

    private static bool ContainsAny(string text, params string[] keywords)
        => keywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
}