USE [AccessControlDB]
GO

-- =========================================================================================
-- SCRIPT SEED DATA: HỆ THỐNG KIỂM SOÁT RA VÀO THÔNG MINH (SMART ACCESS CONTROL)
-- NGÀY TẠO: 24/03/2026
-- =========================================================================================

-- =========================================================================================
-- 0. XÓA DỮ LIỆU CŨ THEO THỨ TỰ AN TOÀN (TRÁNH LỖI KHÓA NGOẠI)
-- =========================================================================================
DELETE FROM [Access_Log];
DELETE FROM [Visitor_Details];
DELETE FROM [Pre_Registration];
DELETE FROM [Registration_Links];
DELETE FROM [CameraPlates];
DELETE FROM [GuestProfile];
DELETE FROM [Vehicle];
DELETE FROM [EmployeeFaceModels];
DELETE FROM [EmployeeFaceVideos];
DELETE FROM [Employee];
DELETE FROM [Position];
DELETE FROM [Department];
DELETE FROM [Camera];
DELETE FROM [Gate];
DELETE FROM [Exception_Reason];
DELETE FROM [VehicleType];
GO

-- =========================================================================================
-- 1. MASTER DATA: PHÒNG BAN, CHỨC VỤ, CỔNG, CAMERA, LÝ DO NGOẠI LỆ, LOẠI XE
-- =========================================================================================

-- 1.1 Department (10 Phòng ban)
SET IDENTITY_INSERT [Department] ON;
INSERT INTO [Department] ([DepartmentId], [Name]) VALUES
(1, N'Ban Giám Đốc'), (2, N'Hành Chính Nhân Sự'), (3, N'Kế Toán - Tài Chính'), (4, N'Công Nghệ Thông Tin'),
(5, N'Kinh Doanh'), (6, N'Marketing'), (7, N'Chăm Sóc Khách Hàng'), (8, N'Vận Hành'),
(9, N'Bảo Trì & Kỹ Thuật'), (10, N'An Ninh & Giám Sát');
SET IDENTITY_INSERT [Department] OFF;
GO

-- 1.2 Position (12 Chức vụ)
SET IDENTITY_INSERT [Position] ON;
INSERT INTO [Position] ([PositionId], [Name]) VALUES
(1, N'Tổng Giám Đốc'), (2, N'Phó Tổng Giám Đốc'), (3, N'Giám Đốc IT'), (4, N'Trưởng Phòng'),
(5, N'Phó Phòng'), (6, N'Trưởng Nhóm (Leader)'), (7, N'Chuyên Viên Cấp Cao'), (8, N'Nhân Viên'),
(9, N'Thực Tập Sinh'), (10, N'Trưởng Ca Bảo Vệ'), (11, N'Nhân Viên An Ninh'), (12, N'Lễ Tân');
SET IDENTITY_INSERT [Position] OFF;
GO

-- 1.3 Gate (5 Cổng)
SET IDENTITY_INSERT [Gate] ON;
INSERT INTO [Gate] ([GateId], [GateName], [Location]) VALUES
(1, N'Cổng Chính Tòa Nhà', N'Mặt tiền đường Nguyễn Văn Trỗi'),
(2, N'Cổng Phụ Tòa Nhà', N'Mặt hẻm nội bộ phía sau'),
(3, N'Lối xuống Hầm B1', N'Dốc hầm B1 - Dành cho xe máy'),
(4, N'Lối lên Hầm B1', N'Dốc hầm B1 - Dành cho xe máy'),
(5, N'Lối xuống Hầm B2', N'Dốc hầm B2 - Dành cho ô tô');
SET IDENTITY_INSERT [Gate] OFF;
GO

-- 1.4 Camera (10 Camera, phân bổ theo cổng)
SET IDENTITY_INSERT [Camera] ON;
INSERT INTO [Camera] ([CameraId], [CameraName], [GateId], [CameraType]) VALUES
(1, N'CAM-MAIN-FACE-IN', 1, 'FACE_RECOGNITION'),
(2, N'CAM-MAIN-FACE-OUT', 1, 'FACE_RECOGNITION'),
(3, N'CAM-SIDE-MIX-IN', 2, 'MIXED_DUAL'),
(4, N'CAM-SIDE-MIX-OUT', 2, 'MIXED_DUAL'),
(5, N'CAM-B1-LPR-IN', 3, 'ALPR'),
(6, N'CAM-B1-FACE-IN', 3, 'FACE_RECOGNITION'),
(7, N'CAM-B1-LPR-OUT', 4, 'ALPR'),
(8, N'CAM-B1-FACE-OUT', 4, 'FACE_RECOGNITION'),
(9, N'CAM-B2-LPR-IN', 5, 'ALPR'),
(10, N'CAM-B2-LPR-OUT', 5, 'ALPR');
SET IDENTITY_INSERT [Camera] OFF;
GO

-- 1.5 Exception_Reason (9 Lý do ngoại lệ thực tế)
SET IDENTITY_INSERT [Exception_Reason] ON;
INSERT INTO [Exception_Reason] ([ReasonId], [ReasonCode], [Description]) VALUES
(1, 'FACE_MISMATCH', N'Khuôn mặt không khớp dữ liệu'),
(2, 'PLATE_MISMATCH', N'Biển số xe không khớp với người đăng ký'),
(3, 'NO_REGISTRATION', N'Khách không có thông tin đăng ký trước'),
(4, 'VIP_BYPASS', N'Khách VIP hoặc BGĐ đi xe khác - Mở thủ công'),
(5, 'SYSTEM_OFFLINE', N'Lỗi mạng hoặc hệ thống AI timeout'),
(6, 'FORGET_CARD_FACE_BLUR', N'Nhân viên quên thẻ, camera mờ do thời tiết'),
(7, 'TAILGATING', N'Bám đuôi xe trước vượt trạm'),
(8, 'WRONG_TIME', N'Đến ngoài khung giờ cho phép'),
(9, 'BLACKLISTED', N'Đối tượng nằm trong danh sách đen chặn cửa');
SET IDENTITY_INSERT [Exception_Reason] OFF;
GO

-- 1.6 VehicleType (6 Loại phương tiện)
SET IDENTITY_INSERT [VehicleType] ON;
INSERT INTO [VehicleType] ([VehicleTypeId], [TypeName]) VALUES
(1, N'Xe máy tay ga'), (2, N'Xe máy số'), (3, N'Ô tô 4 chỗ'), 
(4, N'Ô tô 7 chỗ'), (5, N'Xe bán tải (Pickup)'), (6, N'Xe máy điện');
SET IDENTITY_INSERT [VehicleType] OFF;
GO

-- =========================================================================================
-- 2. DỮ LIỆU NHÂN SỰ (EMPLOYEES) - 65 Nhân viên
-- =========================================================================================
SET IDENTITY_INSERT [Employee] ON;
INSERT INTO [Employee] ([EmployeeId], [DepartmentId], [PositionId], [FullName], [Phone], [Email], [FaceImageURL], [Status]) VALUES
(1, 1, 1, N'Phạm Nhật Vượng', '0901234567', 'vuongpn@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347337/vshield_access_control/faces/emp1.avif', 1),
(2, 1, 2, N'Nguyễn Thị Phương Thảo', '0912345678', 'thaontp@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347346/vshield_access_control/faces/emp2.avif', 1),
(3, 4, 3, N'Trần Tuấn Anh', '0987654321', 'anhtt@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347354/vshield_access_control/faces/emp3.avif', 1),
(4, 2, 4, N'Lê Minh Hằng', '0977111222', 'hanglm@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347365/vshield_access_control/faces/emp4.avif', 1),
(5, 3, 4, N'Vũ Quang Huy', '0933444555', 'huyvq@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347375/vshield_access_control/faces/emp5.avif', 1),
(6, 5, 4, N'Đặng Lê Nguyên Vũ', '0909999888', 'vudln@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347385/vshield_access_control/faces/emp6.avif', 1),
(7, 6, 4, N'Hoàng Thanh Trúc', '0911222333', 'trucht@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347391/vshield_access_control/faces/emp7.avif', 1),
(8, 7, 4, N'Phan Trọng Đạt', '0922333444', 'datpt@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347391/vshield_access_control/faces/emp8.avif', 1),
(9, 8, 4, N'Ngô Quý Đôn', '0944555666', 'donnq@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347392/vshield_access_control/faces/emp9.avif', 1),
(10, 9, 4, N'Lý Thường Kiệt', '0966777888', 'kietlt@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347337/vshield_access_control/faces/emp10.avif', 1),
(11, 10, 10, N'Bùi Xuân Huấn', '0988999000', 'huanbx@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347338/vshield_access_control/faces/emp11.avif', 1),
(12, 10, 11, N'Trần Hạo Nam', '0900111222', 'namth@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347339/vshield_access_control/faces/emp12.avif', 1),
(13, 10, 11, N'Lê Quốc Cường', '0934567890', 'cuonglq@congty.vn', NULL, 1),
(14, 2, 12, N'Nguyễn Ngọc Trinh', '0912349876', 'trinhnn@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347340/vshield_access_control/faces/emp14.avif', 1),
(15, 2, 12, N'Trần Khởi My', '0987651234', 'mytk@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347341/vshield_access_control/faces/emp15.avif', 1),
(16, 4, 6, N'Phan Hoàng Khải', '0908123456', 'khaiph@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347342/vshield_access_control/faces/emp16.avif', 1), -- Cấp quản lý IT
(17, 4, 7, N'Đinh Tiến Dũng', '0918234567', 'dungdt@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347343/vshield_access_control/faces/emp17.avif', 1),
(18, 4, 8, N'Vương Đình Huệ', '0928345678', 'huevd@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347344/vshield_access_control/faces/emp18.avif', 1),
(19, 4, 8, N'Lưu Đức Hoa', '0938456789', 'hoald@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347345/vshield_access_control/faces/emp19.avif', 1),
(20, 4, 9, N'Châu Tinh Trì', NULL, 'tricht@congty.vn', NULL, 1),
(21, 5, 6, N'Lê Hoàng Tôn', '0948567890', 'tonlh@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347347/vshield_access_control/faces/emp21.avif', 1),
(22, 5, 8, N'Trần Tùng Anh', '0958678901', 'anhtt2@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347347/vshield_access_control/faces/emp22.avif', 1),
(23, 5, 8, N'Nguyễn Đức Tiến', '0968789012', 'tiennd@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347348/vshield_access_control/faces/emp23.avif', 1),
(24, 5, 8, N'Hoàng Thùy Linh', '0978890123', 'linhht@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347349/vshield_access_control/faces/emp24.avif', 1),
(25, 5, 8, N'Đỗ Mỹ Linh', '0988901234', 'linhdm@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347350/vshield_access_control/faces/emp25.avif', 1),
(26, 6, 6, N'Hồ Ngọc Hà', '0998012345', 'hahn@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347351/vshield_access_control/faces/emp26.avif', 1),
(27, 6, 8, N'Mai Phương Thúy', '0907123456', 'thuymp@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347352/vshield_access_control/faces/emp27.avif', 1),
(28, 6, 8, N'Đặng Thu Thảo', '0917234567', 'thaodt@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347353/vshield_access_control/faces/emp28.avif', 1),
(29, 3, 6, N'Trần Đình Long', '0927345678', 'longtd@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347353/vshield_access_control/faces/emp29.avif', 1),
(30, 3, 8, N'Lê Phước Vũ', '0937456789', 'vulp@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347355/vshield_access_control/faces/emp30.avif', 1),
(31, 3, 8, N'Trương Gia Bình', '0947567890', 'binhtg@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347356/vshield_access_control/faces/emp31.avif', 1),
(32, 7, 6, N'Nguyễn Thanh Tùng', '0957678901', 'tungnt@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347357/vshield_access_control/faces/emp32.avif', 1),
(33, 7, 8, N'Lê Minh Sơn', '0967789012', 'sonlm@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347358/vshield_access_control/faces/emp33.avif', 1),
(34, 7, 8, N'Hoàng Quốc Việt', '0977890123', 'viethq@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347360/vshield_access_control/faces/emp34.avif', 1),
(35, 7, 8, N'Vũ Khắc Tiệp', '0987901234', 'tiepvk@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347361/vshield_access_control/faces/emp35.avif', 0), -- Đã nghỉ việc
(36, 8, 6, N'Nguyễn Tử Quảng', '0997012345', 'quangnt@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347362/vshield_access_control/faces/emp36.avif', 1),
(37, 8, 8, N'Đoàn Nguyên Đức', '0906123456', 'ducdn@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347363/vshield_access_control/faces/emp37.avif', 1),
(38, 8, 8, N'Phạm Thu Hương', '0916234567', 'huongpt@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347364/vshield_access_control/faces/emp38.avif', 1),
(39, 9, 6, N'Trần Bá Dương', '0926345678', 'duongtb@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347365/vshield_access_control/faces/emp39.avif', 1),
(40, 9, 8, N'Nguyễn Đăng Quang', '0936456789', 'quangnd@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347366/vshield_access_control/faces/emp40.avif', 1),
(41, 4, 8, N'Ngô Kiến Huy', NULL, NULL, 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347367/vshield_access_control/faces/emp41.avif', 1),
(42, 4, 8, N'Trấn Thành', '0956678901', 'thanht@congty.vn', NULL, 1),
(43, 5, 8, N'Trường Giang', '0966789012', 'giangt@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347369/vshield_access_control/faces/emp43.avif', 1),
(44, 5, 8, N'Mạc Văn Khoa', '0976890123', 'khoamv@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347370/vshield_access_control/faces/emp44.avif', 1),
(45, 6, 8, N'Lâm Vỹ Dạ', '0986901234', 'dalv@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347370/vshield_access_control/faces/emp45.avif', 1),
(46, 6, 8, N'Ninh Dương Lan Ngọc', '0996012345', 'ngocndl@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347371/vshield_access_control/faces/emp46.avif', 1),
(47, 7, 8, N'Phạm Hương', '0905123456', 'huongp@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347372/vshield_access_control/faces/emp47.avif', 1),
(48, 7, 8, N'Hương Giang', '0915234567', 'giangh@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347373/vshield_access_control/faces/emp48.avif', 1),
(49, 8, 8, N'Minh Hằng', '0925345678', 'hangm@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347374/vshield_access_control/faces/emp49.avif', 1),
(50, 8, 8, N'Thủy Tiên', '0935456789', 'tient@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347375/vshield_access_control/faces/emp50.avif', 1),
(51, 9, 8, N'Công Vinh', '0945567890', 'vinhc@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347377/vshield_access_control/faces/emp51.avif', 0), -- Đã nghỉ việc
(52, 9, 8, N'Quang Hải', '0955678901', 'haiq@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347378/vshield_access_control/faces/emp52.avif', 1),
(53, 2, 8, N'Tiến Linh', '0965789012', 'linht@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347379/vshield_access_control/faces/emp53.avif', 1),
(54, 2, 8, N'Văn Hậu', '0975890123', 'hauv@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347380/vshield_access_control/faces/emp54.avif', 1),
(55, 3, 8, N'Xuân Trường', '0985901234', 'truongx@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347380/vshield_access_control/faces/emp55.avif', 1),
(56, 3, 8, N'Tuấn Anh', '0995012345', 'anht@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347381/vshield_access_control/faces/emp56.avif', 1),
(57, 4, 8, N'Công Phượng', '0904123456', 'phuongc@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347382/vshield_access_control/faces/emp57.avif', 1),
(58, 4, 8, N'Văn Toàn', '0914234567', 'toanv@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347383/vshield_access_control/faces/emp58.avif', 1),
(59, 5, 8, N'Đặng Văn Lâm', '0924345678', 'lamdv@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347384/vshield_access_control/faces/emp59.avif', 1),
(60, 5, 8, N'Bùi Tiến Dũng', '0934456789', 'dungbt@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347385/vshield_access_control/faces/emp60.avif', 1),
(61, 6, 8, N'Quế Ngọc Hải', '0944567890', 'haiqn@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347386/vshield_access_control/faces/emp61.avif', 1),
(62, 6, 8, N'Đỗ Hùng Dũng', '0954678901', 'dungdh@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347387/vshield_access_control/faces/emp62.avif', 1),
(63, 7, 8, N'Phan Văn Đức', '0964789012', 'ducpv@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347388/vshield_access_control/faces/emp63.avif', 1),
(64, 7, 8, N'Nguyễn Văn Toản', '0974890123', 'toannv@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347389/vshield_access_control/faces/emp64.avif', 1),
(65, 10, 11, N'Phạm Văn Mach', '0984901234', 'machpv@congty.vn', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347390/vshield_access_control/faces/emp65.avif', 1);
SET IDENTITY_INSERT [Employee] OFF;
GO


-- =========================================================================================
-- 4. DỮ LIỆU NHẬN DIỆN KHUÔN MẶT (FACE ASSETS)
-- =========================================================================================
SET IDENTITY_INSERT [EmployeeFaceModels] ON;
INSERT INTO [EmployeeFaceModels] ([Id], [EmployeeId], [ModelFileName], [ModelPath], [CreatedAt]) VALUES
(1, 1, 'emp_1_v1.dat', '/models/faces/emp_1_v1.dat', '2026-01-10'),
(2, 2, 'emp_2_v1.dat', '/models/faces/emp_2_v1.dat', '2026-01-10'),
(3, 3, 'emp_3_v1.dat', '/models/faces/emp_3_v1.dat', '2026-01-10'),
(4, 16, 'emp_16_v2_optimized.dat', '/models/faces/emp_16_v2_optimized.dat', '2026-02-05'),
(5, 17, 'emp_17_v1.dat', '/models/faces/emp_17_v1.dat', '2026-01-15'),
(6, 18, 'emp_18_v1.dat', '/models/faces/emp_18_v1.dat', '2026-01-15'),
(7, 21, 'emp_21_v1.dat', '/models/faces/emp_21_v1.dat', '2026-01-15');
SET IDENTITY_INSERT [EmployeeFaceModels] OFF;

SET IDENTITY_INSERT [EmployeeFaceVideos] ON;
INSERT INTO [EmployeeFaceVideos] ([Id], [EmployeeId], [FileName], [FilePath], [FileSize], [CreatedAt]) VALUES
(1, 1, 'video_enroll_1.mp4', '/videos/enroll/video_enroll_1.mp4', 15400200, '2026-01-10'),
(2, 2, 'video_enroll_2.mp4', '/videos/enroll/video_enroll_2.mp4', 16200100, '2026-01-10'),
(3, 16, 'video_enroll_16_360.mp4', '/videos/enroll/video_enroll_16_360.mp4', 18500000, '2026-02-05');
SET IDENTITY_INSERT [EmployeeFaceVideos] OFF;
GO

-- =========================================================================================
-- 5. PHƯƠNG TIỆN CỦA NHÂN VIÊN (VEHICLES) - 50 Xe
-- =========================================================================================
SET IDENTITY_INSERT [Vehicle] ON;
INSERT INTO [Vehicle] ([VehicleId], [LicensePlate], [VehicleTypeId], [EmployeeId], [Description], [ParkingStatus]) VALUES
(1, '51H-123.45', 4, 1, N'Lexus LX600 Đen', 'OUT'),
(2, '51G-999.99', 3, 2, N'Maybach S680 Trắng', 'OUT'),
(3, '59A-456.78', 3, 3, N'Camry 2.5Q Đen', 'IN'),
(4, '59E1-123.45', 2, 4, N'Honda Wave Alpha Đỏ', 'OUT'),
(5, '59S2-567.89', 1, 5, N'Honda SH 150i Xám', 'IN'),
(6, '60B-111.22', 4, 6, N'Ford Everest Đen', 'OUT'),
(7, '59C-333.44', 5, 7, N'Ford Ranger Raptor Cam', 'OUT'),
(8, '59X1-222.33', 1, 8, N'Vespa Primavera Trắng', 'IN'),
(9, '59Y2-444.55', 1, 9, N'Honda Vision Xanh', 'OUT'),
(10, '59Z3-666.77', 2, 10, N'Yamaha Sirius Đỏ Đen', 'OUT'),
(11, '59A1-888.99', 1, 11, N'Honda AirBlade Đen', 'IN'),
(12, '59B2-000.11', 2, 12, N'Honda Winner X', 'OUT'),
(13, '59C3-222.33', 2, 13, N'Yamaha Exciter 155', 'IN'),
(14, '59D4-444.55', 1, 14, N'Honda Lead Bạc', 'OUT'),
(15, '59E5-666.77', 1, 15, N'Yamaha Grande Trắng', 'IN'),
(16, '60A-888.99', 3, 16, N'Mazda 3 Trắng', 'IN'),
(17, '60B1-123.45', 1, 16, N'Honda SH Mode Đỏ (Xe vợ)', 'OUT'), -- Khải có 2 xe
(18, '59G1-234.56', 2, 17, N'Honda Future Xanh', 'OUT'),
(19, '59H2-345.67', 1, 18, N'Honda PCX Đen', 'IN'),
(20, '59K3-456.78', 2, 19, N'Yamaha Jupiter', 'OUT'),
(21, '59L4-567.89', 1, 21, N'Honda SH 350i Thể thao', 'OUT'),
(22, '59M5-678.90', 2, 22, N'Honda Blade', 'IN'),
(23, '59N1-789.01', 1, 23, N'Yamaha NVX', 'OUT'),
(24, '59P2-890.12', 1, 24, N'Piaggio Liberty Trắng', 'IN'),
(25, '59S3-901.23', 1, 25, N'Honda Vision Cà phê', 'OUT'),
(26, '51K-555.66', 3, 26, N'Mercedes C300 Trắng', 'IN'),
(27, '59T4-012.34', 1, 27, N'Vespa Sprint Vàng', 'OUT'),
(28, '59V5-123.45', 1, 28, N'Honda Lead Đỏ', 'IN'),
(29, '51F-222.33', 4, 29, N'Vinfast VF8 Xanh', 'OUT'),
(30, '59X1-345.67', 1, 30, N'Honda Vario Đen', 'IN'),
(31, '51H-444.55', 3, 31, N'BMW 320i Đen', 'OUT'),
(32, '59Y2-567.89', 2, 32, N'Honda Wave RSX', 'IN'),
(33, '59Z3-678.90', 1, 33, N'Honda Airblade 2022', 'OUT'),
(34, '59A1-789.01', 1, 34, N'Yamaha Janus', 'IN'),
(35, '59B2-890.12', 1, 36, N'Honda SH 150i ABS', 'OUT'),
(36, '51G-901.23', 4, 37, N'Lexus RX350 Trắng', 'IN'),
(37, '59C3-012.34', 1, 38, N'Honda Vision Đen nhám', 'OUT'),
(38, '51K-123.45', 3, 39, N'Porsche Macan Đỏ', 'IN'),
(39, '59D4-234.56', 2, 40, N'Yamaha Sirius Fi', 'OUT'),
(40, '59E5-345.67', 1, 41, N'Honda SH Mode Xanh', 'IN'),
(41, '51H-567.89', 3, 42, N'Kia Carnival Trắng', 'OUT'),
(42, '59G1-678.90', 1, 43, N'Yamaha Grande Đen', 'IN'),
(43, '59H2-789.01', 2, 44, N'Honda Future Đỏ', 'OUT'),
(44, '59K3-890.12', 1, 45, N'Honda Lead Vàng', 'IN'),
(45, '59L4-901.23', 1, 46, N'Vespa GTS Đen', 'OUT'),
(46, '59M5-012.34', 1, 47, N'Honda Vision Trắng', 'IN'),
(47, '59N1-123.45', 1, 48, N'Yamaha NVX Đỏ', 'OUT'),
(48, '59P2-234.56', 1, 49, N'Piaggio Medley Bạc', 'IN'),
(49, '59S3-345.67', 1, 50, N'Honda SH 125i Đỏ', 'OUT'),
(50, '60C-999.88', 5, 52, N'Ford Ranger Wildtrak', 'OUT');
SET IDENTITY_INSERT [Vehicle] OFF;
GO

-- =========================================================================================
-- 6. HỒ SƠ KHÁCH HÀNG / ĐỐI TÁC (GUEST PROFILES) - 35 Khách
-- =========================================================================================
SET IDENTITY_INSERT [GuestProfile] ON;
INSERT INTO [GuestProfile] ([GuestId], [FullName], [Phone], [DefaultLicensePlate], [FaceImageURL]) VALUES
(1, N'Trương Mỹ Lan', '0901112233', '51G-111.11', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347392/vshield_access_control/guest/g1.avif'),
(2, N'Nguyễn Thái Luyện', '0902223344', '60A-222.22', NULL),
(3, N'Tân Hoàng Minh', '0903334455', '29A-333.33', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347400/vshield_access_control/guest/g3.avif'),
(4, N'Quyết Còi', '0904445566', '30F-444.44', NULL),
(5, N'Nguyễn Phương Hằng', '0905556677', '61A-555.55', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347401/vshield_access_control/guest/g5.avif'),
(6, N'Huỳnh Uy Dũng', '0906667788', '61A-666.66', NULL),
(7, N'Shipper ShopeeFood 1', '0981111111', '59S1-111.11', NULL),
(8, N'Shipper Grab 1', '0982222222', '59G1-222.22', NULL),
(9, N'Shipper Baemin 1', '0983333333', '59B1-333.33', NULL),
(10, N'Nhân viên VNPT (Bảo trì mạng)', '0911123123', '59V1-123.45', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347393/vshield_access_control/guest/g10.avif'),
(11, N'Ứng viên Phỏng vấn IT 1', '0933111222', '59U1-111.22', NULL),
(12, N'Ứng viên Phỏng vấn Marketing 1', '0933222333', '59U2-222.33', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347394/vshield_access_control/guest/g12.avif'),
(13, N'Đối tác Samsung', '0909123123', '51H-777.88', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347395/vshield_access_control/guest/g13.avif'),
(14, N'Đối tác FPT', '0988123123', '29H-888.99', NULL),
(15, N'Khách hàng VIP 1', '0900000001', '51K-999.00', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347395/vshield_access_control/guest/g15.avif'),
(16, N'Khách hàng VIP 2', '0900000002', '51K-999.01', NULL),
(17, N'Người nhà NV 1', '0944111222', '59N1-111.22', NULL),
(18, N'Người nhà NV 2', '0944222333', '59N2-222.33', NULL),
(19, N'Giao hàng Tiki', '0977111111', '59T1-111.11', NULL),
(20, N'Giao nước bình LaVie', '0966111111', '59L1-111.11', NULL),
(21, N'Bảo trì máy lạnh Panasonic', '0955111111', '59P1-111.11', NULL),
(22, N'Đối tác quảng cáo Goldsun', '0901239876', '51F-123.98', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347396/vshield_access_control/guest/g22.avif'),
(23, N'Kiểm toán viên KPMG 1', '0912348765', '51G-234.87', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347397/vshield_access_control/guest/g23.avif'),
(24, N'Kiểm toán viên KPMG 2', '0923457654', '51H-345.76', NULL),
(25, N'Nhân viên ngân hàng VCB', '0934566543', '51K-456.65', NULL),
(26, N'Ứng viên Phỏng vấn Sale', '0945675432', '59S1-567.54', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347398/vshield_access_control/guest/g26.avif'),
(27, N'Đại diện cơ quan thuế', '0956784321', '51A-678.43', NULL),
(28, N'Luật sư công ty', '0967893210', '51H-789.32', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347399/vshield_access_control/guest/g28.avif'),
(29, N'Bảo trì thang máy Schindler', '0978902109', '59T1-890.21', NULL),
(30, N'Dịch vụ dọn dẹp vệ sinh', '0989011098', '59V1-901.10', NULL),
(31, N'Khách tham quan 1', '0990120987', '59K1-012.09', NULL),
(32, N'Khách tham quan 2', '0901231098', '59K2-123.10', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347400/vshield_access_control/guest/g32.avif'),
(33, N'Đối tác sự kiện', '0912342109', '51F-234.21', NULL),
(34, N'Nhà cung cấp văn phòng phẩm', '0923453210', '59N1-345.32', NULL),
(35, N'Giao hoa tươi', '0934564321', '59H1-456.43', NULL);
SET IDENTITY_INSERT [GuestProfile] OFF;
GO

-- =========================================================================================
-- 7. ĐĂNG KÝ TRƯỚC (PRE-REGISTRATIONS) & CHI TIẾT KHÁCH ĐI CÙNG
-- Bao gồm quá khứ, hiện tại và tương lai gần (Tháng 2, Tháng 3, Tháng 4 / 2026)
-- Current Context Date: 2026-03-24
-- =========================================================================================
SET IDENTITY_INSERT [Pre_Registration] ON;
INSERT INTO [Pre_Registration] ([RegistrationId], [GuestId], [HostEmployeeId],[ExpectedTimeIn], [ExpectedTimeOut], [Status], [NumberOfVisitors], [CreatedAt]) VALUES
-- Quá khứ (Đã hoàn thành / Hết hạn / Hủy)
(1, 1, 1,'2026-03-10 09:00:00', '2026-03-10 11:00:00', 'COMPLETED', 1, '2026-03-08 14:00:00'),
(2, 2, 5,'2026-03-12 14:00:00', '2026-03-12 16:00:00', 'COMPLETED', 1, '2026-03-11 09:30:00'),
(3, 11, 16,'2026-03-15 08:30:00', '2026-03-15 10:30:00', 'COMPLETED', 1, '2026-03-13 16:00:00'),
(4, 13, 1,'2026-03-18 10:00:00', '2026-03-18 12:00:00', 'COMPLETED', 3, '2026-03-15 10:00:00'), -- Khách đoàn 3 người
(5, 4, 3,'2026-03-19 09:00:00', '2026-03-19 11:00:00', 'EXPIRED', 1, '2026-03-18 08:00:00'), -- No show
(6, 12, 6,'2026-03-20 14:00:00', '2026-03-20 15:30:00', 'COMPLETED', 1, '2026-03-19 11:00:00'),
(7, 23, 31,'2026-03-22 08:00:00', '2026-03-22 17:00:00', 'COMPLETED', 2, '2026-03-20 15:00:00'), -- Đoàn 2 người
(8, 28, 1, '2026-03-23 13:30:00', '2026-03-23 15:00:00', 'COMPLETED', 1, '2026-03-22 09:00:00'),
(9, 10, 16, '2026-03-23 15:00:00', '2026-03-23 17:00:00', 'CANCELLED', 1, '2026-03-23 08:00:00'),

-- Hiện tại (Ngày 2026-03-24)
(10, 14, 16,'2026-03-24 09:00:00', '2026-03-24 11:30:00', 'APPROVED', 2, '2026-03-23 14:00:00'),
(11, 26, 6, '2026-03-24 10:00:00', '2026-03-24 11:00:00', 'APPROVED', 1, '2026-03-22 10:00:00'),
(12, 15, 5,  '2026-03-24 14:00:00', '2026-03-24 16:00:00', 'APPROVED', 1, '2026-03-24 08:30:00'),
(13, 31, 2,  '2026-03-24 15:00:00', '2026-03-24 16:30:00', 'PENDING', 4, '2026-03-24 09:00:00'), -- Đoàn 4 người, chờ duyệt

-- Tương lai gần
(14, 27, 31,  '2026-03-25 08:30:00', '2026-03-25 11:30:00', 'APPROVED', 1, '2026-03-20 16:00:00'),
(15, 33, 6, '2026-03-26 14:00:00', '2026-03-26 17:00:00', 'APPROVED', 2, '2026-03-23 10:30:00'),
(16, 11, 16,  '2026-03-27 09:00:00', '2026-03-27 10:00:00', 'APPROVED', 1, '2026-03-24 11:00:00'), -- Ứng viên quay lại vòng 2
(17, 3, 1,  '2026-03-28 10:00:00', '2026-03-28 12:00:00', 'APPROVED', 1, '2026-03-24 14:00:00'),
(18, 5, 2,  '2026-03-30 09:30:00', '2026-03-30 11:00:00', 'PENDING', 1, '2026-03-24 14:30:00'),
(19, 16, 5,  '2026-04-02 14:00:00', '2026-04-02 16:00:00', 'PENDING', 1, '2026-03-24 15:00:00'),
(20, 22, 6,  '2026-04-05 10:00:00', '2026-04-05 12:00:00', 'PENDING', 3, '2026-03-24 15:05:00');
SET IDENTITY_INSERT [Pre_Registration] OFF;

-- Insert Visitor Details cho các đoàn khách
SET IDENTITY_INSERT [Visitor_Details] ON;
INSERT INTO [Visitor_Details] ([VisitorDetailId], [RegistrationId], [FullName], [IdCardNumber], [ExpectedFaceImage]) VALUES
-- Reg 4 (Đối tác Samsung - 3 người)
(1, 4, N'Lee Min Ho', 'KOR123456', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347402/vshield_access_control/guest/reg4_1.avif'),
(2, 4, N'Park Seo Joon', 'KOR654321', NULL),
(3, 4, N'Nguyễn Văn Phiên Dịch', '079090123456', NULL),
-- Reg 7 (Kiểm toán KPMG - 2 người)
(4, 7, N'Trần Thị Kiểm Toán 1', '001190111222', 'https://res.cloudinary.com/dzczte86h/image/upload/v1774347403/vshield_access_control/guest/reg7_1.avif'),
(5, 7, N'Lê Văn Kiểm Toán 2', '001190333444', NULL),
-- Reg 10 (Đối tác FPT - 2 người)
(6, 10, N'Hoàng Công Nghệ', '079088111222', NULL),
(7, 10, N'Phạm Phần Mềm', '079088333444', NULL),
-- Reg 13 (Khách tham quan - 4 người)
(8, 13, N'Khách Đoàn 1', '011111111111', NULL),
(9, 13, N'Khách Đoàn 2', '022222222222', NULL),
(10, 13, N'Khách Đoàn 3', '033333333333', NULL),
(11, 13, N'Khách Đoàn 4', '044444444444', NULL),
-- Reg 15 (Đối tác sự kiện - 2 người)
(12, 15, N'Đạo Diễn Sự Kiện', '055555555555', NULL),
(13, 15, N'Trợ Lý Sự Kiện', '066666666666', NULL),
-- Reg 20 (Đối tác quảng cáo - 3 người)
(14, 20, N'Trưởng Nhóm Quảng Cáo', '077777777777', NULL),
(15, 20, N'Quay Phim 1', '088888888888', NULL),
(16, 20, N'Quay Phim 2', '099999999999', NULL);
SET IDENTITY_INSERT [Visitor_Details] OFF;
GO

-- =========================================================================================
-- 8. LINK ĐĂNG KÝ TỰ ĐỘNG (REGISTRATION LINKS)
-- =========================================================================================
SET IDENTITY_INSERT [Registration_Links] ON;
INSERT INTO [Registration_Links] ([LinkId], [Token], [HostEmployeeId], [ExpiredAt], [IsUsed], [CreatedAt]) VALUES
(1, 'A1B2C3D4E5F6G7H8I9J0K1L2M3N4O5P6', 1, '2026-03-01', 1, '2026-02-25'),
(2, 'Z9Y8X7W6V5U4T3S2R1Q0P9O8N7M6L5K4', 16, '2026-03-15', 1, '2026-03-10'),
(3, 'QWERTYUIOPASDFGHJKLZXCVBNM123456', 5, '2026-03-20', 1, '2026-03-18'),
(4, '1234567890MNBVCXZLKJHGFDSAPOIUYT', 1, '2026-03-25', 0, '2026-03-23'), -- Còn hạn, chưa dùng
(5, 'PLOKIJUHYGTFRDESWAQ1234567890ZXO', 31, '2026-03-26', 0, '2026-03-24'), -- Còn hạn, chưa dùng
(6, 'MNBVCXZASDFGHJKLPOIUYTREWQ098765', 16, '2026-03-30', 0, '2026-03-24'), -- Cho ứng viên
(7, 'ZXCVBNMASDFGHJKLQWERTYUIOP098765', 6, '2026-03-22', 0, '2026-03-15');  -- Đã hết hạn, chưa dùng
SET IDENTITY_INSERT [Registration_Links] OFF;
GO

-- =========================================================================================
-- 9. CAMERA PLATES (DỮ LIỆU LIVE STREAMING GIẢ LẬP)
-- =========================================================================================
INSERT INTO [CameraPlates] ([CameraIP], [PlateNumber], [X1], [Y1], [X2], [Y2], [LastUpdate]) VALUES
('192.168.1.105', '59A-456.78', 120, 340, 280, 420, '2026-03-24 15:07:15'), -- Cam B1 IN đang soi biển số
('192.168.1.107', '51G-999.99', 450, 500, 600, 580, '2026-03-24 15:07:18'), -- Cam B1 OUT
('192.168.1.109', '60B-111.22', 100, 200, 250, 300, '2026-03-24 15:07:20'), -- Cam B2 IN
('192.168.1.110', NULL, 0, 0, 0, 0, '2026-03-24 15:07:21'), -- Cam B2 OUT không có xe
('192.168.1.103', '59S1-111.11', 200, 400, 350, 480, '2026-03-24 15:07:19'); -- Cam Phụ IN đang thấy xe shipper
GO

-- =========================================================================================
-- 10. NHẬT KÝ RA VÀO (ACCESS LOGS) - PHẦN QUAN TRỌNG NHẤT
-- Bao phủ kịch bản: Hợp lệ, Ngoại lệ, Khách có hẹn, Khách vãng lai, Bypass, Tailgating
-- =========================================================================================
SET IDENTITY_INSERT [Access_Log] ON;
INSERT INTO [Access_Log] ([LogId], [Timestamp], [Direction], [GateId], [CameraId], [CapturedLicensePlate], [CapturedFaceImageURL], [EmployeeId], [RegistrationId], [ResultStatus], [IsBypass], [ExceptionReasonId], [Note], [EntryLogId]) VALUES
-- ===============================================================================
-- NGÀY 2026-03-20: CÁC KỊCH BẢN ĐI LÀM BÌNH THƯỜNG SÁNG & CHIỀU
-- ===============================================================================
-- 07:15 - 08:30: Lượt IN buổi sáng
(1, '2026-03-20 07:15:22', 'IN', 5, 9, '51H-123.45', '/logs/faces/20260320_071522.jpg', 1, NULL, 'ALLOWED', 0, NULL, NULL, NULL), -- GĐ vào sớm (B2)
(2, '2026-03-20 07:30:10', 'IN', 5, 9, '51G-999.99', '/logs/faces/20260320_073010.jpg', 2, NULL, 'ALLOWED', 0, NULL, NULL, NULL),
(3, '2026-03-20 07:45:05', 'IN', 3, 5, '59A-456.78', '/logs/faces/20260320_074505.jpg', 3, NULL, 'ALLOWED', 0, NULL, NULL, NULL), -- B1 IN
(4, '2026-03-20 07:46:12', 'IN', 3, 5, '59E1-123.45', '/logs/faces/20260320_074612.jpg', 4, NULL, 'ALLOWED', 0, NULL, NULL, NULL),
(5, '2026-03-20 07:50:33', 'IN', 3, 5, '59S2-567.89', '/logs/faces/20260320_075033.jpg', 5, NULL, 'ALLOWED', 0, NULL, NULL, NULL),
(6, '2026-03-20 07:55:00', 'IN', 5, 9, '60B-111.22', '/logs/faces/20260320_075500.jpg', 6, NULL, 'ALLOWED', 0, NULL, NULL, NULL),
(7, '2026-03-20 07:58:15', 'IN', 3, 5, '59X1-222.33', '/logs/faces/20260320_075815.jpg', 8, NULL, 'ALLOWED', 0, NULL, NULL, NULL),
(8, '2026-03-20 08:02:40', 'IN', 3, 5, '59A1-888.99', '/logs/faces/20260320_080240.jpg', 11, NULL, 'ALLOWED', 0, NULL, NULL, NULL),
(9, '2026-03-20 08:15:10', 'IN', 3, 5, '60A-888.99', '/logs/faces/20260320_081510.jpg', 16, NULL, 'ALLOWED', 0, NULL, NULL, NULL), -- IT Lead Khải vào

-- 17:00 - 18:30: Lượt OUT buổi chiều (Map EntryLogId)
(10, '2026-03-20 17:05:22', 'OUT', 4, 7, '59E1-123.45', '/logs/faces/20260320_170522.jpg', 4, NULL, 'ALLOWED', 0, NULL, NULL, 4),
(11, '2026-03-20 17:30:15', 'OUT', 4, 7, '59A-456.78', '/logs/faces/20260320_173015.jpg', 3, NULL, 'ALLOWED', 0, NULL, NULL, 3),
(12, '2026-03-20 17:35:40', 'OUT', 5, 10, '51H-123.45', '/logs/faces/20260320_173540.jpg', 1, NULL, 'ALLOWED', 0, NULL, NULL, 1),
(13, '2026-03-20 17:45:00', 'OUT', 5, 10, '51G-999.99', '/logs/faces/20260320_174500.jpg', 2, NULL, 'ALLOWED', 0, NULL, NULL, 2),
(14, '2026-03-20 18:00:10', 'OUT', 4, 7, '59S2-567.89', '/logs/faces/20260320_180010.jpg', 5, NULL, 'ALLOWED', 0, NULL, NULL, 5),
(15, '2026-03-20 18:05:33', 'OUT', 5, 10, '60B-111.22', '/logs/faces/20260320_180533.jpg', 6, NULL, 'ALLOWED', 0, NULL, NULL, 6),
(16, '2026-03-20 18:15:20', 'OUT', 4, 7, '59X1-222.33', '/logs/faces/20260320_181520.jpg', 8, NULL, 'ALLOWED', 0, NULL, NULL, 7),
(17, '2026-03-20 19:30:00', 'OUT', 4, 7, '60A-888.99', '/logs/faces/20260320_193000.jpg', 16, NULL, 'ALLOWED', 0, NULL, NULL, 9), -- Khải OT về trễ

-- KỊCH BẢN NGOẠI LỆ TRONG NGÀY 2026-03-20
-- Nhân viên 13 (Không có Face Image) quẹt thẻ/hoặc không nhận diện được mặt -> Bypass
(18, '2026-03-20 08:20:00', 'IN', 3, 5, '59C3-222.33', '/logs/faces/20260320_082000_unrec.jpg', 13, NULL, 'MANUAL_APPROVED', 1, 1, N'Đeo khẩu trang che kín mặt', NULL),
(19, '2026-03-20 17:50:00', 'OUT', 4, 7, '59C3-222.33', '/logs/faces/20260320_175000_unrec.jpg', 13, NULL, 'MANUAL_APPROVED', 1, 1, N'Bảo vệ mở thủ công', 18),

-- Khách có đăng ký trước (Reg 6 - 59U2-222.33, Expected 14:00 - 15:30)
(20, '2026-03-20 13:50:15', 'IN', 1, 1, '59U2-222.33', '/logs/faces/20260320_135015.jpg', NULL, 6, 'ALLOWED', 0, NULL, N'Đến sớm 10 phút, hợp lệ', NULL),
(21, '2026-03-20 15:45:30', 'OUT', 1, 2, '59U2-222.33', '/logs/faces/20260320_154530.jpg', NULL, 6, 'ALLOWED', 0, NULL, N'Ra trễ 15 phút, hợp lệ', 20),

-- Đoàn Kiểm toán (Reg 7 - 51G-234.87, Đoàn 2 người)
(22, '2026-03-20 07:55:00', 'IN', 1, 1, '51G-234.87', '/logs/faces/20260320_075500_kpmg.jpg', NULL, 7, 'ALLOWED', 0, NULL, N'Đoàn kiểm toán', NULL),
(23, '2026-03-20 17:10:00', 'OUT', 1, 2, '51G-234.87', '/logs/faces/20260320_171000_kpmg.jpg', NULL, 7, 'ALLOWED', 0, NULL, N'Đoàn kiểm toán về', 22),

-- Shipper giao hàng không hẹn trước (Từ chối)
(24, '2026-03-20 10:30:00', 'IN', 2, 3, '59S1-111.11', '/logs/faces/20260320_103000.jpg', NULL, NULL, 'DENIED', 0, 3, N'Shipper ShopeeFood không có hẹn, yêu cầu đỗ ngoài', NULL),

-- ===============================================================================
-- NGÀY 2026-03-21 (THỨ BẢY - ÍT NHÂN VIÊN ĐI LÀM)
-- ===============================================================================
(25, '2026-03-21 08:00:00', 'IN', 5, 9, '51H-123.45', '/logs/faces/20260321_080000.jpg', 1, NULL, 'ALLOWED', 0, NULL, NULL, NULL), -- GĐ vẫn đi làm
(26, '2026-03-21 08:30:15', 'IN', 3, 5, '60A-888.99', '/logs/faces/20260321_083015.jpg', 16, NULL, 'ALLOWED', 0, NULL, NULL, NULL), -- Khải IT trực
(27, '2026-03-21 12:00:00', 'OUT', 5, 10, '51H-123.45', '/logs/faces/20260321_120000.jpg', 1, NULL, 'ALLOWED', 0, NULL, NULL, 25), -- GĐ về sớm
(28, '2026-03-21 17:00:00', 'OUT', 4, 7, '60A-888.99', '/logs/faces/20260321_170000.jpg', 16, NULL, 'ALLOWED', 0, NULL, NULL, 26),

-- KỊCH BẢN NGOẠI LỆ: TAILGATING (Bám đuôi)
(29, '2026-03-21 08:30:18', 'IN', 3, 5, '59UNK-000', '/logs/faces/20260321_083018_tailgate.jpg', NULL, NULL, 'EXCEPTION', 0, 7, N'Xe lạ bám đuôi xe anh Khải IT lúc barrier chưa đóng', NULL),

-- ===============================================================================
-- NGÀY 2026-03-23 (THỨ HAI)
-- ===============================================================================
(30, '2026-03-23 07:20:00', 'IN', 5, 9, '51H-123.45', '/logs/faces/20260323_0720.jpg', 1, NULL, 'ALLOWED', 0, NULL, NULL, NULL),
(31, '2026-03-23 07:35:00', 'IN', 5, 9, '51G-999.99', '/logs/faces/20260323_0735.jpg', 2, NULL, 'ALLOWED', 0, NULL, NULL, NULL),
(32, '2026-03-23 07:45:00', 'IN', 3, 5, '59A-456.78', '/logs/faces/20260323_0745.jpg', 3, NULL, 'ALLOWED', 0, NULL, NULL, NULL),
(33, '2026-03-23 07:50:00', 'IN', 3, 5, '59S2-567.89', '/logs/faces/20260323_0750.jpg', 5, NULL, 'ALLOWED', 0, NULL, NULL, NULL),
(34, '2026-03-23 08:00:00', 'IN', 3, 5, '60A-888.99', '/logs/faces/20260323_0800.jpg', 16, NULL, 'ALLOWED', 0, NULL, NULL, NULL),

-- KỊCH BẢN NGOẠI LỆ: Biển số bị mờ / Đọc sai
(35, '2026-03-23 08:10:00', 'IN', 3, 5, '59Z3-666.11', '/logs/faces/20260323_0810.jpg', 10, NULL, 'MISMATCH', 0, 2, N'Biển thực tế là 59Z3-666.77 nhưng AI đọc sai thành 11', NULL),
(36, '2026-03-23 08:10:30', 'IN', 3, 5, '59Z3-666.77', '/logs/faces/20260323_0810.jpg', 10, NULL, 'MANUAL_APPROVED', 1, 2, N'Bảo vệ mở bù, sửa lại biển số', NULL),

-- KỊCH BẢN NGOẠI LỆ: Khách đến sai ngày/giờ
(37, '2026-03-23 10:00:00', 'IN', 1, 1, '29H-888.99', '/logs/faces/20260323_1000.jpg', NULL, 10, 'DENIED', 0, 8, N'Lịch hẹn là ngày 24/03 nhưng khách đến 23/03', NULL),

-- KỊCH BẢN: Nhân viên đi xe khác biển số đã đăng ký
(38, '2026-03-23 11:00:00', 'IN', 3, 5, '59ABC-999', '/logs/faces/20260323_1100.jpg', 8, NULL, 'MISMATCH', 0, 2, N'Anh Đạt đi xe mượn', NULL),
(39, '2026-03-23 11:00:20', 'IN', 3, 5, '59ABC-999', '/logs/faces/20260323_1100.jpg', 8, NULL, 'MANUAL_APPROVED', 1, 2, N'Xác nhận đúng người, bảo vệ mở', NULL),

-- Lượt về buổi chiều 23/03
(40, '2026-03-23 17:35:00', 'OUT', 5, 10, '51H-123.45', '/logs/faces/20260323_1735.jpg', 1, NULL, 'ALLOWED', 0, NULL, NULL, 30),
(41, '2026-03-23 17:40:00', 'OUT', 5, 10, '51G-999.99', '/logs/faces/20260323_1740.jpg', 2, NULL, 'ALLOWED', 0, NULL, NULL, 31),
(42, '2026-03-23 17:45:00', 'OUT', 4, 7, '59A-456.78', '/logs/faces/20260323_1745.jpg', 3, NULL, 'ALLOWED', 0, NULL, NULL, 32),
(43, '2026-03-23 18:00:00', 'OUT', 4, 7, '59S2-567.89', '/logs/faces/20260323_1800.jpg', 5, NULL, 'ALLOWED', 0, NULL, NULL, 33),
(44, '2026-03-23 18:15:00', 'OUT', 4, 7, '59Z3-666.77', '/logs/faces/20260323_1815.jpg', 10, NULL, 'ALLOWED', 0, NULL, NULL, 36),
(45, '2026-03-23 18:20:00', 'OUT', 4, 7, '59ABC-999', '/logs/faces/20260323_1820.jpg', 8, NULL, 'MANUAL_APPROVED', 1, 2, N'Xe mượn ra', 39),
-- Lưu ý: Nhân viên 16 (Khải) không có log OUT -> Đây là case qua đêm (Overnight Parking) / Quên quẹt thẻ lúc ra

-- ===============================================================================
-- NGÀY 2026-03-24 (HIỆN TẠI)
-- ===============================================================================
(46, '2026-03-24 07:15:00', 'IN', 5, 9, '51H-123.45', '/logs/faces/20260324_0715.jpg', 1, NULL, 'ALLOWED', 0, NULL, NULL, NULL),
(47, '2026-03-24 07:30:00', 'IN', 5, 9, '51G-999.99', '/logs/faces/20260324_0730.jpg', 2, NULL, 'ALLOWED', 0, NULL, NULL, NULL),
(48, '2026-03-24 07:50:00', 'IN', 3, 5, '59S2-567.89', '/logs/faces/20260324_0750.jpg', 5, NULL, 'ALLOWED', 0, NULL, NULL, NULL),

-- KỊCH BẢN NGOẠI LỆ: BLACKLIST
(49, '2026-03-24 08:15:00', 'IN', 2, 3, '59XXX-999', '/logs/faces/20260324_0815.jpg', NULL, NULL, 'DENIED', 0, 9, N'Đối tượng từng gây rối, biển số nằm trong blacklist', NULL),

-- Khách đến đúng lịch hẹn (Reg 10, 11)
(50, '2026-03-24 08:55:00', 'IN', 1, 1, '29H-888.99', '/logs/faces/20260324_0855.jpg', NULL, 10, 'ALLOWED', 0, NULL, N'Đối tác FPT đến', NULL),
(51, '2026-03-24 09:55:00', 'IN', 1, 1, '59S1-567.54', '/logs/faces/20260324_0955.jpg', NULL, 11, 'ALLOWED', 0, NULL, N'Ứng viên phỏng vấn', NULL),

-- Khách về
(52, '2026-03-24 11:15:00', 'OUT', 1, 2, '59S1-567.54', '/logs/faces/20260324_1115.jpg', NULL, 11, 'ALLOWED', 0, NULL, N'Ứng viên phỏng vấn xong', 51),
(53, '2026-03-24 11:45:00', 'OUT', 1, 2, '29H-888.99', '/logs/faces/20260324_1145.jpg', NULL, 10, 'ALLOWED', 0, NULL, N'Đối tác FPT ra', 50),

-- Khách VIP (Reg 12 - 51K-999.00)
(54, '2026-03-24 13:50:00', 'IN', 1, 1, '51K-999.00', '/logs/faces/20260324_1350.jpg', NULL, 12, 'ALLOWED', 0, NULL, N'Khách VIP vào', NULL),

-- Thêm một số log giả lập cho khung giờ hiện tại (Gần 15:00)
(55, '2026-03-24 14:50:00', 'OUT', 5, 10, '51G-999.99', '/logs/faces/20260324_1450.jpg', 2, NULL, 'ALLOWED', 0, NULL, N'Phó TĐG ra ngoài công tác', 47),
(56, '2026-03-24 15:00:00', 'IN', 2, 3, '59S1-111.11', '/logs/faces/20260324_1500.jpg', NULL, NULL, 'MANUAL_APPROVED', 1, 4, N'Shipper để CMND lại, bảo vệ cho vào', NULL);
SET IDENTITY_INSERT [Access_Log] OFF;
GO

-- =========================================================================================
-- ĐỒNG BỘ LẠI TRẠNG THÁI PARKING (Dựa vào log mới nhất)
-- =========================================================================================
UPDATE [Vehicle] SET [ParkingStatus] = 'OUT';

-- Set IN cho những xe có log IN cuối cùng mà chưa có OUT
UPDATE v
SET v.[ParkingStatus] = 'IN'
FROM [Vehicle] v
INNER JOIN (
    SELECT CapturedLicensePlate
    FROM [Access_Log]
    WHERE LogId IN (
        SELECT MAX(LogId) 
        FROM [Access_Log] 
        WHERE CapturedLicensePlate IS NOT NULL 
        GROUP BY CapturedLicensePlate
    )
    AND Direction = 'IN'
) as LastLogs ON v.LicensePlate = LastLogs.CapturedLicensePlate;
GO