using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace API.Services;

[ExcludeFromCodeCoverage]
public static class DemoDataSeeder
{
    private const string DemoCompanyCode = "VSHIELD-DEMO";

    public static void EnsureSeeded(IServiceProvider services, IConfiguration configuration, IHostEnvironment environment)
    {
        var enabled = configuration.GetValue("DemoData:Enabled", true);
        var allowProduction = configuration.GetValue("DemoData:AllowInProduction", false);

        if (!enabled)
        {
            Console.WriteLine("[INFO] Demo data seeding is disabled.");
            return;
        }

        if (environment.IsProduction() && !allowProduction)
        {
            Console.WriteLine("[WARN] Demo data seeding skipped in Production. Set DemoData:AllowInProduction=true to override.");
            return;
        }

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (db.Companies.AsNoTracking().Any(item => item.Code == DemoCompanyCode))
        {
            ReconcileOperationalDemoData(db);
            EnsureUebaDemoData(db);
            Console.WriteLine("[INFO] Demo data already exists; operational users and QR-centric sample data reconciled.");
            return;
        }

        SeedDefaultNotificationRules(db);
        Seed(db);
        ReconcileOperationalDemoData(db);
        EnsureUebaDemoData(db);
        Console.WriteLine("[OK] Demo data seeded for medium/large company scenario.");
    }

    private static void SeedDefaultNotificationRules(ApplicationDbContext db)
    {
        if (db.NotificationRules.Any()) return;

        db.NotificationRules.AddRange(
            // === ALARMS ===
            new NotificationRule { EventType = "Alarm.Duress", SeverityMin = "Critical", RecipientRole = "BaoVe", NotifyWeb = true, NotifyMobile = true },
            new NotificationRule { EventType = "Alarm.Duress", SeverityMin = "Critical", RecipientRole = "Admin", NotifyWeb = true, NotifyMobile = true },
            new NotificationRule { EventType = "Alarm.EmergencyPass", SeverityMin = "Critical", RecipientRole = "BaoVe", NotifyWeb = true, NotifyMobile = true },
            new NotificationRule { EventType = "Alarm.EmergencyPass", SeverityMin = "Critical", RecipientRole = "Admin", NotifyWeb = true, NotifyMobile = true },
            new NotificationRule { EventType = "Alarm.DeviceOffline", SeverityMin = "High", RecipientRole = "BaoVe", NotifyWeb = true, NotifyMobile = false },
            new NotificationRule { EventType = "Alarm.VisitorOverstay", SeverityMin = "Medium", RecipientRole = "BaoVe", NotifyWeb = true, NotifyMobile = false },
            new NotificationRule { EventType = "Alarm.Generic", SeverityMin = "Critical", RecipientRole = "BaoVe", NotifyWeb = true, NotifyMobile = true },
            new NotificationRule { EventType = "Alarm.Generic", SeverityMin = "Critical", RecipientRole = "Admin", NotifyWeb = true, NotifyMobile = true },
            new NotificationRule { EventType = "Alarm.Generic", SeverityMin = "High", RecipientRole = "BaoVe", NotifyWeb = true, NotifyMobile = false },
            // === APPROVALS ===
            new NotificationRule { EventType = "Approval.LeaveRequest.Submitted", RecipientRole = "Admin", NotifyWeb = true, NotifyMobile = false },
            new NotificationRule { EventType = "Approval.LeaveRequest.Submitted", RecipientRole = "QuanLy", NotifyWeb = true, NotifyMobile = false },
            new NotificationRule { EventType = "Approval.LeaveRequest.Submitted", RecipientRole = "NhanSu", NotifyWeb = true, NotifyMobile = false },
            new NotificationRule { EventType = "Approval.Intervention.Created", RecipientRole = "Admin", NotifyWeb = true, NotifyMobile = true },
            new NotificationRule { EventType = "Approval.Intervention.Created", RecipientRole = "QuanLy", NotifyWeb = true, NotifyMobile = true },
            new NotificationRule { EventType = "Approval.LostFoundClaim.Created", RecipientRole = "Admin", NotifyWeb = true, NotifyMobile = false },
            new NotificationRule { EventType = "Approval.EvidenceExport.Created", RecipientRole = "Admin", NotifyWeb = true, NotifyMobile = false },
            new NotificationRule { EventType = "Approval.EvidenceRedaction.Created", RecipientRole = "Admin", NotifyWeb = true, NotifyMobile = false }
        );
        db.SaveChanges();
    }

    public static object ResetOperationalScenarios(ApplicationDbContext db)
    {
        db.OperationalInterventionRequests.RemoveRange(db.OperationalInterventionRequests);
        db.EmergencyPasses.RemoveRange(db.EmergencyPasses);
        db.LaneEvents.RemoveRange(db.LaneEvents.Where(item =>
            item.EventType == "MANUAL_PASS" ||
            item.EventType == "EMERGENCY_PASS" ||
            item.EventType == "INTERVENTION_EXECUTED" ||
            item.EventType == "ESCALATION_REQUEST"));
        db.Alarms.RemoveRange(db.Alarms.Where(item =>
            item.AlarmType == "DeviceHealth" ||
            item.AlarmType == "DeviceOffline" ||
            item.AlarmType == "EmergencyPass"));
        db.EvidenceItems.RemoveRange(db.EvidenceItems.Where(item => item.StorageReference.StartsWith("demo://")));
        db.SecurityDevices.RemoveRange(db.SecurityDevices.Where(item => item.SerialNumber != null && item.SerialNumber.StartsWith("DEMO-")));
        db.SaveChanges();

        var employees = db.Employees.Include(item => item.Department).OrderBy(item => item.EmployeeId).ToList();
        EnsureEnterpriseDemoScenarios(db, employees, DateTime.UtcNow);
        db.SaveChanges();

        return new
        {
            interventionRequests = db.OperationalInterventionRequests.Count(),
            securityDevices = db.SecurityDevices.Count(item => item.SerialNumber != null && item.SerialNumber.StartsWith("DEMO-")),
            alarms = db.Alarms.Count(),
            evidenceItems = db.EvidenceItems.Count(item => item.StorageReference.StartsWith("demo://"))
        };
    }

    private static void Seed(ApplicationDbContext db)
    {
        var now = DateTime.UtcNow;
        var random = new Random(20260613);

        var company = new Company
        {
            Name = "Tập đoàn Sản xuất V-Shield",
            Code = DemoCompanyCode,
            IsActive = true,
            CreatedAtUtc = now.AddMonths(-18)
        };
        db.Companies.Add(company);
        db.SaveChanges();

        var sites = new[]
        {
            new Site { CompanyId = company.CompanyId, Name = "Trụ sở chính - Hà Nội", Code = "HN-HQ", Address = "Cầu Giấy, Hà Nội", Latitude = 21.028511m, Longitude = 105.804817m, CreatedAtUtc = now.AddMonths(-18) },
            new Site { CompanyId = company.CompanyId, Name = "Khu nhà máy - Bắc Ninh", Code = "BN-FAC", Address = "VSIP Bắc Ninh", Latitude = 21.186111m, Longitude = 106.076389m, CreatedAtUtc = now.AddMonths(-16) },
            new Site { CompanyId = company.CompanyId, Name = "Trung tâm logistics - Hải Phòng", Code = "HP-LOG", Address = "Đình Vũ, Hải Phòng", Latitude = 20.866667m, Longitude = 106.684722m, CreatedAtUtc = now.AddMonths(-12) }
        };
        db.Sites.AddRange(sites);
        db.SaveChanges();

        var buildings = new List<Building>();
        var buildingCoords = new Dictionary<string, (decimal lat, decimal lng)>
        {
            ["HN-HQ-ADM"] = (21.0286m, 105.8049m),
            ["HN-HQ-OPS"] = (21.0287m, 105.8052m),
            ["BN-FAC-ADM"] = (21.1862m, 106.0764m),
            ["BN-FAC-OPS"] = (21.1860m, 106.0760m),
            ["HP-LOG-ADM"] = (20.8668m, 106.6848m),
            ["HP-LOG-OPS"] = (20.8665m, 106.6845m),
        };
        foreach (var site in sites)
        {
            var admCode = $"{site.Code}-ADM";
            var opsCode = $"{site.Code}-OPS";
            var (admLat, admLng) = buildingCoords.GetValueOrDefault(admCode, (site.Latitude ?? 0, site.Longitude ?? 0));
            var (opsLat, opsLng) = buildingCoords.GetValueOrDefault(opsCode, (site.Latitude ?? 0, site.Longitude ?? 0));
            buildings.Add(new Building { SiteId = site.SiteId, Name = $"{site.Code} Hành chính", Code = admCode, Latitude = admLat, Longitude = admLng });
            buildings.Add(new Building { SiteId = site.SiteId, Name = $"{site.Code} Vận hành", Code = opsCode, Latitude = opsLat, Longitude = opsLng });
        }
        db.Buildings.AddRange(buildings);
        db.SaveChanges();

        var floors = new List<FacilityFloor>();
        foreach (var building in buildings)
        {
            for (var floor = 1; floor <= 3; floor++)
            {
                floors.Add(new FacilityFloor
                {
                    BuildingId = building.BuildingId,
                    Name = $"Tầng {floor}",
                    Code = $"{building.Code}-F{floor}",
                    SortOrder = floor
                });
            }
        }
        db.FacilityFloors.AddRange(floors);
        db.SaveChanges();

        var zones = new List<SecurityZone>();
        foreach (var site in sites)
        {
            zones.Add(new SecurityZone { SiteId = site.SiteId, Name = "Sảnh công cộng", Code = $"{site.Code}-PUB", SecurityLevel = "Public", IsRestricted = false });
            zones.Add(new SecurityZone { SiteId = site.SiteId, Name = "Khu văn phòng", Code = $"{site.Code}-OFF", SecurityLevel = "Normal", IsRestricted = false });
            zones.Add(new SecurityZone { SiteId = site.SiteId, Name = "Khu sản xuất", Code = $"{site.Code}-PRD", SecurityLevel = "Restricted", IsRestricted = true });
            zones.Add(new SecurityZone { SiteId = site.SiteId, Name = "Phòng SOC và máy chủ", Code = $"{site.Code}-SOC", SecurityLevel = "Critical", IsRestricted = true });
        }
        db.SecurityZones.AddRange(zones);
        db.SaveChanges();

        var accessPoints = new List<AccessPoint>();
        foreach (var zone in zones)
        {
            accessPoints.Add(new AccessPoint
            {
                SiteId = zone.SiteId,
                SecurityZoneId = zone.SecurityZoneId,
                Name = $"{zone.Code} Cửa chính",
                Type = zone.IsRestricted ? "Turnstile" : "Door",
                DirectionMode = "Bidirectional"
            });
        }
        db.AccessPoints.AddRange(accessPoints);
        db.SaveChanges();

        db.Doors.AddRange(accessPoints.Select(ap => new Door
        {
            AccessPointId = ap.AccessPointId,
            Name = $"{ap.Name} Cửa",
            DoorMode = ap.Type == "Turnstile" ? "DynamicQrAndPlate" : "DynamicQr"
        }));
        db.MusterPoints.AddRange(sites.Select(site => new MusterPoint
        {
            SiteId = site.SiteId,
            Name = $"{site.Code} Điểm tập trung",
            LocationNote = "Điểm tập trung ngoài trời gần cổng chính",
            Capacity = 350
        }));
        db.SaveChanges();

        var gates = new[]
        {
            new Gate { GateName = "Cổng chính HN", Location = "Cổng trước trụ sở", Latitude = 21.0284m, Longitude = 105.8045m },
            new Gate { GateName = "Bãi xe hầm HN", Location = "Dốc B1 trụ sở", Latitude = 21.0283m, Longitude = 105.8049m },
            new Gate { GateName = "Cổng nhân viên BN", Location = "Lối vào nhân viên nhà máy", Latitude = 21.1860m, Longitude = 106.0765m },
            new Gate { GateName = "Cổng xe tải BN", Location = "Làn logistics nhà máy", Latitude = 21.1858m, Longitude = 106.0761m },
            new Gate { GateName = "Cổng kho HP", Location = "Cổng trung tâm logistics", Latitude = 20.8666m, Longitude = 106.6849m }
        };
        db.Gates.AddRange(gates);
        db.SaveChanges();

        db.Lanes.AddRange(new[]
        {
            new Lane { SiteId = sites[0].SiteId, GateId = gates[0].GateId, Name = "Làn vào HN", Direction = "Entry" },
            new Lane { SiteId = sites[0].SiteId, GateId = gates[0].GateId, Name = "Làn ra HN", Direction = "Exit" },
            new Lane { SiteId = sites[1].SiteId, GateId = gates[2].GateId, Name = "Lối vào nhân viên BN", Direction = "Entry" },
            new Lane { SiteId = sites[1].SiteId, GateId = gates[3].GateId, Name = "Làn xe tải BN", Direction = "Bidirectional" },
            new Lane { SiteId = sites[2].SiteId, GateId = gates[4].GateId, Name = "Làn kho HP", Direction = "Bidirectional" }
        });

        var cameras = new List<Camera>();
        foreach (var gate in gates)
        {
            cameras.Add(new Camera { CameraName = $"{gate.GateName} Máy quét QR", GateId = gate.GateId, CameraType = "QR", StreamUrl = "rtsp://demo.local/qr", UrlView = "http://127.0.0.1:1984/stream.html?src=demo" });
            cameras.Add(new Camera { CameraName = $"{gate.GateName} Camera biển số", GateId = gate.GateId, CameraType = "Plate", StreamUrl = "rtsp://demo.local/plate", UrlView = "http://127.0.0.1:1984/stream.html?src=demo" });
        }
        db.Cameras.AddRange(cameras);
        db.SaveChanges();

        var departmentNames = new[]
        {
            "Vận hành an ninh", "Nhân sự", "Sản xuất", "Đảm bảo chất lượng", "Kho",
            "Bảo trì", "Công nghệ thông tin", "Tài chính", "Kinh doanh", "Văn phòng điều hành"
        };
        var departments = departmentNames.Select(name => new Department { Name = name }).ToList();
        db.Departments.AddRange(departments);

        var positionNames = new[]
        {
            "Giám đốc", "Quản lý", "Giám sát", "Nhân viên an ninh", "Kỹ sư",
            "Kỹ thuật viên", "Vận hành viên", "Chuyên viên nhân sự", "Kế toán", "Điều phối kho"
        };
        var positions = positionNames.Select(name => new Position { Name = name }).ToList();
        db.Positions.AddRange(positions);

        var vehicleTypes = new[]
        {
            new VehicleType { TypeName = "Ô tô" },
            new VehicleType { TypeName = "Xe máy" },
            new VehicleType { TypeName = "Xe tải" },
            new VehicleType { TypeName = "Xe van" }
        };
        db.VehicleTypes.AddRange(vehicleTypes);

        db.ExceptionReasons.AddRange(new[]
        {
            new ExceptionReason { ReasonCode = "QR_EXPIRED", Description = "Mã QR động hết hạn hoặc nằm ngoài khung giờ cho phép" },
            new ExceptionReason { ReasonCode = "QR_REPLAY", Description = "Phát hiện mã QR động bị tái sử dụng" },
            new ExceptionReason { ReasonCode = "PLATE_REVIEW", Description = "Biển số cần rà soát thủ công" },
            new ExceptionReason { ReasonCode = "TEMP_ACCESS", Description = "Quyền truy cập tạm thời do quản lý phê duyệt" },
            new ExceptionReason { ReasonCode = "TAILGATING", Description = "Bất thường bám đuôi hoặc vi phạm chống passback" }
        });
        db.SaveChanges();

        var lastNames = new[] { "Nguyen", "Tran", "Le", "Pham", "Hoang", "Phan", "Vu", "Vo", "Dang", "Bui", "Do", "Ngo" };
        var middleNames = new[] { "Van", "Thi", "Minh", "Quang", "Duc", "Thanh", "Anh", "Gia", "Bao", "Hoai" };
        var firstNames = new[] { "An", "Binh", "Chau", "Dung", "Giang", "Hanh", "Khanh", "Lan", "Linh", "Long", "Mai", "Nam", "Phuc", "Quan", "Son", "Trang", "Tuan", "Vy" };

        var employees = new List<Employee>();
        for (var i = 1; i <= 180; i++)
        {
            var dept = departments[(i - 1) % departments.Count];
            var position = positions[i % positions.Count];
            var site = sites[(i - 1) % sites.Length];
            var status = i <= 168 ? EmployeeLifecycleStates.Active :
                i <= 174 ? EmployeeLifecycleStates.OnLeave :
                i <= 178 ? EmployeeLifecycleStates.Suspended :
                EmployeeLifecycleStates.ContractorActive;

            employees.Add(new Employee
            {
                FullName = $"{lastNames[i % lastNames.Length]} {middleNames[i % middleNames.Length]} {firstNames[i % firstNames.Length]} {i:000}",
                DepartmentId = dept.DepartmentId,
                PositionId = position.PositionId,
                Phone = $"09{random.Next(10000000, 99999999)}",
                Email = $"employee{i:000}@vshield-demo.vn",
                Status = status is EmployeeLifecycleStates.Active or EmployeeLifecycleStates.ContractorActive,
                LifecycleStatus = status,
                PrimarySiteId = site.SiteId,
                LifecycleUpdatedAtUtc = now.AddDays(-random.Next(1, 90))
            });
        }
        db.Employees.AddRange(employees);
        db.SaveChanges();
        EnsureEmployeeDynamicQrs(db, employees, now);

        var managers = employees.Where((_, index) => index < 20).ToArray();
        for (var i = 20; i < employees.Count; i++)
        {
            employees[i].ManagerEmployeeId = managers[i % managers.Length].EmployeeId;
        }
        db.SaveChanges();

        var shifts = db.Shifts.ToList();
        if (shifts.Count == 0)
        {
            shifts = new List<Shift>
            {
                new() { ShiftName = "Văn phòng 08:00-17:00", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(17, 0, 0), BreakMinutes = 60, AllowedLateMinutes = 10, AllowedEarlyLeaveMinutes = 10 },
                new() { ShiftName = "Nhà máy sáng 06:00-14:00", StartTime = new TimeSpan(6, 0, 0), EndTime = new TimeSpan(14, 0, 0), BreakMinutes = 45, AllowedLateMinutes = 5, AllowedEarlyLeaveMinutes = 5 },
                new() { ShiftName = "Nhà máy chiều 14:00-22:00", StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(22, 0, 0), BreakMinutes = 45, AllowedLateMinutes = 5, AllowedEarlyLeaveMinutes = 5 }
            };
            db.Shifts.AddRange(shifts);
            db.SaveChanges();
        }

        var vehicles = new List<Vehicle>();
        for (var i = 0; i < 80; i++)
        {
            var employee = employees[i % employees.Count];
            var type = i % 10 == 0 ? vehicleTypes[2] : i % 3 == 0 ? vehicleTypes[1] : vehicleTypes[0];
            vehicles.Add(new Vehicle
            {
                LicensePlate = $"{(i % 3 == 0 ? "29" : i % 3 == 1 ? "30" : "99")}A-{10000 + i * 37}",
                VehicleTypeId = type.VehicleTypeId,
                EmployeeId = employee.EmployeeId,
                SiteId = employee.PrimarySiteId,
                Description = $"{type.TypeName} được gán cho {employee.FullName}",
                ParkingStatus = i % 4 == 0 ? "IN" : "OUT"
            });
        }
        db.Vehicles.AddRange(vehicles);

        var guests = new List<GuestProfile>();
        for (var i = 1; i <= 60; i++)
        {
            guests.Add(new GuestProfile
            {
                FullName = $"{lastNames[(i + 2) % lastNames.Length]} {middleNames[(i + 3) % middleNames.Length]} {firstNames[(i + 4) % firstNames.Length]} - Visitor {i:00}",
                Phone = $"08{random.Next(10000000, 99999999)}",
                DefaultLicensePlate = $"51G-{70000 + i * 29}"
            });
        }
        db.GuestProfiles.AddRange(guests);
        db.SaveChanges();

        var preRegistrations = new List<PreRegistration>();
        for (var i = 0; i < 75; i++)
        {
            var dayOffset = i - 50;
            var expectedIn = DateTime.Today.AddDays(dayOffset).AddHours(8 + random.Next(0, 8)).AddMinutes(random.Next(0, 4) * 15);
            var status = dayOffset < -2 ? "Completed" : dayOffset < 0 ? "CheckedOut" : dayOffset == 0 ? "Approved" : i % 5 == 0 ? "Pending" : "Approved";
            preRegistrations.Add(new PreRegistration
            {
                GuestId = guests[i % guests.Count].GuestId,
                HostEmployeeId = employees[(i * 7) % employees.Count].EmployeeId,
                ExpectedTimeIn = expectedIn,
                ExpectedTimeOut = expectedIn.AddHours(2 + random.Next(1, 5)),
                Status = status,
                NumberOfVisitors = 1 + (i % 4 == 0 ? random.Next(1, 4) : 0),
                CreatedAt = expectedIn.AddDays(-random.Next(1, 14))
            });
        }
        db.PreRegistrations.AddRange(preRegistrations);
        db.SaveChanges();

        db.VisitorDetails.AddRange(preRegistrations.Select((registration, index) => new VisitorDetail
        {
            RegistrationId = registration.RegistrationId,
            FullName = guests[index % guests.Count].FullName,
            IdCardNumber = $"0{random.Next(100000000, 999999999)}",
            QrSecret = $"DEMO-VIS-{index:000}",
            QrPayload = $"VIS:{registration.RegistrationId}|DEMO:{index:000}",
            QrIssuedAt = registration.CreatedAt,
            IsQrActive = registration.Status is "Approved" or "Pending"
        }));
        db.SaveChanges();

        var schedules = new List<WorkSchedule>();
        var attendances = new List<Attendance>();
        for (var day = -30; day <= 7; day++)
        {
            var workDate = DateTime.Today.AddDays(day);
            if (workDate.DayOfWeek is DayOfWeek.Sunday)
            {
                continue;
            }

            foreach (var employee in employees.Take(150))
            {
                var shift = shifts[(employee.EmployeeId + day + 300) % shifts.Count];
                var schedule = new WorkSchedule
                {
                    EmployeeId = employee.EmployeeId,
                    ShiftId = shift.ShiftId,
                    WorkDate = workDate,
                    Status = day < 0 ? WorkScheduleStatuses.Worked : WorkScheduleStatuses.Scheduled,
                    Note = "Lịch làm việc mẫu được tạo tự động",
                    CreatedAt = now.AddDays(-45),
                    UpdatedAt = now.AddDays(-1)
                };
                schedules.Add(schedule);
            }
        }
        db.WorkSchedules.AddRange(schedules);
        db.SaveChanges();

        foreach (var schedule in schedules.Where(item => item.WorkDate <= DateTime.Today))
        {
            var shift = shifts.First(s => s.ShiftId == schedule.ShiftId);
            var employeeNoise = (schedule.EmployeeId * 13 + schedule.WorkDate.DayOfYear) % 37;
            var late = employeeNoise % 11 == 0 ? 18 : employeeNoise % 7 == 0 ? 8 : 0;
            var checkoutEarly = employeeNoise % 17 == 0 ? 20 : 0;
            var absent = employeeNoise % 43 == 0;
            var checkIn = schedule.WorkDate.Date.Add(shift.StartTime).AddMinutes(late == 0 ? random.Next(-8, 8) : late);
            var checkOut = schedule.WorkDate.Date.Add(shift.EndTime).AddMinutes(checkoutEarly == 0 ? random.Next(-5, 18) : -checkoutEarly);

            attendances.Add(new Attendance
            {
                EmployeeId = schedule.EmployeeId,
                ScheduleId = schedule.ScheduleId,
                WorkDate = schedule.WorkDate,
                CheckIn = absent ? null : checkIn,
                CheckOut = absent || schedule.WorkDate == DateTime.Today ? null : checkOut,
                LateMinutes = absent ? 0 : late,
                EarlyLeaveMinutes = absent ? 0 : checkoutEarly,
                TotalWorkingHours = absent ? 0 : 8,
                OvertimeHours = absent ? 0 : (employeeNoise % 9 == 0 ? 1.5m : 0),
                ZoneDwellTime = absent ? 0 : 7.5m,
                ZoneTransitCount = absent ? 0 : 2 + employeeNoise % 4,
                IsZoneDerived = true,
                Status = absent ? AttendanceStatuses.Absent :
                    late > 0 && checkoutEarly > 0 ? AttendanceStatuses.LateAndEarlyLeave :
                    late > 0 ? AttendanceStatuses.Late :
                    checkoutEarly > 0 ? AttendanceStatuses.EarlyLeave :
                    schedule.WorkDate == DateTime.Today ? AttendanceStatuses.CheckedIn :
                    AttendanceStatuses.Completed,
                Source = AttendanceSources.AccessLog,
                Note = "Được tạo từ sự kiện ra vào cổng và QR động",
                CreatedAt = now.AddDays(-20),
                UpdatedAt = now.AddDays(-1)
            });
        }
        db.Attendances.AddRange(attendances);
        db.SaveChanges();

        db.LeaveRequests.AddRange(employees.Skip(25).Take(18).Select((employee, index) => new LeaveRequest
        {
            EmployeeId = employee.EmployeeId,
            LeaveType = index % 3 == 0 ? LeaveTypes.SickLeave : LeaveTypes.AnnualLeave,
            StartDate = DateTime.Today.AddDays(index - 9),
            EndDate = DateTime.Today.AddDays(index - 8),
            Reason = index % 3 == 0 ? "Nghỉ ốm" : "Việc gia đình",
            Status = index % 4 == 0 ? LeaveRequestStatuses.Pending : LeaveRequestStatuses.Approved,
            CreatedAt = now.AddDays(-20 + index),
            UpdatedAt = now.AddDays(-10 + index)
        }));

        var accessLogs = new List<AccessLog>();
        var exceptionReasons = db.ExceptionReasons.ToList();
        for (var day = -90; day <= 0; day++)
        {
            var date = DateTime.Today.AddDays(day);
            var sampleCount = day == 0 ? 90 : 24;
            for (var i = 0; i < sampleCount; i++)
            {
                var employee = employees[(i * 11 + day + 900) % employees.Count];
                var gate = gates[Math.Abs(i + day) % gates.Length];
                var camera = cameras.FirstOrDefault(c => c.GateId == gate.GateId && c.CameraType == "QR")
                    ?? cameras.FirstOrDefault(c => c.GateId == gate.GateId);
                var vehicle = vehicles.FirstOrDefault(v => v.EmployeeId == employee.EmployeeId);
                var direction = i % 2 == 0 ? "IN" : "OUT";
                var failed = i % 53 == 0;
                var timestamp = date.AddHours(6 + i % 15).AddMinutes((i * 7) % 60);
                if (timestamp > now)
                {
                    var start = date.AddHours(6);
                    var maxElapsed = Math.Max(1, (now - start).TotalMinutes - 2);
                    timestamp = start.AddMinutes(maxElapsed * i / sampleCount);
                }

                accessLogs.Add(new AccessLog
                {
                    Timestamp = timestamp,
                    Direction = direction,
                    GateId = gate.GateId,
                    CameraId = camera?.CameraId,
                    CapturedLicensePlate = vehicle?.LicensePlate,
                    CapturedFaceImageUrl = null,
                    EmployeeId = employee.EmployeeId,
                    ResultStatus = failed ? "Denied" : "Granted",
                    IsBypass = i % 89 == 0,
                    ExceptionReasonId = failed ? exceptionReasons[Math.Abs(i + day) % exceptionReasons.Count].ReasonId : null,
                    Note = failed ? "Sự kiện bất thường QR động demo cho SOC rà soát" : "Sự kiện truy cập QR động demo",
                    SiteNameSnapshot = sites[(employee.PrimarySiteId ?? sites[0].SiteId) % sites.Length].Name,
                    SecurityZoneNameSnapshot = zones[Math.Abs(i + day) % zones.Count].Name,
                    AccessPointNameSnapshot = accessPoints[Math.Abs(i + day) % accessPoints.Count].Name,
                    LaneNameSnapshot = $"Làn {(i % 4) + 1}",
                    GateNameSnapshot = gate.GateName,
                    CameraNameSnapshot = camera?.CameraName
                });
            }
        }
        db.AccessLogs.AddRange(accessLogs);
        db.SaveChanges();

        var zoneTransits = accessLogs
            .Where(log => log.EmployeeId.HasValue && log.ResultStatus == "Granted")
            .Take(500)
            .Select((log, index) => new ZoneTransit
            {
                EmployeeId = log.EmployeeId!.Value,
                SecurityZoneId = zones[index % zones.Count].SecurityZoneId,
                AccessPointId = accessPoints[index % accessPoints.Count].AccessPointId,
                AccessLogId = log.LogId,
                Timestamp = log.Timestamp ?? now,
                Direction = log.Direction,
                Source = ZoneTransitSources.AccessLog,
                IsAutoDerived = true,
                CreatedAt = now
            })
            .ToList();
        db.ZoneTransits.AddRange(zoneTransits);
        SeedDynamicQrScanLogs(db, employees, accessLogs.Take(500).ToList(), now);

        db.SystemAuditLogs.Add(new SystemAuditLog
        {
            TimestampUtc = now,
            EventCategory = "DEMO_DATA",
            Severity = "INFO",
            ActionType = "SEED",
            EntityName = "DemoDataSeeder",
            EntityId = DemoCompanyCode,
            IsSuccess = true,
            NewValuesJson = "{\"profile\":\"medium-large-company\",\"employees\":180,\"days\":90}"
        });

        db.SaveChanges();
    }

    private static void ReconcileOperationalDemoData(ApplicationDbContext db)
    {
        var now = DateTime.UtcNow;
        var employees = db.Employees
            .Include(employee => employee.Department)
            .Include(employee => employee.Position)
            .OrderBy(employee => employee.EmployeeId)
            .ToList();

        EnsureDemoUserAccounts(db, employees, now);
        EnsureEmployeeDynamicQrs(db, employees, now);
        db.SaveChanges();
        EnsureEnterpriseDemoScenarios(db, employees, now);

        foreach (var door in db.Doors)
        {
            door.DoorMode = door.DoorMode.Contains("Face", StringComparison.OrdinalIgnoreCase)
                ? "DynamicQrAndPlate"
                : string.IsNullOrWhiteSpace(door.DoorMode) || door.DoorMode == "Normal"
                    ? "DynamicQr"
                    : door.DoorMode;
        }

        foreach (var camera in db.Cameras.ToList().Where(item =>
                     (item.CameraType ?? string.Empty).Contains("Face", StringComparison.OrdinalIgnoreCase) ||
                     item.CameraName.Contains("Face", StringComparison.OrdinalIgnoreCase) ||
                     (item.StreamUrl ?? string.Empty).Contains("/face", StringComparison.OrdinalIgnoreCase)))
        {
            camera.CameraName = camera.CameraName.Replace("Face Camera", "QR Scanner", StringComparison.OrdinalIgnoreCase);
            camera.CameraName = camera.CameraName.Replace("Face", "QR", StringComparison.OrdinalIgnoreCase);
            camera.CameraType = "QR";
            camera.StreamUrl = "rtsp://demo.local/qr";
        }

        var noFaceReason = db.ExceptionReasons.FirstOrDefault(reason => reason.ReasonCode == "NO_FACE_MATCH");
        if (noFaceReason != null)
        {
            var qrExpiredReason = db.ExceptionReasons.FirstOrDefault(reason => reason.ReasonCode == "QR_EXPIRED");
            if (qrExpiredReason != null && qrExpiredReason.ReasonId != noFaceReason.ReasonId)
            {
                foreach (var log in db.AccessLogs.Where(item => item.ExceptionReasonId == noFaceReason.ReasonId))
                {
                    log.ExceptionReasonId = qrExpiredReason.ReasonId;
                }

                db.ExceptionReasons.Remove(noFaceReason);
            }
            else
            {
                noFaceReason.ReasonCode = "QR_EXPIRED";
                noFaceReason.Description = "Dynamic QR token expired or outside allowed time window";
            }
        }

        UpsertExceptionReason(db, "QR_REPLAY", "Dynamic QR token replay detected");
        UpsertExceptionReason(db, "PLATE_REVIEW", "License plate requires manual review");
        UpsertExceptionReason(db, "TEMP_ACCESS", "Temporary access approved by supervisor");
        UpsertExceptionReason(db, "TAILGATING", "Tailgating or anti-passback anomaly");

        foreach (var log in db.AccessLogs.ToList().Where(item =>
                     item.CapturedFaceImageUrl == null && item.CapturedSnapshotUrl == null))
        {
            log.CapturedFaceImageUrl = "/uploads/evidence/demo/face-demo.jpg";
            log.CapturedSnapshotUrl = "/uploads/evidence/demo/snapshot-demo.jpg";
        }

        if (!db.DynamicQrScanLogs.Any() && employees.Count > 0)
        {
            SeedDynamicQrScanLogs(db, employees, db.AccessLogs.OrderByDescending(item => item.Timestamp).Take(500).ToList(), now);
        }

        // ── Seed additional demo data if tables are empty ──
        var rng = new Random(20260613);
        var gates = db.Gates.OrderBy(g => g.GateId).ToList();
        var sites = db.Sites.OrderBy(s => s.SiteId).ToList();
        var departments = db.Departments.OrderBy(d => d.DepartmentId).ToList();
        var vehiclesFull = db.Vehicles.OrderBy(v => v.VehicleId).ToList();

        if (!db.EmployeeAccessPermissions.Any() && employees.Count > 0 && gates.Count > 0)
        {
            var hnSiteId = sites[0].SiteId;
            var bnSiteId = sites.Count > 1 ? sites[1].SiteId : hnSiteId;
            var hpSiteId = sites.Count > 2 ? sites[2].SiteId : hnSiteId;
            var secOpsDeptId = departments.FirstOrDefault()?.DepartmentId ?? 0;
            var empPerms = new List<EmployeeAccessPermission>();
            foreach (var emp in employees.Where(e => e.Status == true))
            {
                var targetGates = emp switch
                {
                    { PrimarySiteId: var s } when s == hnSiteId => new[] { gates[0], gates.Count > 1 ? gates[1] : gates[0] },
                    { PrimarySiteId: var s } when s == bnSiteId => new[] { gates.Count > 2 ? gates[2] : gates[0], gates.Count > 3 ? gates[3] : gates[0] },
                    { PrimarySiteId: var s } when s == hpSiteId => new[] { gates.Count > 4 ? gates[4] : gates[0] },
                    _ => new[] { gates[0], gates.Count > 2 ? gates[2] : gates[0], gates.Count > 4 ? gates[4] : gates[0] }
                };
                if (secOpsDeptId > 0 && emp.DepartmentId == secOpsDeptId)
                    targetGates = gates.ToArray();
                foreach (var g in targetGates)
                {
                    empPerms.Add(new EmployeeAccessPermission
                    {
                        EmployeeId = emp.EmployeeId, GateId = g.GateId, IsAllowed = true, CreatedAt = now
                    });
                }
            }
            db.EmployeeAccessPermissions.AddRange(empPerms);
        }

        if (!db.VisitorAccessPermissions.Any())
        {
            var visitorDetails = db.VisitorDetails.ToList();
            var visPerms = new List<VisitorAccessPermission>();
            foreach (var vd in visitorDetails)
            {
                foreach (var g in gates.OrderBy(_ => rng.Next()).Take(2))
                {
                    visPerms.Add(new VisitorAccessPermission
                    {
                        VisitorDetailId = vd.VisitorDetailId, GateId = g.GateId, IsAllowed = true, CreatedAt = now
                    });
                }
            }
            db.VisitorAccessPermissions.AddRange(visPerms);
        }

        if (!db.ParkingAreas.Any() && sites.Count > 0)
        {
            var parkingAreas = new List<ParkingArea>
            {
                new() { SiteId = sites[0].SiteId, Name = "HN Tầng hầm B1", Capacity = 80, IsActive = true },
                new() { SiteId = sites[0].SiteId, Name = "HN Bãi ngoài trời", Capacity = 120, IsActive = true }
            };
            if (sites.Count > 1)
            {
                parkingAreas.Add(new() { SiteId = sites[1].SiteId, Name = "BN Nhà xe nhân viên", Capacity = 200, IsActive = true });
                parkingAreas.Add(new() { SiteId = sites[1].SiteId, Name = "BN Bãi xe tải", Capacity = 50, IsActive = true });
            }
            if (sites.Count > 2)
                parkingAreas.Add(new() { SiteId = sites[2].SiteId, Name = "HP Kho bãi logistics", Capacity = 100, IsActive = true });
            db.ParkingAreas.AddRange(parkingAreas);
        }

        if (!db.ParkingPermits.Any() && vehiclesFull.Count > 0)
        {
            var areas = db.ParkingAreas.ToList();
            if (areas.Count > 0)
            {
                var permits = vehiclesFull.Take(30).Select((v, i) => new ParkingPermit
                {
                    ParkingAreaId = areas[i % areas.Count].ParkingAreaId,
                    VehicleId = v.VehicleId,
                    PermitType = i < 20 ? "Monthly" : "Temporary",
                    ValidFromUtc = now.AddDays(-60),
                    ValidToUtc = now.AddDays(30 + i),
                    IsRevoked = i > 28
                }).ToList();
                db.ParkingPermits.AddRange(permits);
            }
        }

        if (!db.Barriers.Any())
        {
            var lanes = db.Lanes.ToList();
            if (lanes.Count > 0)
            {
                var barriers = lanes.Select((l, i) => new SecurityBarrier
                {
                    LaneId = l.LaneId,
                    Name = $"{l.Name} Barrier",
                    State = i % 3 == 0 ? "Open" : i % 3 == 1 ? "Closed" : "Unknown",
                    IsActive = i < 4
                }).ToList();
                db.Barriers.AddRange(barriers);
                db.SaveChanges();
            }
        }

        if (!db.BarrierCommandAudits.Any())
        {
            var barriers = db.Barriers.ToList();
            if (barriers.Count > 0)
            {
                var barrierAudits = barriers.Take(4).SelectMany((b, i) => new[]
                {
                    new BarrierCommandAudit
                    {
                        BarrierId = b.BarrierId, Command = i % 2 == 0 ? "Open" : "Close",
                        Reason = "Dữ liệu mẫu — kiểm tra thủ công", RequestedByUserId = null,
                        RequestedAtUtc = now.AddDays(-i - 1), Result = "Success"
                    },
                    new BarrierCommandAudit
                    {
                        BarrierId = b.BarrierId, Command = i % 2 == 0 ? "Close" : "Open",
                        Reason = "Dữ liệu mẫu — kết thúc kiểm tra", RequestedByUserId = null,
                        RequestedAtUtc = now.AddDays(-i), Result = "Success"
                    }
                }).ToList();
                db.BarrierCommandAudits.AddRange(barrierAudits);
            }
        }

        if (!db.Visits.Any())
        {
            var preRegs = db.PreRegistrations.Include(p => p.Guest).OrderBy(p => p.RegistrationId).ToList();
            if (preRegs.Count > 0)
            {
                var visits = preRegs.Select(pr => new Visit
                {
                    SiteId = sites[(pr.RegistrationId - 1) % sites.Count].SiteId,
                    HostEmployeeId = pr.HostEmployeeId,
                    VisitorName = pr.Guest?.FullName ?? $"Visitor {pr.RegistrationId}",
                    VisitorType = "Visitor",
                    VisitorPhone = null,
                    VisitorEmail = null,
                    Status = pr.Status ?? VisitStatuses.Approved,
                    ExpectedInUtc = pr.ExpectedTimeIn,
                    ExpectedOutUtc = pr.ExpectedTimeOut,
                    EscortRequired = false,
                    NdaRequired = false,
                    SafetyBriefingRequired = false,
                    HostNotified = false,
                    CreatedAtUtc = pr.CreatedAt
                }).ToList();
                db.Visits.AddRange(visits);
                db.SaveChanges();
            }
        }

        if (!db.VisitorCredentials.Any())
        {
            var visits = db.Visits.OrderBy(v => v.VisitId).Take(20).ToList();
            if (visits.Count > 0)
            {
                var credentials = visits.Select((v, i) => new VisitorCredential
                {
                    VisitId = v.VisitId,
                    CredentialType = "QR",
                    CredentialReference = $"DEMO-CRED-{v.VisitId:000}",
                    ValidFromUtc = v.ExpectedInUtc,
                    ValidToUtc = v.ExpectedOutUtc,
                    IsRevoked = i > 17
                }).ToList();
                db.VisitorCredentials.AddRange(credentials);
            }
        }

        SeedCampus3DScene(db, sites, now);
        SeedGeolocationDemoData(db, now);
        SeedDemoNotifications(db, now);
        SeedRegistrationLinks(db, employees, now);
        SeedVisitorJourneyData(db, now);
        SeedWatchlistAndReceptionData(db, now);
        SeedContractorAndDelegationData(db, employees, now);
        SeedCameraTelemetryData(db, now);
        SeedEvidenceGovernanceData(db, now);
        SeedAccessPolicyAndEmergencyData(db, employees, now);
        SeedLostFoundAndLockerData(db, now);
        SeedSocAwarenessAndChatData(db, employees, now);
        SeedOperationalReadinessData(db, now);
        db.SaveChanges();
    }

    private static void SeedGeolocationDemoData(ApplicationDbContext db, DateTime now)
    {
        var sites = db.Sites.Where(s => s.Latitude == null).ToList();
        foreach (var site in sites)
        {
            switch (site.Code)
            {
                case "HN-HQ": site.Latitude = 21.028511m; site.Longitude = 105.804817m; break;
                case "BN-FAC": site.Latitude = 21.186111m; site.Longitude = 106.076389m; break;
                case "HP-LOG": site.Latitude = 20.866667m; site.Longitude = 106.684722m; break;
            }
        }

        var buildings = db.Buildings.Where(b => b.Latitude == null).ToList();
        foreach (var building in buildings)
        {
            switch (building.Code)
            {
                case "HN-HQ-ADM": building.Latitude = 21.0286m; building.Longitude = 105.8049m; building.TotalFloors = 5; break;
                case "HN-HQ-OPS": building.Latitude = 21.0287m; building.Longitude = 105.8052m; building.TotalFloors = 3; break;
                case "BN-FAC-ADM": building.Latitude = 21.1862m; building.Longitude = 106.0764m; building.TotalFloors = 4; break;
                case "BN-FAC-OPS": building.Latitude = 21.1860m; building.Longitude = 106.0760m; building.TotalFloors = 2; break;
                case "HP-LOG-ADM": building.Latitude = 20.8668m; building.Longitude = 106.6848m; building.TotalFloors = 3; break;
                case "HP-LOG-OPS": building.Latitude = 20.8665m; building.Longitude = 106.6845m; building.TotalFloors = 1; break;
            }
        }

        var gatesList = db.Gates.Where(g => g.Latitude == null).ToList();
        foreach (var gate in gatesList)
        {
            switch (gate.GateName)
            {
                case "Cổng chính HN": gate.Latitude = 21.0284m; gate.Longitude = 105.8045m; break;
                case "Bãi xe hầm HN": gate.Latitude = 21.0283m; gate.Longitude = 105.8049m; break;
                case "Cổng nhân viên BN": gate.Latitude = 21.1860m; gate.Longitude = 106.0765m; break;
                case "Cổng xe tải BN": gate.Latitude = 21.1858m; gate.Longitude = 106.0761m; break;
                case "Cổng kho HP": gate.Latitude = 20.8666m; gate.Longitude = 106.6849m; break;
            }
        }

        var accessPoints = db.AccessPoints.Where(ap => ap.Latitude == null).ToList();
        foreach (var ap in accessPoints)
        {
            ap.Latitude = 21.0285m;
            ap.Longitude = 105.8048m;
        }

        if (!db.IndoorPathNodes.Any())
        {
            var hnAdm = db.Buildings.FirstOrDefault(b => b.Code == "HN-HQ-ADM");
            if (hnAdm != null)
            {
                var floors = db.FacilityFloors.Where(f => f.BuildingId == hnAdm.BuildingId).OrderBy(f => f.SortOrder).ToList();
                var nodes = new List<IndoorPathNode>();

                void AddNode(int floorSort, string label, string type, decimal x, decimal y, decimal z, bool isExit)
                {
                    var floor = floors.FirstOrDefault(f => f.SortOrder == floorSort);
                    nodes.Add(new IndoorPathNode
                    {
                        BuildingId = hnAdm.BuildingId,
                        FacilityFloorId = floor?.FacilityFloorId,
                        Label = label,
                        NodeType = type,
                        X = x, Y = y, Z = z,
                        IsEmergencyExit = isExit,
                        NeighborsJson = "[]"
                    });
                }

                AddNode(1, "Lối vào chính (F1)", "Entrance", 0m, 0m, 0m, false);
                AddNode(1, "Hành lang chính F1", "Corridor", 3m, 0m, 0m, false);
                AddNode(1, "Cầu thang A (F1→F2)", "Stair", 7m, 0m, 0m, true);
                AddNode(1, "Thang máy chính (F1)", "Elevator", 3m, 3m, 0m, false);
                AddNode(1, "Phòng Bảo vệ (SOC)", "Room", 1m, 2m, 0m, false);
                AddNode(2, "Cầu thang A (F2)", "Stair", 7m, 0m, 3m, true);
                AddNode(2, "Hành lang chính F2", "Corridor", 3m, 0m, 3m, false);
                AddNode(2, "Thang máy chính (F2)", "Elevator", 3m, 3m, 3m, false);
                AddNode(2, "Phòng Họp A (F2)", "Room", 1m, 1m, 3m, false);
                AddNode(2, "Phòng IT (F2)", "Room", 5m, 0m, 3m, false);
                AddNode(3, "Thang máy chính (F3)", "Elevator", 3m, 3m, 6m, false);
                AddNode(3, "Hành lang chính F3", "Corridor", 3m, 0m, 6m, false);
                AddNode(3, "Văn phòng Giám đốc", "Room", 1m, 1m, 6m, false);
                AddNode(3, "Phòng Hành chính", "Room", 5m, 0m, 6m, false);

                db.IndoorPathNodes.AddRange(nodes);
            }
        }
    }

    private static void SeedDemoNotifications(ApplicationDbContext db, DateTime now)
    {
        if (db.Notifications.Any()) return;

        var adminUser = db.AppUsers.FirstOrDefault(u => u.Role == "Admin");
        var managerUser = db.AppUsers.FirstOrDefault(u => u.Role == "QuanLy");
        var guardUser = db.AppUsers.FirstOrDefault(u => u.Role == "BaoVe");
        var receptionUser = db.AppUsers.FirstOrDefault(u => u.Role == "LeTan");
        var nhanVienUser = db.AppUsers.FirstOrDefault(u => u.Role == "NhanVien");
        var nhanSuUser = db.AppUsers.FirstOrDefault(u => u.Role == "NhanSu");

        var demoNotifications = new List<Notification>();

        if (adminUser != null)
        {
            demoNotifications.Add(new Notification
            {
                RecipientUserId = adminUser.UserId, Title = "Báo động uy hiếp", Body = "Phát hiện uy hiếp tại Access Point #12 — Nhân viên Nguyễn Văn An", Category = "Alarm", ReferenceType = "Alarm", Latitude = 21.0285m, Longitude = 105.8048m, LocationLabel = "Tòa nhà HN Admin - Tầng 1", CreatedAt = now.AddMinutes(-5), IsRead = false
            });
            demoNotifications.Add(new Notification
            {
                RecipientUserId = adminUser.UserId, Title = "Yêu cầu xuất bằng chứng mới", Body = "Nhân viên Trần Thị Bình yêu cầu xuất video camera #HN-CAM-03", Category = "Approval", ReferenceType = "Evidence", CreatedAt = now.AddMinutes(-30), IsRead = false
            });
            demoNotifications.Add(new Notification
            {
                RecipientUserId = adminUser.UserId, Title = "Đơn nghỉ phép mới", Body = "Nhân viên Lê Văn Cường xin nghỉ ốm từ 30/06 đến 01/07", Category = "Approval", ReferenceType = "LeaveRequest", ReferenceId = "42", ActionUrl = "/attendance/leave-approvals", CreatedAt = now.AddHours(-2), IsRead = false
            });
            demoNotifications.Add(new Notification
            {
                RecipientUserId = adminUser.UserId, Title = "Yêu cầu nhận đồ thất lạc", Body = "Có yêu cầu nhận lại điện thoại iPhone từ tủ đồ #L3", Category = "Approval", ReferenceType = "LostFound", CreatedAt = now.AddHours(-4), IsRead = true, ReadAt = now.AddHours(-3)
            });
            demoNotifications.Add(new Notification
            {
                RecipientUserId = adminUser.UserId, Title = "Hệ thống đồng bộ thành công", Body = "Đồng bộ danh sách hẹn trước và dashboard hoàn tất.", Category = "System", CreatedAt = now.AddHours(-8), IsRead = true, ReadAt = now.AddHours(-7)
            });
        }

        if (guardUser != null)
        {
            demoNotifications.Add(new Notification
            {
                RecipientUserId = guardUser.UserId, Title = "Báo động khẩn cấp", Body = "Vượt cổng khẩn cấp tại Cổng chính HN — xe 29A-12345", Category = "Alarm", ReferenceType = "Alarm", Latitude = 21.0284m, Longitude = 105.8045m, LocationLabel = "Cổng chính HN - Cầu Giấy", CreatedAt = now.AddMinutes(-10), IsRead = false
            });
            demoNotifications.Add(new Notification
            {
                RecipientUserId = guardUser.UserId, Title = "Cảnh báo thiết bị ngoại tuyến", Body = "Camera HN-B1-Parking mất kết nối hơn 5 phút.", Category = "Alarm", ReferenceType = "Alarm", Latitude = 21.0283m, Longitude = 105.8049m, LocationLabel = "HN Tầng hầm B1", CreatedAt = now.AddMinutes(-20), IsRead = false
            });
            demoNotifications.Add(new Notification
            {
                RecipientUserId = guardUser.UserId, Title = "Yêu cầu can thiệp mới", Body = "Yêu cầu can thiệp tại khu vực Production Zone — loại Emergency.", Category = "Approval", ReferenceType = "Intervention", CreatedAt = now.AddHours(-1), IsRead = false
            });
            demoNotifications.Add(new Notification
            {
                RecipientUserId = guardUser.UserId, Title = "Thiết bị phục hồi", Body = "Camera BN-Employee-Gate đã kết nối lại.", Category = "System", CreatedAt = now.AddHours(-3), IsRead = true, ReadAt = now.AddHours(-2)
            });
        }

        if (managerUser != null)
        {
            demoNotifications.Add(new Notification
            {
                RecipientUserId = managerUser.UserId, Title = "Đơn nghỉ phép cần duyệt", Body = "Nhân viên Phạm Thị Dung xin nghỉ phép năm 3 ngày từ 05/07.", Category = "Approval", ReferenceType = "LeaveRequest", ActionUrl = "/attendance/leave-approvals", CreatedAt = now.AddMinutes(-45), IsRead = false
            });
            demoNotifications.Add(new Notification
            {
                RecipientUserId = managerUser.UserId, Title = "Yêu cầu can thiệp cần phê duyệt", Body = "Bảo vệ yêu cầu can thiệp tại khu vực hạn chế.", Category = "Approval", ReferenceType = "Intervention", CreatedAt = now.AddHours(-2), IsRead = true, ReadAt = now.AddHours(-1)
            });
        }

        if (nhanVienUser != null)
        {
            demoNotifications.Add(new Notification
            {
                RecipientUserId = nhanVienUser.UserId, Title = "Đơn nghỉ phép đã được duyệt", Body = "Đơn nghỉ phép của bạn từ 28/06 đến 29/06 đã được duyệt.", Category = "Approval", ReferenceType = "LeaveRequest", ActionUrl = "/attendance/my-leave-requests", CreatedAt = now.AddHours(-3), IsRead = true, ReadAt = now.AddHours(-2)
            });
            demoNotifications.Add(new Notification
            {
                RecipientUserId = nhanVienUser.UserId, Title = "Yêu cầu điều xe đã được chấp nhận", Body = "Anh/chị Hoàng Văn E đã chấp nhận điều xe 29A-67890 cho bạn.", Category = "Approval", ReferenceType = "VehicleDelegation", CreatedAt = now.AddHours(-6), IsRead = true, ReadAt = now.AddHours(-5)
            });
        }

        if (receptionUser != null)
        {
            demoNotifications.Add(new Notification
            {
                RecipientUserId = receptionUser.UserId, Title = "Khách đến lễ tân", Body = "Khách Nguyễn Thị Hương đã đến — vui lòng hỗ trợ làm thủ tục.", Category = "System", CreatedAt = now.AddMinutes(-15), IsRead = false
            });
        }

        if (nhanSuUser != null)
        {
            demoNotifications.Add(new Notification
            {
                RecipientUserId = nhanSuUser.UserId, Title = "Đơn nghỉ phép cần xem xét", Body = "Nhân viên Đặng Văn Khoa xin nghỉ chế độ 5 ngày.", Category = "Approval", ReferenceType = "LeaveRequest", CreatedAt = now.AddHours(-1), IsRead = false
            });
        }

        db.Notifications.AddRange(demoNotifications);
    }

    private static void SeedRegistrationLinks(ApplicationDbContext db, List<Employee> employees, DateTime now)
    {
        if (db.RegistrationLinks.Any() || employees.Count == 0)
            return;

        var hosts = employees
            .Where(employee => employee.Status == true)
            .OrderBy(employee => employee.EmployeeId)
            .Take(24)
            .ToList();

        db.RegistrationLinks.AddRange(hosts.Select((employee, index) => new RegistrationLink
        {
            HostEmployeeId = employee.EmployeeId,
            Token = $"DEMOHOST{employee.EmployeeId:X6}{index:X2}",
            CreatedAt = now.AddDays(-(index % 12 + 1)),
            ExpiredAt = now.AddDays(index % 6 - 2).AddHours(18),
            IsUsed = index % 4 == 0
        }));
    }

    private static void SeedVisitorJourneyData(ApplicationDbContext db, DateTime now)
    {
        if (!db.VisitorFormTemplates.Any())
        {
            db.VisitorFormTemplates.AddRange(
                new VisitorFormTemplate
                {
                    Name = "NDA tiêu chuẩn khách tham quan",
                    FormType = "NDA",
                    Version = 3,
                    Body = "Khách tham quan cam kết giữ bí mật mọi thông tin vận hành, sản xuất và an ninh quan sát được tại cơ sở.",
                    IsActive = true
                },
                new VisitorFormTemplate
                {
                    Name = "Hướng dẫn an toàn nhà máy",
                    FormType = "Safety",
                    Version = 2,
                    Body = "Khách tham quan xác nhận đã nắm quy tắc PPE, yêu cầu đi kèm và hướng dẫn tập trung khẩn cấp trước khi vào khu sản xuất.",
                    IsActive = true
                });
        }

        if (!db.VisitorCheckIns.Any())
        {
            var visits = db.Visits
                .OrderByDescending(visit => visit.ExpectedInUtc)
                .Take(28)
                .ToList();
            var users = db.AppUsers
                .Where(user => user.Role == "LeTan" || user.Role == "BaoVe")
                .OrderBy(user => user.UserId)
                .ToList();

            if (visits.Count > 0)
            {
                db.VisitorCheckIns.AddRange(visits.Select((visit, index) =>
                {
                    var operatorUserId = users.Count == 0 ? (int?)null : users[index % users.Count].UserId;
                    var checkedInAt = visit.ExpectedInUtc.AddMinutes(index % 5 == 0 ? 18 : -5 + index % 11);
                    var status = index % 7 == 0 ? VisitStatuses.Overstay :
                        index % 4 == 0 ? VisitStatuses.CheckedOut :
                        VisitStatuses.CheckedIn;

                    visit.Status = status;

                    return new VisitorCheckIn
                    {
                        VisitId = visit.VisitId,
                        CheckedInAtUtc = checkedInAt,
                        CheckedOutAtUtc = status == VisitStatuses.CheckedOut ? checkedInAt.AddHours(1 + index % 3) : null,
                        CheckedInByUserId = operatorUserId,
                        CheckedOutByUserId = status == VisitStatuses.CheckedOut ? operatorUserId : null,
                        IdDocumentType = index % 3 == 0 ? "CitizenId" : "Passport",
                        IdDocumentReference = $"DOC-{visit.VisitId:0000}-{index:000}",
                        VerificationStatus = index % 6 == 0 ? "ManualReview" : "Verified"
                    };
                }));
            }
        }

        if (!db.VisitorFormAcceptances.Any())
        {
            var templates = db.VisitorFormTemplates.OrderBy(template => template.VisitorFormTemplateId).ToList();
            var visits = db.Visits
                .Where(visit => visit.NdaRequired || visit.SafetyBriefingRequired || visit.Status == VisitStatuses.CheckedIn || visit.Status == VisitStatuses.CheckedOut)
                .OrderBy(visit => visit.VisitId)
                .Take(18)
                .ToList();

            if (templates.Count > 0 && visits.Count > 0)
            {
                db.VisitorFormAcceptances.AddRange(visits.SelectMany((visit, index) =>
                {
                    var acceptedBy = visit.VisitorName;
                    var acceptances = new List<VisitorFormAcceptance>
                    {
                        new()
                        {
                            VisitId = visit.VisitId,
                            VisitorFormTemplateId = templates[0].VisitorFormTemplateId,
                            AcceptedAtUtc = visit.ExpectedInUtc.AddMinutes(-15),
                            AcceptedByName = acceptedBy
                        }
                    };

                    if (templates.Count > 1 && (visit.SafetyBriefingRequired || index % 3 == 0))
                    {
                        acceptances.Add(new VisitorFormAcceptance
                        {
                            VisitId = visit.VisitId,
                            VisitorFormTemplateId = templates[1].VisitorFormTemplateId,
                            AcceptedAtUtc = visit.ExpectedInUtc.AddMinutes(-10),
                            AcceptedByName = acceptedBy
                        });
                    }

                    return acceptances;
                }));
            }
        }
    }

    private static void SeedWatchlistAndReceptionData(ApplicationDbContext db, DateTime now)
    {
        if (!db.WatchlistEntries.Any())
        {
            db.WatchlistEntries.AddRange(
                new WatchlistEntry
                {
                    EntityType = "Person",
                    DisplayName = "Tran Van Kiem Tra",
                    Identifier = "ID-ALERT-001",
                    Severity = "High",
                    Reason = "Nhiều lần cố vào khu vực hạn chế bằng thông tin đăng nhập khách đã hết hạn.",
                    CreatedAtUtc = now.AddDays(-21)
                },
                new WatchlistEntry
                {
                    EntityType = "Vehicle",
                    DisplayName = "Truck nghi van logistics",
                    Identifier = "99C-67890",
                    Severity = "Critical",
                    Reason = "Được kiểm toán logistics gắn cờ vì sự kiện ra vào và manifest không khớp.",
                    CreatedAtUtc = now.AddDays(-14)
                },
                new WatchlistEntry
                {
                    EntityType = "Person",
                    DisplayName = "Khách quá giờ lặp lại",
                    Identifier = "VIS-OVERSTAY-003",
                    Severity = "Medium",
                    Reason = "Nhiều lần quá giờ trong 30 ngày trước.",
                    CreatedAtUtc = now.AddDays(-9)
                });
        }

        if (!db.WatchlistMatches.Any())
        {
            var entries = db.WatchlistEntries.OrderBy(entry => entry.WatchlistEntryId).ToList();
            var visits = db.Visits.OrderByDescending(visit => visit.ExpectedInUtc).Take(10).ToList();
            var vehicles = db.Vehicles.OrderBy(vehicle => vehicle.VehicleId).Take(12).ToList();
            var reviewers = db.AppUsers.Where(user => user.Role == "BaoVe" || user.Role == "Admin").OrderBy(user => user.UserId).ToList();

            if (entries.Count > 0)
            {
                var matches = new List<WatchlistMatch>();
                if (visits.Count > 0)
                {
                    matches.Add(new WatchlistMatch
                    {
                        WatchlistEntryId = entries[0].WatchlistEntryId,
                        VisitId = visits[0].VisitId,
                        Status = "Escalated",
                        ReviewNote = "Lễ tân xác nhận sai lệch giấy tờ và chuyển cấp cho trưởng ca an ninh.",
                        MatchedAtUtc = now.AddHours(-8),
                        ReviewedAtUtc = now.AddHours(-7),
                        ReviewedByUserId = reviewers.FirstOrDefault()?.UserId
                    });
                }

                if (entries.Count > 1 && vehicles.Count > 0)
                {
                    matches.Add(new WatchlistMatch
                    {
                        WatchlistEntryId = entries[1].WatchlistEntryId,
                        VehicleId = vehicles[^1].VehicleId,
                        Status = "Pending",
                        ReviewNote = "Xe đang chờ kiểm tra làn thủ công ở lần đến tiếp theo.",
                        MatchedAtUtc = now.AddHours(-3)
                    });
                }

                if (entries.Count > 2 && visits.Count > 1)
                {
                    matches.Add(new WatchlistMatch
                    {
                        WatchlistEntryId = entries[2].WatchlistEntryId,
                        VisitId = visits[1].VisitId,
                        Status = "Resolved",
                        ReviewNote = "Chủ nhà xác nhận chuyến thăm kéo dài do cuộc họp kiểm toán kéo dài.",
                        MatchedAtUtc = now.AddDays(-1),
                        ReviewedAtUtc = now.AddHours(-12),
                        ReviewedByUserId = reviewers.Skip(1).FirstOrDefault()?.UserId ?? reviewers.FirstOrDefault()?.UserId
                    });
                }

                db.WatchlistMatches.AddRange(matches);
            }
        }

        if (!db.ReceptionInteractions.Any())
        {
            var visits = db.Visits.OrderByDescending(visit => visit.ExpectedInUtc).Take(16).ToList();
            var users = db.AppUsers.Where(user => user.Role == "LeTan" || user.Role == "BaoVe").OrderBy(user => user.UserId).ToList();

            if (visits.Count > 0)
            {
                db.ReceptionInteractions.AddRange(visits.Select((visit, index) => new ReceptionInteraction
                {
                    VisitId = visit.VisitId,
                    InteractionType = (index % 5) switch
                    {
                        0 => ReceptionInteractionTypes.HostContact,
                        1 => ReceptionInteractionTypes.VisitorSupport,
                        2 => ReceptionInteractionTypes.SecurityDispatch,
                        3 => ReceptionInteractionTypes.ParkingInquiry,
                        _ => ReceptionInteractionTypes.Wayfinding
                    },
                    Summary = index % 5 == 2
                        ? $"Đã yêu cầu hỗ trợ đi kèm cho {visit.VisitorName}"
                        : $"Lễ tân đã xử lý quy trình thăm quan cho {visit.VisitorName}",
                    DetailNote = index % 5 == 2
                        ? "Khách yêu cầu vào khu sản xuất hạn chế và cần điều động nhân viên đi kèm."
                        : "Lễ tân đã xác minh đặt lịch, trạng thái chủ nhà và hướng dẫn đỗ xe.",
                    ContactPersonName = visit.VisitorName,
                    ContactPersonPhone = visit.VisitorPhone,
                    RelatedVehiclePlate = index % 3 == 0 ? $"51H-{61000 + index * 17}" : null,
                    Status = index % 6 == 0 ? ReceptionInteractionStatuses.Escalated :
                        index % 4 == 0 ? ReceptionInteractionStatuses.Resolved :
                        ReceptionInteractionStatuses.InProgress,
                    SecurityRequested = index % 5 == 2,
                    ResolutionNote = index % 4 == 0 ? "Đã đóng vụ việc sau khi chủ nhà xác nhận và kiểm tra ra về." : null,
                    CreatedAtUtc = now.AddHours(-(index + 2)),
                    UpdatedAtUtc = now.AddHours(-(index % 3)),
                    CreatedByUserId = users.Count == 0 ? (int?)null : users[index % users.Count].UserId,
                    UpdatedByUserId = users.Count == 0 ? (int?)null : users[index % users.Count].UserId
                }));
            }
        }
    }

    private static void SeedContractorAndDelegationData(ApplicationDbContext db, List<Employee> employees, DateTime now)
    {
        if (!db.Contractors.Any())
        {
            var contractorEmployees = employees
                .Where(employee => string.Equals(employee.LifecycleStatus, EmployeeLifecycleStates.ContractorActive, StringComparison.OrdinalIgnoreCase))
                .Take(12)
                .ToList();
            var siteIds = db.Sites.OrderBy(site => site.SiteId).Select(site => site.SiteId).ToList();

            db.Contractors.AddRange(contractorEmployees.Select((employee, index) => new Contractor
            {
                EmployeeId = employee.EmployeeId,
                FullName = employee.FullName,
                Company = (index % 3) switch
                {
                    0 => "Công ty PCCC An Bình",
                    1 => "Dịch vụ Cơ sở hạ tầng North Star",
                    _ => "Bảo trì công nghiệp Delta"
                },
                Phone = employee.Phone,
                Email = employee.Email,
                ContractFromUtc = now.AddDays(-(60 + index * 3)),
                ContractToUtc = now.AddDays(index % 4 == 0 ? 7 : 45 + index * 2),
                Status = index % 5 == 0 ? ContractorStatuses.Expiring : ContractorStatuses.Active,
                SiteId = employee.PrimarySiteId ?? siteIds.ElementAtOrDefault(index % Math.Max(siteIds.Count, 1)),
                RequiredTraining = index % 2 == 0 ? "PPE, Khóa/Gắn thẻ an toàn, Đi kèm khách" : "An toàn điện, nhận thức không gian hạn chế",
                AccessReviewCompleted = index % 3 != 0,
                AccessReviewDateUtc = index % 3 != 0 ? now.AddDays(-(index + 2)) : null,
                CreatedAtUtc = now.AddDays(-(90 - index))
            }));
        }

        if (!db.VehicleDelegations.Any())
        {
            var vehicles = db.Vehicles.Where(vehicle => vehicle.EmployeeId.HasValue).OrderBy(vehicle => vehicle.VehicleId).Take(14).ToList();
            if (vehicles.Count > 0)
            {
                var delegations = new List<VehicleDelegation>();
                foreach (var (vehicle, index) in vehicles.Select((vehicle, index) => (vehicle, index)))
                {
                    var fromEmployeeId = vehicle.EmployeeId!.Value;
                    var toEmployee = employees.FirstOrDefault(employee =>
                        employee.EmployeeId != fromEmployeeId &&
                        employee.PrimarySiteId == vehicle.SiteId &&
                        employee.Status == true);
                    if (toEmployee == null)
                        continue;

                    delegations.Add(new VehicleDelegation
                    {
                        VehicleId = vehicle.VehicleId,
                        FromEmployeeId = fromEmployeeId,
                        ToEmployeeId = toEmployee.EmployeeId,
                        Reason = index % 3 == 0
                            ? "Hỗ trợ trực ca cho ca muộn"
                            : index % 3 == 1
                                ? "Vận chuyển tài liệu giữa các cơ sở và đi chi nhánh"
                                : "Thay thế tạm thời trong khi xe được bảo dưỡng",
                        Status = (index % 5) switch
                        {
                            0 => DelegationStatuses.Pending,
                            1 => DelegationStatuses.Rejected,
                            2 => DelegationStatuses.Revoked,
                            _ => DelegationStatuses.Approved
                        },
                        RequestedAtUtc = now.AddDays(-(index + 1)),
                        RespondedAtUtc = index % 5 == 0 ? null : now.AddDays(-index).AddHours(4)
                    });
                }

                db.VehicleDelegations.AddRange(delegations);
            }
        }
    }

    private static void SeedCameraTelemetryData(ApplicationDbContext db, DateTime now)
    {
        if (!db.RecordedSegments.Any())
        {
            var cameras = db.Cameras.OrderBy(camera => camera.CameraId).Take(10).ToList();
            var segments = new List<RecordedSegment>();

            foreach (var camera in cameras)
            {
                for (var i = 0; i < 6; i++)
                {
                    var startedAt = now.AddHours(-(camera.CameraId % 5 * 6 + i + 1));
                    var durationMinutes = 20 + i * 5;
                    segments.Add(new RecordedSegment
                    {
                        CameraId = camera.CameraId,
                        StartedAt = startedAt,
                        EndedAt = startedAt.AddMinutes(durationMinutes),
                        FilePath = $"/var/lib/vshield/recordings/cam-{camera.CameraId:000}/segment-{startedAt:yyyyMMddHHmm}.mp4",
                        FileSizeBytes = 18_000_000 + i * 2_500_000,
                        DurationSeconds = durationMinutes * 60,
                        StorageUrl = $"demo://recordings/cam-{camera.CameraId:000}/segment-{startedAt:yyyyMMddHHmm}.mp4"
                    });
                }
            }

            db.RecordedSegments.AddRange(segments);
        }

        if (!db.CameraPlates.Any())
        {
            var plateCameras = db.Cameras
                .Where(camera => camera.CameraType == "Plate")
                .OrderBy(camera => camera.CameraId)
                .Take(8)
                .ToList();

            db.CameraPlates.AddRange(plateCameras
                .Select((camera, index) => new CameraPlate
                {
                    CameraIP = $"10.20.{camera.CameraId % 10}.{20 + index}",
                    PlateNumber = index % 3 == 0 ? $"51H-{70000 + index * 13}" : $"29A-{30000 + index * 17}",
                    X1 = 80 + index * 3,
                    Y1 = 120 + index * 2,
                    X2 = 280 + index * 4,
                    Y2 = 200 + index * 3,
                    LastUpdate = now.AddMinutes(-(index + 1) * 4)
                }));
        }
    }

    private static void SeedEvidenceGovernanceData(ApplicationDbContext db, DateTime now)
    {
        var evidenceItems = db.EvidenceItems.OrderBy(item => item.EvidenceItemId).ToList();
        var adminUser = db.AppUsers.FirstOrDefault(user => user.Role == "Admin");
        var guardUser = db.AppUsers.FirstOrDefault(user => user.Role == "BaoVe");

        if (!db.EvidenceCollections.Any() && evidenceItems.Count > 0)
        {
            db.EvidenceCollections.AddRange(
                new EvidenceCollection
                {
                    Name = "Bộ hồ sơ điều tra - bất thường cổng",
                    Purpose = "Investigation",
                    Status = "Open",
                    CreatedByUserId = adminUser?.UserId,
                    CreatedAtUtc = now.AddDays(-2)
                },
                new EvidenceCollection
                {
                    Name = "Mẫu kiểm tra tuân thủ hàng tuần",
                    Purpose = "Compliance",
                    Status = "Locked",
                    CreatedByUserId = adminUser?.UserId,
                    CreatedAtUtc = now.AddDays(-7),
                    BundleHash = new string('c', 64)
                });
            db.SaveChanges();
        }

        var collections = db.EvidenceCollections.OrderBy(collection => collection.EvidenceCollectionId).ToList();
        if (!db.EvidenceCollectionItems.Any() && collections.Count > 0 && evidenceItems.Count > 0)
        {
            db.EvidenceCollectionItems.AddRange(evidenceItems.Take(4).Select((item, index) => new EvidenceCollectionItem
            {
                EvidenceCollectionId = collections[index % collections.Count].EvidenceCollectionId,
                EvidenceItemId = item.EvidenceItemId,
                AddedAtUtc = now.AddHours(-(index + 4))
            }));
        }

        if (!db.EvidenceAccessLogs.Any() && evidenceItems.Count > 0)
        {
            db.EvidenceAccessLogs.AddRange(evidenceItems.Take(5).Select((item, index) => new EvidenceAccessLog
            {
                EvidenceItemId = item.EvidenceItemId,
                UserId = index % 2 == 0 ? adminUser?.UserId : guardUser?.UserId,
                AccessType = index % 3 == 0 ? "Download" : "Read",
                Purpose = index % 3 == 0 ? "Chuẩn bị xuất hồ sơ xem xét sự cố" : "Xác minh định kỳ và kiểm tra tuân thủ ngẫu nhiên",
                AccessedAtUtc = now.AddHours(-(index + 1))
            }));
        }

        if (!db.RetentionPolicies.Any())
        {
            db.RetentionPolicies.AddRange(
                new RetentionPolicy
                {
                    Name = "Bằng chứng sự cố 2 năm",
                    EvidenceType = "Snapshot",
                    RetentionCategory = "Incident",
                    RetentionDays = 730,
                    PurgeMode = "ReviewRequired",
                    IsActive = true
                },
                new RetentionPolicy
                {
                    Name = "Bản ghi vận hành 90 ngày",
                    EvidenceType = "Recording",
                    RetentionCategory = "Operational",
                    RetentionDays = 90,
                    PurgeMode = "AutoAfterReview",
                    IsActive = true
                });
        }

        if (!db.ChainOfCustodyEntries.Any() && evidenceItems.Count > 0)
        {
            db.ChainOfCustodyEntries.AddRange(evidenceItems.Take(3).SelectMany((item, index) => new[]
            {
                new ChainOfCustodyEntry
                {
                    EvidenceItemId = item.EvidenceItemId,
                    Action = "Registered",
                    ActorUserId = guardUser?.UserId,
                    ToCustodian = "Trung tâm điều hành an ninh (SOC)",
                    HashAfter = item.HashSha256,
                    Note = "Bằng chứng được thu thập từ quy trình an ninh tự động.",
                    CreatedAtUtc = now.AddHours(-(18 - index))
                },
                new ChainOfCustodyEntry
                {
                    EvidenceItemId = item.EvidenceItemId,
                    Action = "Transferred",
                    ActorUserId = adminUser?.UserId,
                    FromCustodian = "Trung tâm điều hành an ninh (SOC)",
                    ToCustodian = "Văn phòng tuân thủ",
                    HashBefore = item.HashSha256,
                    HashAfter = item.HashSha256,
                    Note = "Bàn giao để kiểm tra tuân thủ định kỳ hàng tháng.",
                    CreatedAtUtc = now.AddHours(-(12 - index))
                }
            }));
        }

        if (!db.EvidenceExportRequests.Any() && evidenceItems.Count > 0)
        {
            db.EvidenceExportRequests.AddRange(
                new EvidenceExportRequest
                {
                    EvidenceItemId = evidenceItems[0].EvidenceItemId,
                    Purpose = "Chia sẻ với chỉ huy sự cố nội bộ",
                    Recipient = "soc-commander@vshield-demo.vn",
                    Status = "Approved",
                    RequestedByUserId = guardUser?.UserId,
                    ApprovedByUserId = adminUser?.UserId,
                    RequestedAtUtc = now.AddHours(-10),
                    ApprovedAtUtc = now.AddHours(-9),
                    ExportHash = new string('d', 64),
                    Watermark = "DEMO-MẬT",
                    SignatureReference = "sig://demo/export/001"
                },
                new EvidenceExportRequest
                {
                    EvidenceCollectionId = collections.FirstOrDefault()?.EvidenceCollectionId,
                    Purpose = "Xem lại gói tuân thủ hàng tuần",
                    Recipient = "compliance@vshield-demo.vn",
                    Status = "PendingApproval",
                    RequestedByUserId = adminUser?.UserId,
                    RequestedAtUtc = now.AddHours(-2)
                });
        }

        if (!db.RedactionRequests.Any() && evidenceItems.Count > 1)
        {
            db.RedactionRequests.AddRange(
                new RedactionRequest
                {
                    EvidenceItemId = evidenceItems[1].EvidenceItemId,
                    Reason = "Che dữ liệu cá nhân khách trước khi dùng cho đào tạo.",
                    PrivacyLabel = "PersonalData",
                    Status = "Verified",
                    RequestedByUserId = adminUser?.UserId,
                    ApprovedByUserId = adminUser?.UserId,
                    PerformedByUserId = guardUser?.UserId,
                    VerifiedByUserId = adminUser?.UserId,
                    RequestedAtUtc = now.AddHours(-16),
                    ApprovedAtUtc = now.AddHours(-15),
                    PerformedAtUtc = now.AddHours(-14),
                    VerifiedAtUtc = now.AddHours(-13),
                    RedactedStorageReference = "demo://evidence/redacted/manual-pass-51c-33333.jpg"
                });
        }

        if (!db.LegalHolds.Any() && evidenceItems.Count > 0 && collections.Count > 0)
        {
            db.LegalHolds.Add(new LegalHold
            {
                EvidenceCollectionId = collections[0].EvidenceCollectionId,
                Reason = "Giữ nguyên bộ hồ sơ trong khi chờ kết thúc điều tra nội bộ.",
                Status = "Active",
                AppliedByUserId = adminUser?.UserId,
                AppliedAtUtc = now.AddHours(-6)
            });
        }

        if (!db.ComplianceReportRuns.Any())
        {
            db.ComplianceReportRuns.AddRange(
                new ComplianceReportRun
                {
                    ReportType = "EvidenceRetention",
                    PeriodStartUtc = now.AddDays(-30),
                    PeriodEndUtc = now,
                    Status = "Completed",
                    OutputReference = "demo://compliance/retention-2026-06.pdf",
                    RequestedByUserId = adminUser?.UserId,
                    CreatedAtUtc = now.AddHours(-20),
                    CompletedAtUtc = now.AddHours(-19)
                },
                new ComplianceReportRun
                {
                    ReportType = "WatchlistAndVisitorAudit",
                    PeriodStartUtc = now.AddDays(-7),
                    PeriodEndUtc = now,
                    Status = "Completed",
                    OutputReference = "demo://compliance/watchlist-visitor-2026-week26.pdf",
                    RequestedByUserId = adminUser?.UserId,
                    CreatedAtUtc = now.AddHours(-11),
                    CompletedAtUtc = now.AddHours(-10)
                });
        }
    }

    private static void SeedAccessPolicyAndEmergencyData(ApplicationDbContext db, List<Employee> employees, DateTime now)
    {
        if (!db.AccessSchedules.Any())
        {
            db.AccessSchedules.AddRange(
                new AccessSchedule
                {
                    Name = "Giờ văn phòng",
                    StartTime = new TimeSpan(7, 30, 0),
                    EndTime = new TimeSpan(19, 0, 0),
                    DaysOfWeek = "Mon,Tue,Wed,Thu,Fri",
                    IsActive = true
                },
                new AccessSchedule
                {
                    Name = "Ca nhà máy 24x6",
                    StartTime = TimeSpan.Zero,
                    EndTime = new TimeSpan(23, 59, 0),
                    DaysOfWeek = "Mon,Tue,Wed,Thu,Fri,Sat",
                    IsActive = true
                },
                new AccessSchedule
                {
                    Name = "Khẩn cấp cuối tuần",
                    StartTime = TimeSpan.Zero,
                    EndTime = new TimeSpan(23, 59, 0),
                    DaysOfWeek = "Sat,Sun",
                    IsActive = true
                });
        }

        var sites = db.Sites.OrderBy(site => site.SiteId).ToList();
        if (!db.HolidayCalendars.Any() && sites.Count > 0)
        {
            db.HolidayCalendars.AddRange(sites.SelectMany((site, index) => new[]
            {
                new HolidayCalendar
                {
                    SiteId = site.SiteId,
                    Name = $"{site.Code} Ngày quốc khánh",
                    HolidayDate = new DateTime(now.Year, 9, 2),
                    Note = "Chế độ nhân sự ngày lễ quốc gia"
                },
                new HolidayCalendar
                {
                    SiteId = site.SiteId,
                    Name = $"{site.Code} Diễn tập phòng cháy",
                    HolidayDate = now.Date.AddDays(10 + index),
                    Note = "Diễn tập toàn cơ sở với lịch tạm thời bị thay đổi"
                }
            }));
        }

        if (!db.AccessLevels.Any())
        {
            db.AccessLevels.AddRange(
                new AccessLevel { Name = "Nhân viên chung", Code = "GENERAL", Description = "Khu văn phòng và khu vận hành thông thường", RequiresApproval = false, IsActive = true },
                new AccessLevel { Name = "Vận hành hạn chế", Code = "RESTRICTED", Description = "Khu sản xuất và logistics hạn chế", RequiresApproval = true, IsActive = true },
                new AccessLevel { Name = "Khu vực SOC mức nghiêm trọng", Code = "SOC_CRIT", Description = "Phòng SOC và khu máy chủ", RequiresApproval = true, IsActive = true });
        }

        if (!db.AccessGroups.Any())
        {
            db.AccessGroups.AddRange(
                new AccessGroup { Name = "Nhân viên văn phòng HQ", Code = "HQ_OFFICE", IsActive = true },
                new AccessGroup { Name = "Vận hành nhà máy", Code = "FACTORY_OPS", IsActive = true },
                new AccessGroup { Name = "Ứng phó an ninh", Code = "SECURITY_RESP", IsActive = true });
        }

        db.SaveChanges();

        var schedules = db.AccessSchedules.OrderBy(schedule => schedule.AccessScheduleId).ToList();
        var levels = db.AccessLevels.OrderBy(level => level.AccessLevelId).ToList();
        var groups = db.AccessGroups.OrderBy(group => group.AccessGroupId).ToList();
        var zones = db.SecurityZones.OrderBy(zone => zone.SecurityZoneId).ToList();
        var accessPoints = db.AccessPoints.OrderBy(accessPoint => accessPoint.AccessPointId).ToList();
        var adminUser = db.AppUsers.FirstOrDefault(user => user.Role == "Admin");
        var guardUsers = db.AppUsers.Where(user => user.Role == "BaoVe").OrderBy(user => user.UserId).Take(4).ToList();

        if (!db.AccessPolicyVersions.Any())
        {
            db.AccessPolicyVersions.AddRange(
                new AccessPolicyVersion
                {
                    Name = "Chính sách cơ sở Quý 2/2026",
                    Status = "Active",
                    ChangeSummary = "Chuẩn hóa quyền truy cập nhân viên, kiểm soát chặt hơn khu SOC và dự phòng QR động.",
                    CreatedAtUtc = now.AddDays(-45),
                    SubmittedAtUtc = now.AddDays(-44),
                    ApprovedAtUtc = now.AddDays(-43),
                    ActivatedAtUtc = now.AddDays(-42),
                    CreatedByUserId = adminUser?.UserId,
                    ApprovedByUserId = adminUser?.UserId
                },
                new AccessPolicyVersion
                {
                    Name = "Siết chặt khách tham quan Quý 3/2026",
                    Status = "Draft",
                    ChangeSummary = "Bổ sung quy tắc khách phải đi kèm cho khu sản xuất và logistics ngoài giờ.",
                    CreatedAtUtc = now.AddDays(-5),
                    CreatedByUserId = adminUser?.UserId
                });
            db.SaveChanges();
        }

        var policyVersions = db.AccessPolicyVersions.OrderBy(policy => policy.AccessPolicyVersionId).ToList();
        if (!db.AccessRules.Any() && levels.Count > 0 && groups.Count > 0 && schedules.Count > 0 && zones.Count > 0 && accessPoints.Count > 0)
        {
            var activePolicyId = policyVersions.FirstOrDefault(policy => policy.Status == "Active")?.AccessPolicyVersionId;
            var rules = new List<AccessRule>();
            if (sites.Count > 0)
            {
                rules.Add(new AccessRule
                {
                    AccessPolicyVersionId = activePolicyId,
                    AccessLevelId = levels[0].AccessLevelId,
                    AccessGroupId = groups[0].AccessGroupId,
                    SiteId = sites[0].SiteId,
                    SecurityZoneId = zones.First().SecurityZoneId,
                    AccessPointId = accessPoints.First().AccessPointId,
                    AccessScheduleId = schedules[0].AccessScheduleId,
                    SubjectType = "Group",
                    CredentialType = "DynamicQr",
                    AllowAccess = true,
                    ValidFromUtc = now.AddDays(-60),
                    IsActive = true
                });
            }

            if (sites.Count > 1 && zones.Count > 2)
            {
                rules.Add(new AccessRule
                {
                    AccessPolicyVersionId = activePolicyId,
                    AccessLevelId = levels[Math.Min(1, levels.Count - 1)].AccessLevelId,
                    AccessGroupId = groups[Math.Min(1, groups.Count - 1)].AccessGroupId,
                    SiteId = sites[1].SiteId,
                    SecurityZoneId = zones[2].SecurityZoneId,
                    AccessPointId = accessPoints[Math.Min(2, accessPoints.Count - 1)].AccessPointId,
                    AccessScheduleId = schedules[Math.Min(1, schedules.Count - 1)].AccessScheduleId,
                    SubjectType = "Group",
                    CredentialType = "DynamicQrAndPlate",
                    AllowAccess = true,
                    ValidFromUtc = now.AddDays(-60),
                    IsActive = true
                });
            }

            if (zones.Count > 0)
            {
                rules.Add(new AccessRule
                {
                    AccessPolicyVersionId = activePolicyId,
                    AccessLevelId = levels[^1].AccessLevelId,
                    AccessGroupId = groups[^1].AccessGroupId,
                    SiteId = sites.FirstOrDefault()?.SiteId,
                    SecurityZoneId = zones.Last().SecurityZoneId,
                    AccessPointId = accessPoints.Last().AccessPointId,
                    AccessScheduleId = schedules[0].AccessScheduleId,
                    SubjectType = "Role",
                    CredentialType = "Any",
                    AllowAccess = true,
                    ValidFromUtc = now.AddDays(-60),
                    IsActive = true
                });
            }

            db.AccessRules.AddRange(rules);
        }

        if (!db.TemporaryAccessGrants.Any() && employees.Count > 0 && accessPoints.Count > 0)
        {
            db.TemporaryAccessGrants.AddRange(employees
                .Where(employee => employee.Status == true)
                .Take(6)
                .Select((employee, index) => new TemporaryAccessGrant
                {
                    SubjectType = "Employee",
                    SubjectId = employee.EmployeeId,
                    SiteId = employee.PrimarySiteId,
                    SecurityZoneId = zones[index % zones.Count].SecurityZoneId,
                    AccessPointId = accessPoints[index % accessPoints.Count].AccessPointId,
                    ValidFromUtc = now.AddHours(-(index + 1)),
                    ValidToUtc = now.AddHours(8 + index),
                    Reason = index % 2 == 0 ? "Gia hạn đi kèm kiểm toán tạm thời" : "Ghi đè truy cập trong cửa sổ bảo trì",
                    ApprovedByUserId = adminUser?.UserId,
                    IsRevoked = index == 5
                }));
        }

        if (!db.AccessDecisions.Any() && policyVersions.Count > 0 && employees.Count > 0)
        {
            var activePolicy = policyVersions.First();
            db.AccessDecisions.AddRange(employees.Take(16).Select((employee, index) => new AccessDecision
            {
                AccessPolicyVersionId = activePolicy.AccessPolicyVersionId,
                SubjectType = "Employee",
                SubjectId = employee.EmployeeId,
                SiteId = employee.PrimarySiteId,
                SecurityZoneId = zones[index % zones.Count].SecurityZoneId,
                AccessPointId = accessPoints[index % accessPoints.Count].AccessPointId,
                CredentialType = index % 3 == 0 ? "DynamicQrAndPlate" : "DynamicQr",
                Result = index % 7 == 0 ? AccessDecisionResults.Review : index % 5 == 0 ? AccessDecisionResults.Deny : AccessDecisionResults.Allow,
                Reason = index % 7 == 0 ? "Chính sách mô phỏng không khớp cho truy cập ngoài giờ." :
                    index % 5 == 0 ? "Bị từ chối do lịch trình không khớp và hạn chế khu vực." :
                    "Truy cập được cấp theo chính sách cơ sở đang hoạt động.",
                DecisionMode = index % 7 == 0 ? "Shadow" : "Enforced",
                LegacyResult = index % 5 == 0 ? "Denied" : "Granted",
                ShadowMismatch = index % 7 == 0,
                EvaluatedAtUtc = now.AddMinutes(-(index * 12 + 5)),
                EvaluatedByUserId = index % 4 == 0 ? adminUser?.UserId : null
            }));
        }

        if (!db.AntiPassbackStates.Any() && employees.Count > 0)
        {
            db.AntiPassbackStates.AddRange(employees.Take(12).Select((employee, index) => new AntiPassbackState
            {
                SubjectType = "Employee",
                SubjectId = employee.EmployeeId,
                SecurityZoneId = zones[index % zones.Count].SecurityZoneId,
                State = index % 5 == 0 ? "Inside" : index % 4 == 0 ? "Violation" : "Outside",
                UpdatedAtUtc = now.AddMinutes(-(index * 9)),
                IsViolated = index % 4 == 0,
                ResetReason = index % 4 == 0 ? "Đặt lại thủ công sau khi rà soát bám đuôi." : null
            }));
        }

        if (!db.OccupancySnapshots.Any() && sites.Count > 0 && zones.Count > 0)
        {
            db.OccupancySnapshots.AddRange(sites.SelectMany((site, siteIndex) =>
                zones.Where(zone => zone.SiteId == site.SiteId).Take(3).Select((zone, zoneIndex) => new OccupancySnapshot
                {
                    SiteId = site.SiteId,
                    SecurityZoneId = zone.SecurityZoneId,
                    Count = 12 + siteIndex * 18 + zoneIndex * 7,
                    MaxAllowed = zone.IsRestricted ? 40 : 120,
                    CapturedAtUtc = now.AddMinutes(-(siteIndex * 20 + zoneIndex * 8))
                })));
        }

        if (!db.DuressEvents.Any() && employees.Count > 0)
        {
            db.DuressEvents.AddRange(employees.Take(3).Select((employee, index) => new DuressEvent
            {
                UserId = guardUsers.ElementAtOrDefault(index)?.UserId,
                EmployeeId = employee.EmployeeId,
                AccessPointId = accessPoints[index % accessPoints.Count].AccessPointId,
                SecurityZoneId = zones[index % zones.Count].SecurityZoneId,
                SiteId = employee.PrimarySiteId,
                CredentialType = "PanicQr",
                Description = index == 0
                    ? "Bảo vệ kích hoạt cấp cứu khi xác minh thủ công với tài xế có hành vi hung hăng."
                    : "Kịch bản diễn tập cấp cứu im lặng để kiểm tra sự sẵn sàng của SOC.",
                Latitude = 21.0284m + index / 1000m,
                Longitude = 105.8045m + index / 1000m,
                IsAcknowledged = index > 0,
                OccurredAtUtc = now.AddMinutes(-(45 + index * 11)),
                AcknowledgedAtUtc = index > 0 ? now.AddMinutes(-(32 + index * 9)) : null,
                AcknowledgedByUserId = index > 0 ? adminUser?.UserId : null
            }));
        }

        if (!db.EmergencyStates.Any() && sites.Count > 0)
        {
            db.EmergencyStates.AddRange(
                new EmergencyState
                {
                    State = "Normal",
                    SiteId = sites[0].SiteId,
                    SecurityZoneId = zones.FirstOrDefault(zone => zone.SiteId == sites[0].SiteId)?.SecurityZoneId,
                    AccessPointId = accessPoints.FirstOrDefault(point => point.SiteId == sites[0].SiteId)?.AccessPointId,
                    Reason = "Cơ sở hoạt động định kỳ bình thường.",
                    IsActive = true,
                    StartedAtUtc = now.AddDays(-3),
                    CreatedByUserId = adminUser?.UserId
                },
                new EmergencyState
                {
                    State = "Drill",
                    SiteId = sites.Last().SiteId,
                    SecurityZoneId = zones.Last().SecurityZoneId,
                    AccessPointId = accessPoints.Last().AccessPointId,
                    Reason = "Diễn tập bàn giấy sơ tán kho bãi.",
                    IsActive = false,
                    StartedAtUtc = now.AddDays(-2),
                    EndedAtUtc = now.AddDays(-2).AddHours(1),
                    CreatedByUserId = adminUser?.UserId
                });
        }
    }

    private static void SeedLostFoundAndLockerData(ApplicationDbContext db, DateTime now)
    {
        var receptionUser = db.AppUsers.FirstOrDefault(user => user.Role == "LeTan");
        var adminUser = db.AppUsers.FirstOrDefault(user => user.Role == "Admin");

        if (!db.LockerCabinets.Any())
        {
            db.LockerCabinets.AddRange(
                new LockerCabinet
                {
                    Name = "Tủ lễ tân A",
                    Location = "Phòng sau lễ tân HN",
                    Description = "Kho lưu trữ thất lạc chính gần quầy lễ tân",
                    IsActive = true,
                    CreatedAtUtc = now.AddDays(-30)
                },
                new LockerCabinet
                {
                    Name = "Tủ an ninh nhà máy",
                    Location = "Phòng kho trạm bảo vệ BN",
                    Description = "Tủ giữ đồ có giá trị khu sản xuất, hạn chế truy cập",
                    IsActive = true,
                    CreatedAtUtc = now.AddDays(-24)
                });
            db.SaveChanges();
        }

        if (!db.LockerCompartments.Any())
        {
            var cabinets = db.LockerCabinets.OrderBy(cabinet => cabinet.LockerCabinetId).ToList();
            db.LockerCompartments.AddRange(cabinets.SelectMany((cabinet, cabinetIndex) =>
                Enumerable.Range(1, 4).Select(slot => new LockerCompartment
                {
                    LockerCabinetId = cabinet.LockerCabinetId,
                    Code = $"{(cabinetIndex == 0 ? "A" : "F")}{slot:00}",
                    Status = slot == 1 ? "Occupied" : slot == 4 ? "Maintenance" : "Empty"
                })));
            db.SaveChanges();
        }

        var compartments = db.LockerCompartments.OrderBy(compartment => compartment.LockerCompartmentId).ToList();

        if (!db.LostItemReports.Any())
        {
            db.LostItemReports.AddRange(
                new LostItemReport
                {
                    ReporterName = "Nguyen Thi Huong",
                    ReporterPhone = "0912345678",
                    ReporterEmail = "huong.visitor@example.com",
                    ReporterIdNumber = "079123456789",
                    ItemDescription = "iPhone 14 màu đen có ốp lưng xanh và ghi chú họp nhân viên.",
                    LastSeenLocation = "Khu ghế chờ lễ tân HN",
                    LostAtUtc = now.AddHours(-14),
                    PhotoUrl = "/uploads/evidence/demo/lost-phone.jpg",
                    Status = "Matched",
                    CreatedByUserId = receptionUser?.UserId,
                    CreatedAtUtc = now.AddHours(-13)
                },
                new LostItemReport
                {
                    ReporterName = "Tran Van Binh",
                    ReporterPhone = "0988112233",
                    ReporterEmail = "binh.contractor@example.com",
                    ReporterIdNumber = "051998877665",
                    ItemDescription = "Thẻ ra vào công ty trong vỏ đỏ.",
                    LastSeenLocation = "Bàn khu tủ tại cổng nhân viên BN",
                    LostAtUtc = now.AddHours(-9),
                    PhotoUrl = "/uploads/evidence/demo/lost-badge.jpg",
                    Status = "Open",
                    CreatedByUserId = receptionUser?.UserId,
                    CreatedAtUtc = now.AddHours(-8)
                });
            db.SaveChanges();
        }

        var evidenceItem = db.EvidenceItems.OrderBy(item => item.EvidenceItemId).FirstOrDefault();
        if (!db.FoundItemReports.Any())
        {
            db.FoundItemReports.AddRange(
                new FoundItemReport
                {
                    FoundByName = "Le Thi Lan",
                    FoundByPhone = "0903123123",
                    FoundByIdNumber = "034455667788",
                    FoundLocation = "Khu ghế sofa lễ tân HN",
                    FoundAtUtc = now.AddHours(-12),
                    ItemDescription = "iPhone 14 màu đen, màn hình khóa hiển thị tên Nguyen Thi Huong.",
                    PhotoUrl = "/uploads/evidence/demo/found-phone.jpg",
                    StorageLocation = "Tủ lễ tân A / A01",
                    LockerCompartmentId = compartments.FirstOrDefault()?.LockerCompartmentId,
                    ItemEvidenceId = evidenceItem?.EvidenceItemId,
                    Status = "Matched",
                    CreatedByUserId = receptionUser?.UserId,
                    CreatedAtUtc = now.AddHours(-11)
                },
                new FoundItemReport
                {
                    FoundByName = "Pham Quoc Huy",
                    FoundByPhone = "0911223344",
                    FoundByIdNumber = "066677788899",
                    FoundLocation = "Khay tại cổng nhân viên BN",
                    FoundAtUtc = now.AddHours(-6),
                    ItemDescription = "Thẻ RFID với dây đeo đỏ đã phai màu.",
                    PhotoUrl = "/uploads/evidence/demo/found-badge.jpg",
                    StorageLocation = "Tủ an ninh nhà máy / F02",
                    LockerCompartmentId = compartments.Skip(1).FirstOrDefault()?.LockerCompartmentId,
                    Status = "Unclaimed",
                    CreatedByUserId = adminUser?.UserId,
                    CreatedAtUtc = now.AddHours(-5)
                });
            db.SaveChanges();
        }

        if (!db.ItemMatches.Any())
        {
            var lost = db.LostItemReports.OrderBy(item => item.LostItemReportId).ToList();
            var found = db.FoundItemReports.OrderBy(item => item.FoundItemReportId).ToList();
            if (lost.Count > 0 && found.Count > 0)
            {
                db.ItemMatches.Add(new ItemMatch
                {
                    LostItemReportId = lost[0].LostItemReportId,
                    FoundItemReportId = found[0].FoundItemReportId,
                    ConfidenceScore = 0.96,
                    MatchedByUserId = adminUser?.UserId,
                    MatchedAtUtc = now.AddHours(-10),
                    Status = "Confirmed",
                    Note = "Màu sắc, model thiết bị và chi tiết màn hình khóa của chủ sở hữu đều khớp."
                });
            }
        }

        if (!db.ClaimRequests.Any())
        {
            var lost = db.LostItemReports.OrderBy(item => item.LostItemReportId).ToList();
            var found = db.FoundItemReports.OrderBy(item => item.FoundItemReportId).ToList();
            if (found.Count > 0)
            {
                db.ClaimRequests.AddRange(
                    new ClaimRequest
                    {
                        FoundItemReportId = found[0].FoundItemReportId,
                        LostItemReportId = lost.FirstOrDefault()?.LostItemReportId,
                        ClaimantName = "Nguyen Thi Huong",
                        ClaimantIdNumber = "079123456789",
                        ClaimantPhone = "0912345678",
                        ProofDocumentUrl = "/uploads/evidence/demo/claim-proof-phone.pdf",
                        ClaimantPhotoUrl = "/uploads/evidence/demo/claimant-huong.jpg",
                        ItemPhotoUrl = "/uploads/evidence/demo/found-phone.jpg",
                        Status = "Approved",
                        ReviewedByUserId = adminUser?.UserId,
                        ReviewNote = "Đã xác nhận qua IMEI và nhận diện màn hình khóa.",
                        RequestedAtUtc = now.AddHours(-9),
                        ReviewedAtUtc = now.AddHours(-8),
                        CompletedAtUtc = now.AddHours(-7),
                        CompletedByUserId = receptionUser?.UserId,
                        WitnessName = "Le Thi Lan",
                        HandoverNote = "Đã trả lại tại lễ tân với nhân chứng và xác minh giấy tờ hoàn tất.",
                        ReturnPhotoUrl = "/uploads/evidence/demo/return-phone.jpg"
                    },
                    new ClaimRequest
                    {
                        FoundItemReportId = found.Last().FoundItemReportId,
                        ClaimantName = "Tran Van Binh",
                        ClaimantIdNumber = "051998877665",
                        ClaimantPhone = "0988112233",
                        ProofDocumentUrl = "/uploads/evidence/demo/claim-proof-badge.pdf",
                        ClaimantPhotoUrl = "/uploads/evidence/demo/claimant-binh.jpg",
                        ItemPhotoUrl = "/uploads/evidence/demo/found-badge.jpg",
                        Status = "Pending",
                        RequestedAtUtc = now.AddHours(-2)
                    });
            }
        }

        if (!db.LockerAccessLogs.Any() && compartments.Count > 0)
        {
            db.LockerAccessLogs.AddRange(
                new LockerAccessLog
                {
                    LockerCompartmentId = compartments[0].LockerCompartmentId,
                    UserId = receptionUser?.UserId,
                    Action = "StoreItem",
                    Purpose = "Đã cất iPhone tìm thấy trong lúc chờ xác minh chủ sở hữu.",
                    Timestamp = now.AddHours(-11)
                },
                new LockerAccessLog
                {
                    LockerCompartmentId = compartments[0].LockerCompartmentId,
                    UserId = adminUser?.UserId,
                    Action = "ReviewAccess",
                    Purpose = "Rà soát tuân thủ có nhân chứng trước khi bàn giao đã phê duyệt.",
                    Timestamp = now.AddHours(-8)
                },
                new LockerAccessLog
                {
                    LockerCompartmentId = compartments[0].LockerCompartmentId,
                    UserId = receptionUser?.UserId,
                    Action = "ReleaseItem",
                    Purpose = "Đã bàn giao vật phẩm yêu cầu đã phê duyệt cho chủ sở hữu đã xác minh.",
                    Timestamp = now.AddHours(-7)
                });
        }
    }

    private static void SeedSocAwarenessAndChatData(ApplicationDbContext db, List<Employee> employees, DateTime now)
    {
        var sites = db.Sites.OrderBy(site => site.SiteId).ToList();
        var zones = db.SecurityZones.OrderBy(zone => zone.SecurityZoneId).ToList();
        var accessPoints = db.AccessPoints.OrderBy(point => point.AccessPointId).ToList();
        var cameras = db.Cameras.OrderBy(camera => camera.CameraId).ToList();
        var devices = db.SecurityDevices.OrderBy(device => device.SecurityDeviceId).ToList();
        var adminUser = db.AppUsers.FirstOrDefault(user => user.Role == "Admin");
        var guardUser = db.AppUsers.FirstOrDefault(user => user.Role == "BaoVe");
        var managerUser = db.AppUsers.FirstOrDefault(user => user.Role == "QuanLy");

        if (!db.SecurityEvents.Any() && sites.Count > 0 && zones.Count > 0 && accessPoints.Count > 0)
        {
            var vehicles = db.Vehicles.OrderBy(vehicle => vehicle.VehicleId).Take(4).ToList();
            db.SecurityEvents.AddRange(new[]
            {
                new SecurityEvent
                {
                    SourceType = "LaneEvent",
                    SourceId = "DEMO-LANE-001",
                    EventType = "TailgatingSuspected",
                    Severity = "High",
                    SiteId = sites[0].SiteId,
                    SecurityZoneId = zones[0].SecurityZoneId,
                    AccessPointId = accessPoints[0].AccessPointId,
                    SubjectType = "Employee",
                    SubjectId = employees.FirstOrDefault()?.EmployeeId,
                    Confidence = 0.84m,
                    Summary = "Hai người cùng đi qua một vòng quay cổng soát vé trong khung giờ cao điểm ra vào.",
                    OccurredAtUtc = now.AddMinutes(-58),
                    SiteNameSnapshot = sites[0].Name,
                    SecurityZoneNameSnapshot = zones[0].Name,
                    AccessPointNameSnapshot = accessPoints[0].Name
                },
                new SecurityEvent
                {
                    SourceType = "PlateCamera",
                    SourceId = "DEMO-PLATE-002",
                    EventType = "ManifestMismatch",
                    Severity = "Critical",
                    SiteId = sites.Last().SiteId,
                    SecurityZoneId = zones.Last().SecurityZoneId,
                    AccessPointId = accessPoints.Last().AccessPointId,
                    SubjectType = "Vehicle",
                    VehicleId = vehicles.LastOrDefault()?.VehicleId,
                    PlateText = vehicles.LastOrDefault()?.LicensePlate,
                    Confidence = 0.92m,
                    Summary = "Xe tải vào làn logistics với biển số gắn với manifest đã bị đình chỉ.",
                    OccurredAtUtc = now.AddMinutes(-34),
                    SiteNameSnapshot = sites.Last().Name,
                    SecurityZoneNameSnapshot = zones.Last().Name,
                    AccessPointNameSnapshot = accessPoints.Last().Name
                }
            });
            db.SaveChanges();
        }

        var securityEvents = db.SecurityEvents.OrderBy(item => item.SecurityEventId).ToList();
        if (!db.EventCorrelations.Any() && securityEvents.Count > 0)
        {
            db.EventCorrelations.AddRange(
                new EventCorrelation
                {
                    CorrelationId = securityEvents[0].CorrelationId,
                    RuleName = "Tương quan bám đuôi + chống passback",
                    Severity = "High",
                    Summary = "Đã tương quan bám đuôi, vi phạm chống passback và rà soát thủ công trong cùng cửa sổ 10 phút.",
                    CreatedAtUtc = now.AddMinutes(-50)
                },
                new EventCorrelation
                {
                    CorrelationId = securityEvents.Last().CorrelationId,
                    RuleName = "Nhóm bất thường phương tiện",
                    Severity = "Critical",
                    Summary = "Đã liên kết manifest không khớp với hoạt động watchlist và ghi đè làn.",
                    CreatedAtUtc = now.AddMinutes(-28)
                });
        }

        if (!db.VideoBookmarks.Any() && securityEvents.Count > 0 && cameras.Count > 0)
        {
            db.VideoBookmarks.AddRange(securityEvents.Select((securityEvent, index) => new VideoBookmark
            {
                SecurityEventId = securityEvent.SecurityEventId,
                CameraId = cameras[index % cameras.Count].CameraId,
                ArtifactReference = $"demo://video-bookmarks/event-{securityEvent.SecurityEventId:000}.mp4",
                StartUtc = securityEvent.OccurredAtUtc.AddMinutes(-2),
                EndUtc = securityEvent.OccurredAtUtc.AddMinutes(3),
                Note = "Đoạn clip ghim để điều tra và triển khai nhiệm vụ điều phối."
            }));
        }

        if (!db.SiteMaps.Any() && sites.Count > 0)
        {
            db.SiteMaps.AddRange(sites.Select(site => new SiteMap
            {
                SiteId = site.SiteId,
                Name = $"{site.Code} Operations Map",
                AssetReference = $"demo://maps/{site.Code.ToLowerInvariant()}-operations.svg",
                CoordinateSystem = "Normalized",
                IsActive = true
            }));
            db.SaveChanges();
        }

        if (!db.MapDevicePlacements.Any())
        {
            var maps = db.SiteMaps.OrderBy(map => map.SiteMapId).ToList();
            if (maps.Count > 0)
            {
                var placements = new List<MapDevicePlacement>();
                foreach (var (map, index) in maps.Select((map, index) => (map, index)))
                {
                    var siteCameras = cameras.Skip(index).Take(2).ToList();
                    foreach (var (camera, cameraIndex) in siteCameras.Select((camera, cameraIndex) => (camera, cameraIndex)))
                    {
                        placements.Add(new MapDevicePlacement
                        {
                            SiteMapId = map.SiteMapId,
                            CameraId = camera.CameraId,
                            X = 0.18m + index * 0.11m + cameraIndex * 0.09m,
                            Y = 0.22m + cameraIndex * 0.14m,
                            IconType = "Camera"
                        });
                    }

                    foreach (var (device, deviceIndex) in devices.Take(2).Select((device, deviceIndex) => (device, deviceIndex)))
                    {
                        placements.Add(new MapDevicePlacement
                        {
                            SiteMapId = map.SiteMapId,
                            SecurityDeviceId = device.SecurityDeviceId,
                            X = 0.42m + index * 0.1m + deviceIndex * 0.08m,
                            Y = 0.3m + deviceIndex * 0.16m,
                            IconType = "Reader"
                        });
                    }
                }

                db.MapDevicePlacements.AddRange(placements);
            }
        }

        if (!db.AlarmRules.Any())
        {
            db.AlarmRules.AddRange(
                new AlarmRule { Name = "Chuyển cấp bám đuôi", EventType = "TailgatingSuspected", Severity = "High", IsActive = true },
                new AlarmRule { Name = "Manifest không khớp mức nghiêm trọng", EventType = "ManifestMismatch", Severity = "Critical", IsActive = true },
                new AlarmRule { Name = "Cảnh báo khách quá giờ", EventType = "VisitorOverstay", Severity = "Medium", IsActive = true });
        }

        if (!db.SopTemplates.Any())
        {
            db.SopTemplates.AddRange(
                new SopTemplate
                {
                    Name = "SOP xử lý bám đuôi",
                    AlarmType = "Generic",
                    Version = 2,
                    ChecklistJson = "[\"Xác nhận clip camera\",\"Liên hệ bảo vệ gần nhất\",\"Kiểm tra trạng thái chống passback\",\"Ghi kết quả\"]",
                    IsActive = true
                },
                new SopTemplate
                {
                    Name = "SOP điều phối bất thường phương tiện",
                    AlarmType = "DeviceOffline",
                    Version = 1,
                    ChecklistJson = "[\"Dừng xe an toàn\",\"Kiểm tra manifest\",\"Mở sự cố nếu chưa giải quyết\",\"Thu thập bằng chứng xuất kho\"]",
                    IsActive = true
                });
            db.SaveChanges();
        }

        if (!db.Incidents.Any())
        {
            var primaryAlarm = db.Alarms.OrderByDescending(alarm => alarm.AlarmId).FirstOrDefault();
            db.Incidents.AddRange(
                new Incident
                {
                    Title = "Điều tra bám đuôi tại trụ sở",
                    Severity = "High",
                    Status = "InProgress",
                    PrimaryAlarmId = primaryAlarm?.AlarmId,
                    OwnerUserId = adminUser?.UserId,
                    Outcome = "Đang chờ phỏng vấn bảo vệ và rà soát phát lại thẻ.",
                    OpenedAtUtc = now.AddMinutes(-54)
                },
                new Incident
                {
                    Title = "Manifest logistics không khớp",
                    Severity = "Critical",
                    Status = "Open",
                    PrimaryAlarmId = primaryAlarm?.AlarmId,
                    OwnerUserId = managerUser?.UserId,
                    Outcome = "Đã chuyển tới bộ phận logistics và SOC để xác minh thực địa.",
                    OpenedAtUtc = now.AddMinutes(-30)
                });
            db.SaveChanges();
        }

        var incidents = db.Incidents.OrderBy(incident => incident.IncidentId).ToList();
        if (!db.AlarmComments.Any())
        {
            var alarms = db.Alarms.OrderByDescending(alarm => alarm.AlarmId).Take(2).ToList();
            db.AlarmComments.AddRange(alarms.Select((alarm, index) => new AlarmComment
            {
                AlarmId = alarm.AlarmId,
                UserId = index % 2 == 0 ? adminUser?.UserId : guardUser?.UserId,
                Comment = index % 2 == 0
                    ? "SOC đã xác nhận video hỗ trợ và yêu cầu điều phối bảo vệ."
                    : "Bảo vệ hiện trường đã xác nhận cảnh báo và đang tiếp cận vị trí.",
                CreatedAtUtc = now.AddMinutes(-(26 - index * 4))
            }));
        }

        if (!db.SopExecutions.Any() && incidents.Count > 0)
        {
            var templates = db.SopTemplates.OrderBy(template => template.SopTemplateId).ToList();
            db.SopExecutions.AddRange(incidents.Select((incident, index) => new SopExecution
            {
                IncidentId = incident.IncidentId,
                SopTemplateId = templates[index % templates.Count].SopTemplateId,
                Status = index == 0 ? "InProgress" : "Completed",
                CompletedStepsJson = index == 0
                    ? "[\"Xác nhận clip camera\",\"Liên hệ bảo vệ gần nhất\"]"
                    : "[\"Dừng xe an toàn\",\"Kiểm tra manifest\",\"Thu thập bằng chứng xuất kho\"]",
                ExecutedByUserId = guardUser?.UserId,
                StartedAtUtc = incident.OpenedAtUtc.AddMinutes(2),
                CompletedAtUtc = index == 0 ? null : incident.OpenedAtUtc.AddMinutes(18)
            }));
        }

        if (!db.IncidentTimelineItems.Any() && incidents.Count > 0)
        {
            db.IncidentTimelineItems.AddRange(incidents.SelectMany((incident, index) => new[]
            {
                new IncidentTimelineItem
                {
                    IncidentId = incident.IncidentId,
                    ItemType = "Created",
                    Text = $"Đã mở sự cố: {incident.Title}.",
                    UserId = adminUser?.UserId,
                    CreatedAtUtc = incident.OpenedAtUtc
                },
                new IncidentTimelineItem
                {
                    IncidentId = incident.IncidentId,
                    ItemType = "Dispatch",
                    Text = index == 0 ? "Bảo vệ gần nhất đã được điều tới cổng xoay trụ sở." : "Cổng logistics đã được đóng để xác minh manifest.",
                    UserId = guardUser?.UserId,
                    CreatedAtUtc = incident.OpenedAtUtc.AddMinutes(6)
                },
                new IncidentTimelineItem
                {
                    IncidentId = incident.IncidentId,
                    ItemType = "Evidence",
                    Text = "Các clip liên quan và tài liệu kiểm toán đã được ghim để xem xét hồ sơ vụ việc.",
                    UserId = adminUser?.UserId,
                    CreatedAtUtc = incident.OpenedAtUtc.AddMinutes(11)
                }
            }));
        }

        if (!db.DispatchTasks.Any() && incidents.Count > 0)
        {
            db.DispatchTasks.AddRange(incidents.Select((incident, index) => new DispatchTask
            {
                IncidentId = incident.IncidentId,
                SiteId = sites[index % sites.Count].SiteId,
                LocationText = index == 0 ? "Dãy cổng xoay A trụ sở" : "Làn 2 cổng kho logistics HP",
                Latitude = 21.0284m + index / 100m,
                Longitude = 105.8045m + index / 100m,
                Priority = index == 0 ? "High" : "Critical",
                Status = index == 0 ? "InProgress" : "Open",
                AssignedGuardUserId = guardUser?.UserId,
                Instructions = index == 0
                    ? "Phỏng vấn nhân viên liên quan, lưu bản ghi QR và kiểm tra trạng thái chống passback."
                    : "Giữ xe an toàn, xác minh manifest, chuyển cấp ngay các sai lệch phát hiện.",
                CreatedAtUtc = now.AddMinutes(-(40 - index * 5)),
                CompletedAtUtc = index == 0 ? null : now.AddMinutes(-12)
            }));
        }

        if (!db.ShiftHandovers.Any() && sites.Count > 0)
        {
            var nextGuard = db.AppUsers.Where(user => user.Role == "BaoVe").OrderBy(user => user.UserId).Skip(1).FirstOrDefault();
            db.ShiftHandovers.Add(new ShiftHandover
            {
                SiteId = sites[0].SiteId,
                FromUserId = guardUser?.UserId,
                ToUserId = nextGuard?.UserId,
                Summary = "Xe trong danh sách theo dõi vẫn đang chờ xác minh, một lỗi thẻ nhà thầu chưa giải quyết, và vụ bám đuôi tại trụ sở vẫn đang được rà soát.",
                CreatedAtUtc = now.AddHours(-1)
            });
        }

        if (!db.EmergencyMusterSnapshots.Any() && sites.Count > 0)
        {
            var musterPoints = db.MusterPoints.OrderBy(point => point.MusterPointId).ToList();
            db.EmergencyMusterSnapshots.AddRange(sites.Select((site, index) => new EmergencyMusterSnapshot
            {
                SiteId = site.SiteId,
                MusterPointId = musterPoints.ElementAtOrDefault(index)?.MusterPointId,
                KnownOnsite = 120 + index * 45,
                AccountedFor = 118 + index * 44,
                VisitorsOnsite = 6 + index,
                UnaccountedFor = index == 1 ? 3 : 1,
                CapturedAtUtc = now.AddDays(-1).AddMinutes(index * 15)
            }));
        }

        if (!db.ClipRequests.Any() && securityEvents.Count > 0 && cameras.Count > 0)
        {
            db.ClipRequests.AddRange(securityEvents.Select((securityEvent, index) => new ClipRequest
            {
                CameraId = cameras[index % cameras.Count].CameraId,
                SecurityEventId = securityEvent.SecurityEventId,
                StartUtc = securityEvent.OccurredAtUtc.AddMinutes(-2),
                EndUtc = securityEvent.OccurredAtUtc.AddMinutes(4),
                RequestedBy = index % 2 == 0 ? "Chuyên viên SOC" : "Quản lý an ninh",
                Status = index % 2 == 0 ? "Exported" : "Approved",
                RetentionCategory = "Incident",
                ExportReference = index % 2 == 0 ? $"demo://clips/security-event-{securityEvent.SecurityEventId:000}.mp4" : null,
                Note = "Clip được yêu cầu để xem xét tương quan và làm hồ sơ báo cáo.",
                CreatedAtUtc = securityEvent.OccurredAtUtc.AddMinutes(3),
                ApprovedAtUtc = securityEvent.OccurredAtUtc.AddMinutes(7),
                ExportedAtUtc = index % 2 == 0 ? securityEvent.OccurredAtUtc.AddMinutes(12) : null
            }));
        }

        if (!db.ChatConversations.Any() && employees.Count >= 4)
        {
            var conversation = new ChatConversation
            {
                CreatedAt = now.AddHours(-6),
                Title = "Điều phối ca trực SOC"
            };
            db.ChatConversations.Add(conversation);
            db.SaveChanges();

            var participants = employees.Take(4).ToList();
            db.ChatParticipants.AddRange(participants.Select((employee, index) => new ChatParticipant
            {
                ConversationId = conversation.ConversationId,
                EmployeeId = employee.EmployeeId,
                LastReadAt = now.AddMinutes(-(15 - index * 2))
            }));

            db.ChatMessages.AddRange(new[]
            {
                new ChatMessage
                {
                    ConversationId = conversation.ConversationId,
                    SenderId = participants[0].EmployeeId,
                    Content = "Clip bám đuôi đã được tải lên. Cần một bảo vệ xác minh chủ thẻ gần cổng xoay A.",
                    SentAt = now.AddHours(-5).AddMinutes(42),
                    IsRead = true,
                    ReadAt = now.AddHours(-5).AddMinutes(30),
                    MessageType = "Text"
                },
                new ChatMessage
                {
                    ConversationId = conversation.ConversationId,
                    SenderId = participants[1].EmployeeId,
                    Content = "Điều phối đã xác nhận. Đang tiếp cận vị trí và sẽ báo cáo lại sau 5 phút.",
                    SentAt = now.AddHours(-5).AddMinutes(35),
                    IsRead = true,
                    ReadAt = now.AddHours(-5).AddMinutes(22),
                    MessageType = "Text"
                },
                new ChatMessage
                {
                    ConversationId = conversation.ConversationId,
                    SenderId = participants[2].EmployeeId,
                    Content = "Xe có manifest không khớp vẫn đang xếp hàng tại cổng HP. Đã yêu cầu xuất hồ sơ.",
                    SentAt = now.AddHours(-2).AddMinutes(18),
                    IsRead = false,
                    MessageType = "Text"
                }
            });
        }
    }

    private static void SeedOperationalReadinessData(ApplicationDbContext db, DateTime now)
    {
        if (!db.OutboxEvents.Any())
        {
            db.OutboxEvents.AddRange(
                new OutboxEvent
                {
                    Channel = "Operations",
                    EventType = "Alarm.Created",
                    AggregateType = "Alarm",
                    AggregateId = "DEMO-ALARM-001",
                    PayloadJson = "{\"alarmType\":\"Tailgating\",\"severity\":\"High\"}",
                    Status = "Dispatched",
                    SourceSystem = "Central",
                    SchemaVersion = 1,
                    OccurredAtUtc = now.AddHours(-4),
                    RetryCount = 0,
                    NextAttemptAtUtc = null,
                    CreatedAtUtc = now.AddHours(-4),
                    DispatchedAtUtc = now.AddHours(-4).AddMinutes(1)
                },
                new OutboxEvent
                {
                    Channel = "Operations",
                    EventType = "Visit.Overstay",
                    AggregateType = "Visit",
                    AggregateId = "DEMO-VISIT-004",
                    PayloadJson = "{\"status\":\"Overstay\",\"minutes\":37}",
                    Status = "Pending",
                    SourceSystem = "Central",
                    SchemaVersion = 1,
                    OccurredAtUtc = now.AddMinutes(-35),
                    RetryCount = 1,
                    NextAttemptAtUtc = now.AddMinutes(20),
                    CreatedAtUtc = now.AddMinutes(-35)
                });
            db.SaveChanges();
        }

        if (!db.WebhookSubscriptions.Any())
        {
            db.WebhookSubscriptions.AddRange(
                new WebhookSubscription
                {
                    Name = "Cầu nối sự kiện SOC",
                    TargetUrl = "https://ops.example.internal/hooks/vshield/soc",
                    SecretReference = "secret://webhooks/soc-event-bridge",
                    EventTypes = "Alarm.Created,Incident.Updated,Visit.Overstay",
                    IsActive = true,
                    CreatedAtUtc = now.AddDays(-20)
                },
                new WebhookSubscription
                {
                    Name = "Bộ thông báo xuất tuân thủ",
                    TargetUrl = "https://governance.example.internal/hooks/vshield/export",
                    SecretReference = "secret://webhooks/compliance-export",
                    EventTypes = "EvidenceExport.Approved,Retention.ReportReady",
                    IsActive = true,
                    CreatedAtUtc = now.AddDays(-11)
                });
            db.SaveChanges();
        }

        if (!db.WebhookDeliveries.Any())
        {
            var subscription = db.WebhookSubscriptions.OrderBy(item => item.WebhookSubscriptionId).FirstOrDefault();
            var outbox = db.OutboxEvents.OrderBy(item => item.OutboxEventId).FirstOrDefault();
            if (subscription != null)
            {
                db.WebhookDeliveries.AddRange(
                    new WebhookDelivery
                    {
                        WebhookSubscriptionId = subscription.WebhookSubscriptionId,
                        OutboxEventId = outbox?.OutboxEventId,
                        Status = "Delivered",
                        AttemptCount = 1,
                        LastAttemptAtUtc = now.AddHours(-4).AddMinutes(1),
                        ResponseStatusCode = 202,
                        ResponseBody = "{\"accepted\":true}",
                        Signature = "sig-demo-delivered",
                        CreatedAtUtc = now.AddHours(-4)
                    },
                    new WebhookDelivery
                    {
                        WebhookSubscriptionId = subscription.WebhookSubscriptionId,
                        OutboxEventId = db.OutboxEvents.OrderByDescending(item => item.OutboxEventId).FirstOrDefault()?.OutboxEventId,
                        Status = "Retrying",
                        AttemptCount = 2,
                        LastAttemptAtUtc = now.AddMinutes(-5),
                        ResponseStatusCode = 503,
                        ResponseBody = "nguồn phía trên tạm thời không khả dụng",
                        Signature = "sig-demo-retry",
                        CreatedAtUtc = now.AddMinutes(-30)
                    });
            }
        }

        if (!db.RuntimeDependencyHealths.Any())
        {
            db.RuntimeDependencyHealths.AddRange(
                new RuntimeDependencyHealth
                {
                    DependencyName = "sql-primary",
                    DependencyType = "Database",
                    Status = "Healthy",
                    LatencyMs = 18,
                    Message = "Độ trễ SQL chính nằm trong mức mục tiêu.",
                    ObservedAtUtc = now.AddMinutes(-3)
                },
                new RuntimeDependencyHealth
                {
                    DependencyName = "go2rtc",
                    DependencyType = "Streaming",
                    Status = "Degraded",
                    LatencyMs = 240,
                    Message = "Quan sát thấy chậm khởi động lại lẻ tẻ khi tải lại cấu hình camera.",
                    ObservedAtUtc = now.AddMinutes(-6)
                },
                new RuntimeDependencyHealth
                {
                    DependencyName = "notification-webhook-bridge",
                    DependencyType = "Webhook",
                    Status = "Warning",
                    LatencyMs = 510,
                    Message = "Cầu nối tuân thủ phía trên trả về lỗi 503 lẻ tẻ.",
                    ObservedAtUtc = now.AddMinutes(-12)
                });
        }

        if (!db.BackupRuns.Any())
        {
            db.BackupRuns.AddRange(
                new BackupRun
                {
                    Profile = "Production-like",
                    Status = "Completed",
                    StartedAtUtc = now.AddDays(-1).AddHours(-2),
                    CompletedAtUtc = now.AddDays(-1).AddHours(-1).AddMinutes(14),
                    BackupReference = "demo://backup/sql/accesscontroldb-2026-06-29.bak",
                    SizeBytes = 3_250_000_000,
                    TargetRpoMinutes = 30,
                    TargetRtoMinutes = 120,
                    Verified = true,
                    Notes = "Sao lưu hàng đêm hoàn tất và checksum đã xác minh."
                },
                new BackupRun
                {
                    Profile = "Production-like",
                    Status = "Completed",
                    StartedAtUtc = now.AddDays(-2).AddHours(-2),
                    CompletedAtUtc = now.AddDays(-2).AddHours(-1).AddMinutes(9),
                    BackupReference = "demo://backup/sql/accesscontroldb-2026-06-28.bak",
                    SizeBytes = 3_180_000_000,
                    TargetRpoMinutes = 30,
                    TargetRtoMinutes = 120,
                    Verified = true,
                    Notes = "Chuỗi lưu trữ nhất quán với lần sao lưu trước."
                });
            db.SaveChanges();
        }

        if (!db.RestoreDrills.Any())
        {
            var backupRun = db.BackupRuns.OrderByDescending(run => run.BackupRunId).FirstOrDefault();
            db.RestoreDrills.Add(new RestoreDrill
            {
                BackupRunId = backupRun?.BackupRunId,
                Profile = "Production-like",
                Status = "Completed",
                StartedAtUtc = now.AddDays(-1).AddHours(1),
                CompletedAtUtc = now.AddDays(-1).AddHours(2).AddMinutes(8),
                MeasuredRpoMinutes = 11,
                MeasuredRtoMinutes = 68,
                Passed = true,
                Findings = "Diễn tập khôi phục đạt; còn một bước liên kết lại bí mật webhook sau khôi phục là thao tác thủ công."
            });
        }

        if (!db.SecurityOperationsChecks.Any())
        {
            db.SecurityOperationsChecks.AddRange(
                new SecurityOperationsCheck
                {
                    CheckType = "Runbook",
                    Name = "Danh sách kiểm tra bắt đầu ca SOC",
                    Status = "Passed",
                    Evidence = "demo://runbooks/soc-shift-start-ack-2026-06-30.pdf",
                    CheckedAtUtc = now.AddHours(-7)
                },
                new SecurityOperationsCheck
                {
                    CheckType = "Resilience",
                    Name = "Rà soát hàng đợi thử lại webhook",
                    Status = "NeedsAttention",
                    Evidence = "Hàng đợi gửi có 1 đăng ký đang thử lại.",
                    CheckedAtUtc = now.AddHours(-1)
                });
        }

        if (!db.QaTestRuns.Any())
        {
            db.QaTestRuns.AddRange(
                new QaTestRun
                {
                    TestType = "Regression",
                    Profile = "MediumCompany",
                    Status = "Completed",
                    PassedCount = 182,
                    FailedCount = 0,
                    EvidenceReference = "demo://qa/regression-2026-06-29.html",
                    StartedAtUtc = now.AddDays(-1).AddHours(-6),
                    CompletedAtUtc = now.AddDays(-1).AddHours(-4),
                    Notes = "Các luồng web cốt lõi, kiểm soát truy cập và quản trị bằng chứng đều đạt."
                },
                new QaTestRun
                {
                    TestType = "Smoke",
                    Profile = "WebOnlyVps",
                    Status = "Completed",
                    PassedCount = 34,
                    FailedCount = 0,
                    EvidenceReference = "demo://qa/vps-smoke-2026-06-30.html",
                    StartedAtUtc = now.AddHours(-3),
                    CompletedAtUtc = now.AddHours(-2).AddMinutes(35),
                    Notes = "Chạy kiểm tra sức khỏe compose web-only VPS hoàn tất."
                });
            db.SaveChanges();
        }

        if (!db.ReleaseCandidates.Any())
        {
            db.ReleaseCandidates.AddRange(
                new ReleaseCandidate
                {
                    Version = "2.0.0-rc3",
                    Status = "Approved",
                    MigrationId = "20260629071320_AddGeolocationSystem",
                    BuildReference = "build://vshield/2.0.0-rc3",
                    CreatedByUserId = db.AppUsers.FirstOrDefault(user => user.Role == "Admin")?.UserId,
                    ApprovedByUserId = db.AppUsers.FirstOrDefault(user => user.Role == "Admin")?.UserId,
                    CreatedAtUtc = now.AddDays(-2),
                    ApprovedAtUtc = now.AddDays(-1)
                });
            db.SaveChanges();
        }

        if (!db.ReleaseGateChecks.Any())
        {
            var candidate = db.ReleaseCandidates.OrderByDescending(item => item.ReleaseCandidateId).FirstOrDefault();
            if (candidate != null)
            {
                db.ReleaseGateChecks.AddRange(
                    new ReleaseGateCheck
                    {
                        ReleaseCandidateId = candidate.ReleaseCandidateId,
                        GateName = "Rà soát cấu hình an ninh",
                        Status = "Passed",
                        Required = true,
                        EvidenceReference = "demo://release/security-review-rc3.pdf",
                        Notes = "Đã rà soát cấu hình JWT, MFA và ký bằng chứng.",
                        VerifiedByUserId = candidate.ApprovedByUserId,
                        VerifiedAtUtc = now.AddDays(-1).AddHours(1)
                    },
                    new ReleaseGateCheck
                    {
                        ReleaseCandidateId = candidate.ReleaseCandidateId,
                        GateName = "Triển khai thử web-only VPS",
                        Status = "Passed",
                        Required = true,
                        EvidenceReference = "demo://release/vps-smoke-rc3.txt",
                        Notes = "Stack frontend, API và SQL đạt trạng thái khỏe mạnh với proxy cùng nguồn.",
                        VerifiedByUserId = candidate.ApprovedByUserId,
                        VerifiedAtUtc = now.AddHours(-2)
                    });
            }
        }

        if (!db.RunbookAcknowledgements.Any())
        {
            db.RunbookAcknowledgements.AddRange(
                new RunbookAcknowledgement
                {
                    RunbookName = "Phân loại sự cố SOC",
                    RoleName = "BaoVe",
                    AcknowledgedByUserId = db.AppUsers.FirstOrDefault(user => user.Role == "BaoVe")?.UserId,
                    EvidenceReference = "demo://runbooks/soc-triage-ack.pdf",
                    AcknowledgedAtUtc = now.AddDays(-3)
                },
                new RunbookAcknowledgement
                {
                    RunbookName = "Phê duyệt xuất bằng chứng",
                    RoleName = "Admin",
                    AcknowledgedByUserId = db.AppUsers.FirstOrDefault(user => user.Role == "Admin")?.UserId,
                    EvidenceReference = "demo://runbooks/evidence-export-ack.pdf",
                    AcknowledgedAtUtc = now.AddDays(-5)
                });
        }
    }

    private static void SeedCampus3DScene(ApplicationDbContext db, List<Site> sites, DateTime now)
    {
        if (db.Campus3DObjects.Any()) return;

        var hnSite = sites.FirstOrDefault(s => s.Code == "HN-HQ");
        var bnSite = sites.FirstOrDefault(s => s.Code == "BN-FAC");
        var hpSite = sites.FirstOrDefault(s => s.Code == "HP-LOG");

        var objects = new List<Campus3DObject>();

        // ── HN — Head Office ──
        if (hnSite != null)
        {
            objects.AddRange(new[]
            {
                // Admin Building — 3 floors
                new Campus3DObject { SiteId = hnSite.SiteId, ObjectType = "Building", Label = "HN Hành chính", PositionX = -30, PositionZ = 0, PositionY = 0, Width = 40, Length = 20, Height = 15, Floors = 3, Rotation = 0, Color = "#2563eb", PropertiesJson = "{\"zone\":\"Office Zone\",\"level\":\"Normal\"}" },
                // Ops Building — 2 floors
                new Campus3DObject { SiteId = hnSite.SiteId, ObjectType = "Building", Label = "HN Vận hành", PositionX = 20, PositionZ = 5, PositionY = 0, Width = 30, Length = 18, Height = 10, Floors = 2, Rotation = 0, Color = "#7c3aed", PropertiesJson = "{\"zone\":\"Office Zone\",\"level\":\"Normal\"}" },
                // SOC & Server Room
                new Campus3DObject { SiteId = hnSite.SiteId, ObjectType = "Building", Label = "HN SOC & Máy chủ", PositionX = 25, PositionZ = -12, PositionY = 0, Width = 15, Length = 12, Height = 4, Floors = 1, Rotation = 0, Color = "#dc2626", PropertiesJson = "{\"zone\":\"SOC and Server Room\",\"level\":\"Critical\"}" },
                // Parking B1
                new Campus3DObject { SiteId = hnSite.SiteId, ObjectType = "ParkingArea", Label = "HN Tầng hầm B1", PositionX = -25, PositionZ = -22, PositionY = -0.5m, Width = 35, Length = 15, Height = 0.5m, Rotation = 0, Color = "#64748b" },
                // Outdoor parking
                new Campus3DObject { SiteId = hnSite.SiteId, ObjectType = "ParkingArea", Label = "HN Bãi ngoài trời", PositionX = 15, PositionZ = -22, PositionY = -0.5m, Width = 30, Length = 20, Height = 0.5m, Rotation = 0, Color = "#94a3b8" },
                // Main Gate marker
                new Campus3DObject { SiteId = hnSite.SiteId, ObjectType = "GateMarker", Label = "Cổng chính HN", PositionX = -35, PositionZ = 16, PositionY = 0, Width = 6, Length = 2, Height = 3, Rotation = 0, Color = "#0f766e" },
                // Basement gate marker
                new Campus3DObject { SiteId = hnSite.SiteId, ObjectType = "GateMarker", Label = "Bãi xe hầm HN", PositionX = -15, PositionZ = -18, PositionY = -1, Width = 6, Length = 2, Height = 3, Rotation = 0, Color = "#0f766e" },
                // Walkway: Admin → Main Gate
                new Campus3DObject { SiteId = hnSite.SiteId, ObjectType = "Path", Label = "HN Lối đi Hành chính-Cổng", PositionX = -32, PositionZ = 8, PositionY = -0.3m, Width = 3, Length = 8, Height = 0.3m, Rotation = 0, Color = "#475569" },
                // Walkway: Admin → Ops
                new Campus3DObject { SiteId = hnSite.SiteId, ObjectType = "Path", Label = "HN Lối đi Hành chính-Vận hành", PositionX = -5, PositionZ = 2, PositionY = -0.3m, Width = 3, Length = 16, Height = 0.3m, Rotation = 0, Color = "#475569" },
                // Walkway: Ops → SOC
                new Campus3DObject { SiteId = hnSite.SiteId, ObjectType = "Path", Label = "HN Lối đi Vận hành-SOC", PositionX = 28, PositionZ = -3, PositionY = -0.3m, Width = 2, Length = 9, Height = 0.3m, Rotation = 0, Color = "#475569" },
                // Landscape trees
                new Campus3DObject { SiteId = hnSite.SiteId, ObjectType = "Landmark", Label = "Cây xanh HN", PositionX = -10, PositionZ = 15, PositionY = 0, Width = 3, Length = 3, Height = 5, Rotation = 0, Color = "#22c55e" },
                new Campus3DObject { SiteId = hnSite.SiteId, ObjectType = "Landmark", Label = "Cây xanh HN", PositionX = 0, PositionZ = 18, PositionY = 0, Width = 3, Length = 3, Height = 5, Rotation = 0, Color = "#16a34a" },
                new Campus3DObject { SiteId = hnSite.SiteId, ObjectType = "Landmark", Label = "Cây xanh HN", PositionX = 10, PositionZ = 17, PositionY = 0, Width = 3, Length = 3, Height = 4, Rotation = 0, Color = "#22c55e" },
            });
        }

        // ── BN — Factory Campus ──
        if (bnSite != null)
        {
            objects.AddRange(new[]
            {
                new Campus3DObject { SiteId = bnSite.SiteId, ObjectType = "Building", Label = "BN Hành chính", PositionX = 0, PositionZ = 0, PositionY = 0, Width = 35, Length = 18, Height = 10, Floors = 2, Rotation = 0, Color = "#2563eb", PropertiesJson = "{\"zone\":\"Office Zone\",\"level\":\"Normal\"}" },
                new Campus3DObject { SiteId = bnSite.SiteId, ObjectType = "Building", Label = "BN Sản xuất", PositionX = 45, PositionZ = 5, PositionY = 0, Width = 50, Length = 25, Height = 8, Floors = 2, Rotation = 0, Color = "#f59e0b", PropertiesJson = "{\"zone\":\"Production Zone\",\"level\":\"Restricted\"}" },
                new Campus3DObject { SiteId = bnSite.SiteId, ObjectType = "Building", Label = "BN Phòng QA", PositionX = 50, PositionZ = -15, PositionY = 0, Width = 20, Length = 12, Height = 5, Floors = 1, Rotation = 0, Color = "#eab308", PropertiesJson = "{\"zone\":\"Production Zone\",\"level\":\"Restricted\"}" },
                new Campus3DObject { SiteId = bnSite.SiteId, ObjectType = "ParkingArea", Label = "BN Nhà xe nhân viên", PositionX = 5, PositionZ = -22, PositionY = -0.5m, Width = 30, Length = 18, Height = 0.5m, Rotation = 0, Color = "#64748b" },
                new Campus3DObject { SiteId = bnSite.SiteId, ObjectType = "ParkingArea", Label = "BN Bãi xe tải", PositionX = 55, PositionZ = -22, PositionY = -0.5m, Width = 25, Length = 15, Height = 0.5m, Rotation = 0, Color = "#94a3b8" },
                new Campus3DObject { SiteId = bnSite.SiteId, ObjectType = "GateMarker", Label = "Cổng nhân viên BN", PositionX = 0, PositionZ = 16, PositionY = 0, Width = 6, Length = 2, Height = 3, Rotation = 0, Color = "#0f766e" },
                new Campus3DObject { SiteId = bnSite.SiteId, ObjectType = "GateMarker", Label = "Cổng xe tải BN", PositionX = 60, PositionZ = 14, PositionY = 0, Width = 8, Length = 2, Height = 4, Rotation = 0, Color = "#0f766e" },
                new Campus3DObject { SiteId = bnSite.SiteId, ObjectType = "Path", Label = "BN Lối đi Hành chính-Cổng NV", PositionX = -2, PositionZ = 8, PositionY = -0.3m, Width = 3, Length = 8, Height = 0.3m, Rotation = 0, Color = "#475569" },
                new Campus3DObject { SiteId = bnSite.SiteId, ObjectType = "Path", Label = "BN Lối đi Hành chính-Sản xuất", PositionX = 22, PositionZ = 3, PositionY = -0.3m, Width = 3, Length = 22, Height = 0.3m, Rotation = 0, Color = "#475569" },
                new Campus3DObject { SiteId = bnSite.SiteId, ObjectType = "Path", Label = "BN Lối đi Sản xuất-QA", PositionX = 52, PositionZ = -5, PositionY = -0.3m, Width = 2, Length = 10, Height = 0.3m, Rotation = 0, Color = "#475569" },
                new Campus3DObject { SiteId = bnSite.SiteId, ObjectType = "Landmark", Label = "Cây xanh BN", PositionX = -10, PositionZ = 12, PositionY = 0, Width = 4, Length = 4, Height = 6, Rotation = 0, Color = "#22c55e" },
                new Campus3DObject { SiteId = bnSite.SiteId, ObjectType = "Landmark", Label = "Cây xanh BN", PositionX = 30, PositionZ = 18, PositionY = 0, Width = 4, Length = 4, Height = 5, Rotation = 0, Color = "#16a34a" },
            });
        }

        // ── HP — Logistics Hub ──
        if (hpSite != null)
        {
            objects.AddRange(new[]
            {
                new Campus3DObject { SiteId = hpSite.SiteId, ObjectType = "Building", Label = "HP Hành chính", PositionX = 0, PositionZ = 0, PositionY = 0, Width = 25, Length = 15, Height = 6, Floors = 2, Rotation = 0, Color = "#2563eb", PropertiesJson = "{\"zone\":\"Office Zone\",\"level\":\"Normal\"}" },
                new Campus3DObject { SiteId = hpSite.SiteId, ObjectType = "Building", Label = "HP Kho bãi", PositionX = 30, PositionZ = 5, PositionY = 0, Width = 40, Length = 30, Height = 8, Floors = 1, Rotation = 0, Color = "#f59e0b", PropertiesJson = "{\"zone\":\"Logistics\",\"level\":\"Normal\"}" },
                new Campus3DObject { SiteId = hpSite.SiteId, ObjectType = "ParkingArea", Label = "HP Kho bãi logistics", PositionX = 5, PositionZ = -20, PositionY = -0.5m, Width = 30, Length = 15, Height = 0.5m, Rotation = 0, Color = "#64748b" },
                new Campus3DObject { SiteId = hpSite.SiteId, ObjectType = "GateMarker", Label = "Cổng kho HP", PositionX = -5, PositionZ = 15, PositionY = 0, Width = 6, Length = 2, Height = 3, Rotation = 0, Color = "#0f766e" },
                new Campus3DObject { SiteId = hpSite.SiteId, ObjectType = "Path", Label = "HP Lối đi Hành chính-Cổng", PositionX = -3, PositionZ = 7, PositionY = -0.3m, Width = 3, Length = 8, Height = 0.3m, Rotation = 0, Color = "#475569" },
                new Campus3DObject { SiteId = hpSite.SiteId, ObjectType = "Path", Label = "HP Lối đi Hành chính-Kho bãi", PositionX = 15, PositionZ = 3, PositionY = -0.3m, Width = 3, Length = 12, Height = 0.3m, Rotation = 0, Color = "#475569" },
                new Campus3DObject { SiteId = hpSite.SiteId, ObjectType = "Landmark", Label = "Cây xanh HP", PositionX = -8, PositionZ = 10, PositionY = 0, Width = 3, Length = 3, Height = 4, Rotation = 0, Color = "#22c55e" },
            });
        }

        db.Campus3DObjects.AddRange(objects);
    }

    private static void EnsureUebaDemoData(ApplicationDbContext db)
    {
        var employeeIds = db.AccessLogs.AsNoTracking()
            .Where(item => item.EmployeeId.HasValue)
            .Select(item => item.EmployeeId!.Value)
            .Distinct()
            .ToList();

        if (employeeIds.Count == 0)
        {
            return;
        }

        var uebaService = new UebaService(db);
        foreach (var employeeId in employeeIds)
        {
            uebaService.BuildProfileAsync(employeeId).GetAwaiter().GetResult();
        }

        SeedUebaScenarioLogs(db, uebaService);
    }

    private static void SeedUebaScenarioLogs(ApplicationDbContext db, UebaService uebaService)
    {
        const string scenarioPrefix = "UEBA_DEMO_SCENARIO";

        var candidateProfiles = db.UEBAProfiles.AsNoTracking()
            .Where(item => item.TotalAccessCount >= 10)
            .OrderByDescending(item => item.TotalAccessCount)
            .ThenByDescending(item => item.RiskScore)
            .Take(30)
            .ToList();

        var employeeIds = candidateProfiles.Select(item => item.EmployeeId).Distinct().ToList();
        var employees = db.Employees.AsNoTracking()
            .Where(item => employeeIds.Contains(item.EmployeeId))
            .OrderBy(item => item.EmployeeId)
            .ToList();

        var gates = db.Gates.AsNoTracking().OrderBy(item => item.GateId).ToList();
        var cameras = db.Cameras.AsNoTracking().ToList();
        if (employees.Count < 4 || gates.Count == 0)
        {
            return;
        }

        var employeeById = employees.ToDictionary(item => item.EmployeeId);
        var profileByEmployeeId = candidateProfiles
            .Where(item => employeeById.ContainsKey(item.EmployeeId))
            .ToDictionary(item => item.EmployeeId);

        var existingScenarioLogs = db.AccessLogs
            .Where(item => item.Note != null && item.Note.StartsWith(scenarioPrefix))
            .ToList();

        if (existingScenarioLogs.Count == 0)
        {
            var now = DateTime.UtcNow;
            var weekendAnchor = now.Date.AddDays(-(int)now.DayOfWeek);
            var createdLogs = new List<AccessLog>();
            var plateByEmployeeId = db.Vehicles.AsNoTracking()
                .Where(vehicle => vehicle.EmployeeId.HasValue && employeeById.Keys.Contains(vehicle.EmployeeId.Value))
                .GroupBy(vehicle => vehicle.EmployeeId!.Value)
                .ToDictionary(group => group.Key, group => group.Select(vehicle => vehicle.LicensePlate).FirstOrDefault());
            var siteById = db.Sites.AsNoTracking().ToDictionary(site => site.SiteId, site => site.Name);

            AccessLog CreateScenarioLog(
                Employee employee,
                Gate gate,
                DateTime timestamp,
                string direction,
                bool isBypass,
                string scenarioName)
            {
                var camera = cameras.FirstOrDefault(item => item.GateId == gate.GateId && item.CameraType == "QR")
                    ?? cameras.FirstOrDefault(item => item.GateId == gate.GateId);

                return new AccessLog
                {
                    Timestamp = timestamp,
                    Direction = direction,
                    GateId = gate.GateId,
                    CameraId = camera?.CameraId,
                    EmployeeId = employee.EmployeeId,
                    CapturedLicensePlate = plateByEmployeeId.GetValueOrDefault(employee.EmployeeId),
                    ResultStatus = "Granted",
                    IsBypass = isBypass,
                    Note = $"{scenarioPrefix}:{scenarioName}",
                    SiteNameSnapshot = employee.PrimarySiteId.HasValue ? siteById.GetValueOrDefault(employee.PrimarySiteId.Value) : null,
                    SecurityZoneNameSnapshot = "Kịch bản UEBA hạn chế",
                    AccessPointNameSnapshot = $"{gate.GateName} Điểm truy cập",
                    LaneNameSnapshot = $"{gate.GateName} Làn demo",
                    GateNameSnapshot = gate.GateName,
                    CameraNameSnapshot = camera?.CameraName
                };
            }

            var unusualTimeProfile = candidateProfiles
                .FirstOrDefault(item => item.TypicalStartHour >= 6);
            var unusualGateProfile = candidateProfiles
                .FirstOrDefault(item => gates.Any(gate => !ProfileContainsGate(item.CommonGatesJson, gate.GateId)));
            var bypassProfile = candidateProfiles
                .FirstOrDefault(item => item.BypassRate < 10);
            var rapidFrequencyProfile = candidateProfiles
                .FirstOrDefault(item => item.AvgAccessPerDay < 5);

            if (unusualTimeProfile == null || unusualGateProfile == null || bypassProfile == null || rapidFrequencyProfile == null)
            {
                return;
            }

            var unusualTimeEmployee = employeeById[unusualTimeProfile.EmployeeId];
            var unusualGateEmployee = employeeById[unusualGateProfile.EmployeeId];
            var bypassEmployee = employeeById[bypassProfile.EmployeeId];
            var rapidFrequencyEmployee = employeeById[rapidFrequencyProfile.EmployeeId];
            var unusualGate = gates.First(gate => !ProfileContainsGate(unusualGateProfile.CommonGatesJson, gate.GateId));

            createdLogs.Add(CreateScenarioLog(
                unusualTimeEmployee,
                gates[0],
                weekendAnchor.AddHours(2).AddMinutes(10),
                "IN",
                false,
                "UNUSUAL_TIME"));

            createdLogs.Add(CreateScenarioLog(
                unusualGateEmployee,
                unusualGate,
                now.Date.AddHours(11).AddMinutes(5),
                "IN",
                false,
                "UNUSUAL_GATE"));

            createdLogs.Add(CreateScenarioLog(
                bypassEmployee,
                gates[Math.Min(1, gates.Count - 1)],
                now.Date.AddHours(9).AddMinutes(10),
                "IN",
                true,
                "BYPASS"));

            for (var i = 0; i < 7; i++)
            {
                createdLogs.Add(CreateScenarioLog(
                    rapidFrequencyEmployee,
                    gates[Math.Min(2, gates.Count - 1)],
                    now.AddMinutes(-(25 - i * 3)),
                    i % 2 == 0 ? "IN" : "OUT",
                    false,
                    "RAPID_FREQUENCY"));
            }

            db.AccessLogs.AddRange(createdLogs);
            db.SaveChanges();
            existingScenarioLogs = createdLogs;
        }

        foreach (var log in existingScenarioLogs.OrderBy(item => item.Timestamp))
        {
            uebaService.AnalyzeAccessLogAsync(log).GetAwaiter().GetResult();
        }

        foreach (var employeeId in existingScenarioLogs
                     .Where(item => item.EmployeeId.HasValue)
                     .Select(item => item.EmployeeId!.Value)
                     .Distinct())
        {
            uebaService.BuildProfileAsync(employeeId).GetAwaiter().GetResult();
        }
    }

    private static bool ProfileContainsGate(string? commonGatesJson, int gateId)
    {
        if (string.IsNullOrWhiteSpace(commonGatesJson))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(commonGatesJson);
            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (element.TryGetProperty("gateId", out var gateElement) && gateElement.GetInt32() == gateId)
                {
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static void EnsureEnterpriseDemoScenarios(ApplicationDbContext db, List<Employee> employees, DateTime now)
    {
        var admin = db.AppUsers.FirstOrDefault(user => user.Username == "admin");
        var guard = db.AppUsers.FirstOrDefault(user => user.Username == "baove1");
        var sampleEmployee = employees.FirstOrDefault();

        if (!db.OperationalInterventionRequests.Any() && guard != null)
        {
            db.OperationalInterventionRequests.AddRange(
                new OperationalInterventionRequest
                {
                    RequestedByUserId = guard.UserId,
                    LaneId = "1",
                    LaneName = "Cổng A - Làn 1",
                    InterventionType = "temporary_grant",
                    SubjectName = sampleEmployee?.FullName ?? "Nguyen Van Minh",
                    SubjectId = (sampleEmployee?.EmployeeId ?? 1).ToString(),
                    SubjectType = "Employee",
                    PlateNumber = "51A-12345",
                    Reason = "Nha thau da duoc xac minh can vao khu vuc han che",
                    Priority = "high",
                    Status = "Pending",
                    CreatedAtUtc = now.AddMinutes(-12),
                    ExpiresAtUtc = now.AddHours(3)
                },
                new OperationalInterventionRequest
                {
                    RequestedByUserId = guard.UserId,
                    AcceptedByUserId = admin?.UserId,
                    LaneId = "2",
                    LaneName = "Bãi xe - Làn 2",
                    InterventionType = "policy_override",
                    SubjectName = "Tran Thi Binh",
                    SubjectId = (employees.Skip(1).FirstOrDefault()?.EmployeeId ?? 2).ToString(),
                    SubjectType = "Employee",
                    PlateNumber = "51C-33333",
                    Reason = "Lech trang thai bai xe sau lan mat ket noi",
                    Priority = "medium",
                    Status = "Accepted",
                    CreatedAtUtc = now.AddMinutes(-40),
                    AcceptedAtUtc = now.AddMinutes(-15),
                    ExpiresAtUtc = now.AddHours(1)
                },
                new OperationalInterventionRequest
                {
                    RequestedByUserId = guard.UserId,
                    LaneId = "1",
                    LaneName = "Cổng A - Làn 1",
                    InterventionType = "temporary_grant",
                    SubjectName = "Khach demo het han",
                    SubjectType = "Guest",
                    Reason = "Yeu cau cu da qua thoi gian xu ly",
                    Priority = "low",
                    Status = "Expired",
                    CreatedAtUtc = now.AddHours(-6),
                    ExpiresAtUtc = now.AddHours(-2)
                });
        }

        if (!db.SecurityDevices.Any())
        {
            db.SecurityDevices.AddRange(
                new SecurityDevice { Name = "Bộ điều khiển QR cổng A", DeviceType = "QRController", Vendor = "V-Shield", Model = "VS-Q1", SerialNumber = "DEMO-QR-001", Status = "Online", LastSeenAtUtc = now.AddSeconds(-15) },
                new SecurityDevice { Name = "Rào chắn bãi xe", DeviceType = "Barrier", Vendor = "V-Shield", Model = "VS-B2", SerialNumber = "DEMO-BAR-002", Status = "Degraded", LastSeenAtUtc = now.AddMinutes(-8) },
                new SecurityDevice { Name = "Đầu đọc khu vực hạn chế", DeviceType = "QRReader", Vendor = "V-Shield", Model = "VS-R1", SerialNumber = "DEMO-QR-003", Status = "Offline", LastSeenAtUtc = now.AddHours(-2) });
        }

        if (!db.Alarms.Any())
        {
            db.Alarms.AddRange(
                new Alarm { AlarmType = "DeviceHealth", Severity = "Low", State = "Acknowledged", Summary = "Cửa sổ bảo trì bộ điều khiển cổng A", CreatedAtUtc = now.AddHours(-3), AcknowledgedAtUtc = now.AddHours(-2) },
                new Alarm { AlarmType = "DeviceOffline", Severity = "Critical", State = "New", Summary = "Đầu đọc khu vực hạn chế đã offline hai giờ", CreatedAtUtc = now.AddMinutes(-20) });
        }

        if (!db.EvidenceItems.Any())
        {
            db.EvidenceItems.AddRange(
                new EvidenceItem { EvidenceType = "Document", SourceType = "DemoAlarm", SourceReference = "DeviceOffline-DEMO-QR-003", StorageReference = "demo://evidence/device-offline-qr-003.json", HashSha256 = new string('a', 64), PrivacyLabel = "Internal", RetentionCategory = "Incident", IsImmutable = true, LastHashVerificationStatus = "Verified", CurrentHashVerifiedAtUtc = now.AddMinutes(-18), CreatedByUserId = admin?.UserId, CreatedAtUtc = now.AddMinutes(-20) },
                new EvidenceItem { EvidenceType = "Snapshot", SourceType = "DemoAccess", SourceReference = "ManualPass-51C-33333", StorageReference = "demo://evidence/manual-pass-51c-33333.jpg", HashSha256 = new string('b', 64), PrivacyLabel = "Restricted", RetentionCategory = "Investigation", IsImmutable = true, LastHashVerificationStatus = "Verified", CurrentHashVerifiedAtUtc = now.AddMinutes(-8), CreatedByUserId = admin?.UserId, CreatedAtUtc = now.AddMinutes(-10) });
        }
    }

    private static void EnsureDemoUserAccounts(ApplicationDbContext db, List<Employee> employees, DateTime now)
    {
        var activeEmployees = employees
            .Where(employee => employee.Status == true)
            .OrderBy(employee => employee.EmployeeId)
            .ToList();

        if (activeEmployees.Count == 0)
            return;

        var assignedEmployeeIds = new HashSet<int>();

        var adminEmployee = SelectEmployeesForRole(activeEmployees, assignedEmployeeIds, IsManagerCandidate, 1).FirstOrDefault()
                            ?? SelectEmployeesForRole(activeEmployees, assignedEmployeeIds, _ => true, 1).FirstOrDefault();

        var managerEmployees = SelectEmployeesForRole(activeEmployees, assignedEmployeeIds, IsManagerCandidate, 4);
        var guardEmployees = SelectEmployeesForRole(activeEmployees, assignedEmployeeIds, IsGuardCandidate, 16);
        var receptionEmployees = SelectEmployeesForRole(activeEmployees, assignedEmployeeIds, IsReceptionCandidate, 2);
        var nhanSuEmployees = SelectEmployeesForRole(activeEmployees, assignedEmployeeIds, IsNhanSuCandidate, 2);
        var nhanVienEmployees = SelectEmployeesForRole(activeEmployees, assignedEmployeeIds, IsNhanVienCandidate, 5);

        UpsertDemoUser(
            db,
            "admin",
            "Admin",
            adminEmployee?.FullName ?? "Quan tri vien",
            "Admin@123",
            adminEmployee?.EmployeeId,
            now,
            resetPassword: false);

        for (var i = 0; i < managerEmployees.Count; i++)
        {
            var employee = managerEmployees[i];
            var username = i == 0 ? "manager" : $"quanly{i + 1}";
            UpsertDemoUser(db, username, "QuanLy", employee.FullName, "Manager@123", employee.EmployeeId, now, resetPassword: true);
        }

        for (var i = 0; i < guardEmployees.Count; i++)
        {
            var employee = guardEmployees[i];
            UpsertDemoUser(db, $"baove{i + 1}", "BaoVe", employee.FullName, "BaoVe@123", employee.EmployeeId, now, resetPassword: true);
        }

        for (var i = 0; i < receptionEmployees.Count; i++)
        {
            var employee = receptionEmployees[i];
            UpsertDemoUser(db, $"letan{i + 1}", "LeTan", employee.FullName, "LeTan@123", employee.EmployeeId, now, resetPassword: true);
        }

        for (var i = 0; i < nhanSuEmployees.Count; i++)
        {
            var employee = nhanSuEmployees[i];
            UpsertDemoUser(db, $"nhansu{i + 1}", "NhanSu", employee.FullName, "HR@123", employee.EmployeeId, now, resetPassword: true);
        }

        for (var i = 0; i < nhanVienEmployees.Count; i++)
        {
            var employee = nhanVienEmployees[i];
            UpsertDemoUser(db, $"nhanvien{i + 1}", "NhanVien", employee.FullName, "Staff@123", employee.EmployeeId, now, resetPassword: true);
        }

        BackfillDemoUserEmployeeLinks(db, activeEmployees);
    }

    private static void UpsertDemoUser(
        ApplicationDbContext db,
        string username,
        string role,
        string fullName,
        string password,
        int? employeeId,
        DateTime now,
        bool resetPassword)
    {
        var normalized = username.ToUpperInvariant();
        var user = db.AppUsers.FirstOrDefault(item => item.Username.Trim().ToUpper() == normalized);
        if (user == null)
        {
            db.AppUsers.Add(new AppUser
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FullName = fullName,
                Role = role,
                IsActive = true,
                CreatedAt = now,
                LastPasswordChangedAtUtc = now,
                EmployeeId = employeeId
            });
            return;
        }

        var roleChanged = !string.Equals(user.Role, role, StringComparison.OrdinalIgnoreCase);
        var employeeChanged = user.EmployeeId != employeeId;

        user.Role = role;
        user.FullName = fullName;
        user.IsActive = true;
        user.EmployeeId = employeeId;

        if (resetPassword && !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.LastPasswordChangedAtUtc = now;
            user.TokenVersion++;
        }

        if (roleChanged || employeeChanged)
            user.TokenVersion++;
    }

    private static List<Employee> SelectEmployeesForRole(
        List<Employee> employees,
        HashSet<int> assignedEmployeeIds,
        Func<Employee, bool> preferredPredicate,
        int take)
    {
        var selected = employees
            .Where(employee => !assignedEmployeeIds.Contains(employee.EmployeeId))
            .Where(preferredPredicate)
            .Take(take)
            .ToList();

        if (selected.Count < take)
        {
            var fallback = employees
                .Where(employee => !assignedEmployeeIds.Contains(employee.EmployeeId))
                .Where(employee => selected.All(item => item.EmployeeId != employee.EmployeeId))
                .Take(take - selected.Count)
                .ToList();

            selected.AddRange(fallback);
        }

        foreach (var employee in selected)
            assignedEmployeeIds.Add(employee.EmployeeId);

        return selected;
    }

    private static void BackfillDemoUserEmployeeLinks(ApplicationDbContext db, List<Employee> employees)
    {
        var employeeIdByName = employees
            .Where(employee => !string.IsNullOrWhiteSpace(employee.FullName))
            .GroupBy(employee => employee.FullName.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.First().EmployeeId, StringComparer.OrdinalIgnoreCase);

        var usedEmployeeIds = db.AppUsers
            .Where(user => user.EmployeeId.HasValue)
            .Select(user => user.EmployeeId!.Value)
            .ToHashSet();

        foreach (var user in db.AppUsers.Where(user => !user.EmployeeId.HasValue).ToList())
        {
            if (string.IsNullOrWhiteSpace(user.FullName))
                continue;

            if (!employeeIdByName.TryGetValue(user.FullName.Trim(), out var employeeId))
                continue;

            if (usedEmployeeIds.Contains(employeeId))
                continue;

            user.EmployeeId = employeeId;
            user.TokenVersion++;
            usedEmployeeIds.Add(employeeId);
        }
    }

    private static bool IsGuardCandidate(Employee employee)
    {
        var department = employee.Department?.Name ?? string.Empty;
        var position = employee.Position?.Name ?? string.Empty;

        return department.Contains("Bảo vệ", StringComparison.OrdinalIgnoreCase)
               || department.Contains("Security", StringComparison.OrdinalIgnoreCase)
               || position.Contains("Bảo vệ", StringComparison.OrdinalIgnoreCase)
               || position.Contains("Security", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsManagerCandidate(Employee employee)
    {
        var department = employee.Department?.Name ?? string.Empty;
        var position = employee.Position?.Name ?? string.Empty;

        return department.Contains("Executive", StringComparison.OrdinalIgnoreCase)
               || department.Contains("Human Resources", StringComparison.OrdinalIgnoreCase)
               || department.Contains("Nhân sự", StringComparison.OrdinalIgnoreCase)
               || position.Contains("Director", StringComparison.OrdinalIgnoreCase)
               || position.Contains("Manager", StringComparison.OrdinalIgnoreCase)
               || position.Contains("Supervisor", StringComparison.OrdinalIgnoreCase)
               || position.Contains("Trưởng", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsReceptionCandidate(Employee employee)
    {
        var department = employee.Department?.Name ?? string.Empty;
        var position = employee.Position?.Name ?? string.Empty;

        return department.Contains("Human Resources", StringComparison.OrdinalIgnoreCase)
               || department.Contains("Nhân sự", StringComparison.OrdinalIgnoreCase)
               || department.Contains("Executive", StringComparison.OrdinalIgnoreCase)
               || department.Contains("Kỹ thuật", StringComparison.OrdinalIgnoreCase)
               || position.Contains("Supervisor", StringComparison.OrdinalIgnoreCase)
               || position.Contains("Nhân viên", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNhanSuCandidate(Employee employee)
    {
        var department = employee.Department?.Name ?? string.Empty;
        var position = employee.Position?.Name ?? string.Empty;

        return department.Contains("Human Resources", StringComparison.OrdinalIgnoreCase)
               || department.Contains("Nhân sự", StringComparison.OrdinalIgnoreCase)
               || position.Contains("HR", StringComparison.OrdinalIgnoreCase)
               || position.Contains("Nhân sự", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNhanVienCandidate(Employee employee) => true;

    private static void EnsureEmployeeDynamicQrs(ApplicationDbContext db, List<Employee> employees, DateTime now)
    {
        var existingEmployeeIds = db.EmployeeDynamicQrs
            .Select(item => item.EmployeeId)
            .ToHashSet();

        var qrs = employees
            .Where(employee => employee.Status == true && !existingEmployeeIds.Contains(employee.EmployeeId))
            .Select(employee => new EmployeeDynamicQr
            {
                EmployeeId = employee.EmployeeId,
                SecretKey = BuildDemoQrSecret(employee.EmployeeId),
                TimeStepSeconds = 30,
                Digits = 6,
                IsActive = true,
                CreatedAt = now.AddDays(-30)
            })
            .ToList();

        if (qrs.Count > 0)
        {
            db.EmployeeDynamicQrs.AddRange(qrs);
            db.SaveChanges();
        }
    }

    private static void SeedDynamicQrScanLogs(ApplicationDbContext db, List<Employee> employees, List<AccessLog> accessLogs, DateTime now)
    {
        if (employees.Count == 0)
            return;

        var scanLogs = accessLogs
            .Where(log => log.EmployeeId.HasValue)
            .Take(300)
            .Select((log, index) =>
            {
                var employeeId = log.EmployeeId!.Value;
                return new DynamicQrScanLog
                {
                    EmployeeId = employeeId,
                    QrPayload = $"EMP:{employeeId}|T:{(log.Timestamp ?? now):yyyyMMddHHmm}|NONCE:{index:000000}",
                    IsValid = log.ResultStatus == "Granted",
                    Message = log.ResultStatus == "Granted"
                        ? "QR động được chấp nhận"
                        : "QR động bị từ chối do chính sách hoặc rà soát tái sử dụng",
                    ScannerDevice = log.CameraNameSnapshot ?? log.GateNameSnapshot ?? "Máy quét QR demo",
                    ScannedAt = log.Timestamp ?? now
                };
            })
            .ToList();

        db.DynamicQrScanLogs.AddRange(scanLogs);
    }

    private static void UpsertExceptionReason(ApplicationDbContext db, string code, string description)
    {
        var reason = db.ExceptionReasons.FirstOrDefault(item => item.ReasonCode == code);
        if (reason == null)
        {
            db.ExceptionReasons.Add(new ExceptionReason { ReasonCode = code, Description = description });
            return;
        }

        reason.Description = description;
    }

    private static string BuildDemoQrSecret(int employeeId)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        return string.Concat(Enumerable.Range(0, 32)
            .Select(index => alphabet[(employeeId * 17 + index * 11) % alphabet.Length]));
    }
}
