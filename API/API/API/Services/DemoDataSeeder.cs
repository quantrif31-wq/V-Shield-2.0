using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace API.Services;

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
            Name = "V-Shield Manufacturing Group",
            Code = DemoCompanyCode,
            IsActive = true,
            CreatedAtUtc = now.AddMonths(-18)
        };
        db.Companies.Add(company);
        db.SaveChanges();

        var sites = new[]
        {
            new Site { CompanyId = company.CompanyId, Name = "Head Office - Ha Noi", Code = "HN-HQ", Address = "Cau Giay, Ha Noi", CreatedAtUtc = now.AddMonths(-18) },
            new Site { CompanyId = company.CompanyId, Name = "Factory Campus - Bac Ninh", Code = "BN-FAC", Address = "VSIP Bac Ninh", CreatedAtUtc = now.AddMonths(-16) },
            new Site { CompanyId = company.CompanyId, Name = "Logistics Hub - Hai Phong", Code = "HP-LOG", Address = "Dinh Vu, Hai Phong", CreatedAtUtc = now.AddMonths(-12) }
        };
        db.Sites.AddRange(sites);
        db.SaveChanges();

        var buildings = new List<Building>();
        foreach (var site in sites)
        {
            buildings.Add(new Building { SiteId = site.SiteId, Name = $"{site.Code} Administration", Code = $"{site.Code}-ADM" });
            buildings.Add(new Building { SiteId = site.SiteId, Name = $"{site.Code} Operations", Code = $"{site.Code}-OPS" });
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
                    Name = $"Floor {floor}",
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
            zones.Add(new SecurityZone { SiteId = site.SiteId, Name = "Public Lobby", Code = $"{site.Code}-PUB", SecurityLevel = "Public", IsRestricted = false });
            zones.Add(new SecurityZone { SiteId = site.SiteId, Name = "Office Zone", Code = $"{site.Code}-OFF", SecurityLevel = "Normal", IsRestricted = false });
            zones.Add(new SecurityZone { SiteId = site.SiteId, Name = "Production Zone", Code = $"{site.Code}-PRD", SecurityLevel = "Restricted", IsRestricted = true });
            zones.Add(new SecurityZone { SiteId = site.SiteId, Name = "SOC and Server Room", Code = $"{site.Code}-SOC", SecurityLevel = "Critical", IsRestricted = true });
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
                Name = $"{zone.Code} Main Door",
                Type = zone.IsRestricted ? "Turnstile" : "Door",
                DirectionMode = "Bidirectional"
            });
        }
        db.AccessPoints.AddRange(accessPoints);
        db.SaveChanges();

        db.Doors.AddRange(accessPoints.Select(ap => new Door
        {
            AccessPointId = ap.AccessPointId,
            Name = $"{ap.Name} Door",
            DoorMode = ap.Type == "Turnstile" ? "DynamicQrAndPlate" : "DynamicQr"
        }));
        db.MusterPoints.AddRange(sites.Select(site => new MusterPoint
        {
            SiteId = site.SiteId,
            Name = $"{site.Code} Muster Area",
            LocationNote = "Outdoor assembly point near main gate",
            Capacity = 350
        }));
        db.SaveChanges();

        var gates = new[]
        {
            new Gate { GateName = "HN Main Gate", Location = "Head office front gate" },
            new Gate { GateName = "HN Basement Parking", Location = "Head office B1 ramp" },
            new Gate { GateName = "BN Employee Gate", Location = "Factory employee entrance" },
            new Gate { GateName = "BN Truck Gate", Location = "Factory logistics lane" },
            new Gate { GateName = "HP Warehouse Gate", Location = "Logistics hub gate" }
        };
        db.Gates.AddRange(gates);
        db.SaveChanges();

        db.Lanes.AddRange(new[]
        {
            new Lane { SiteId = sites[0].SiteId, GateId = gates[0].GateId, Name = "HN Entry Lane", Direction = "Entry" },
            new Lane { SiteId = sites[0].SiteId, GateId = gates[0].GateId, Name = "HN Exit Lane", Direction = "Exit" },
            new Lane { SiteId = sites[1].SiteId, GateId = gates[2].GateId, Name = "BN Employee Entry", Direction = "Entry" },
            new Lane { SiteId = sites[1].SiteId, GateId = gates[3].GateId, Name = "BN Truck Lane", Direction = "Bidirectional" },
            new Lane { SiteId = sites[2].SiteId, GateId = gates[4].GateId, Name = "HP Warehouse Lane", Direction = "Bidirectional" }
        });

        var cameras = new List<Camera>();
        foreach (var gate in gates)
        {
            cameras.Add(new Camera { CameraName = $"{gate.GateName} QR Scanner", GateId = gate.GateId, CameraType = "QR", StreamUrl = "rtsp://demo.local/qr", UrlView = "http://127.0.0.1:1984/stream.html?src=demo" });
            cameras.Add(new Camera { CameraName = $"{gate.GateName} Plate Camera", GateId = gate.GateId, CameraType = "Plate", StreamUrl = "rtsp://demo.local/plate", UrlView = "http://127.0.0.1:1984/stream.html?src=demo" });
        }
        db.Cameras.AddRange(cameras);
        db.SaveChanges();

        var departmentNames = new[]
        {
            "Security Operations", "Human Resources", "Production", "Quality Assurance", "Warehouse",
            "Maintenance", "Information Technology", "Finance", "Sales", "Executive Office"
        };
        var departments = departmentNames.Select(name => new Department { Name = name }).ToList();
        db.Departments.AddRange(departments);

        var positionNames = new[]
        {
            "Director", "Manager", "Supervisor", "Security Officer", "Engineer",
            "Technician", "Operator", "HR Specialist", "Accountant", "Warehouse Coordinator"
        };
        var positions = positionNames.Select(name => new Position { Name = name }).ToList();
        db.Positions.AddRange(positions);

        var vehicleTypes = new[]
        {
            new VehicleType { TypeName = "Car" },
            new VehicleType { TypeName = "Motorbike" },
            new VehicleType { TypeName = "Truck" },
            new VehicleType { TypeName = "Van" }
        };
        db.VehicleTypes.AddRange(vehicleTypes);

        db.ExceptionReasons.AddRange(new[]
        {
            new ExceptionReason { ReasonCode = "QR_EXPIRED", Description = "Dynamic QR token expired or outside allowed time window" },
            new ExceptionReason { ReasonCode = "QR_REPLAY", Description = "Dynamic QR token replay detected" },
            new ExceptionReason { ReasonCode = "PLATE_REVIEW", Description = "License plate requires manual review" },
            new ExceptionReason { ReasonCode = "TEMP_ACCESS", Description = "Temporary access approved by supervisor" },
            new ExceptionReason { ReasonCode = "TAILGATING", Description = "Tailgating or anti-passback anomaly" }
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
                new() { ShiftName = "Office 08:00-17:00", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(17, 0, 0), BreakMinutes = 60, AllowedLateMinutes = 10, AllowedEarlyLeaveMinutes = 10 },
                new() { ShiftName = "Factory Morning 06:00-14:00", StartTime = new TimeSpan(6, 0, 0), EndTime = new TimeSpan(14, 0, 0), BreakMinutes = 45, AllowedLateMinutes = 5, AllowedEarlyLeaveMinutes = 5 },
                new() { ShiftName = "Factory Afternoon 14:00-22:00", StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(22, 0, 0), BreakMinutes = 45, AllowedLateMinutes = 5, AllowedEarlyLeaveMinutes = 5 }
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
                Description = $"{type.TypeName} assigned to {employee.FullName}",
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
                    Note = "Generated demo work schedule",
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
                Note = "Generated from dynamic QR and gate access events",
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
            Reason = index % 3 == 0 ? "Medical leave" : "Family plan",
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
                    Note = failed ? "Demo dynamic QR anomaly event for SOC review" : "Demo dynamic QR access event",
                    SiteNameSnapshot = sites[(employee.PrimarySiteId ?? sites[0].SiteId) % sites.Length].Name,
                    SecurityZoneNameSnapshot = zones[Math.Abs(i + day) % zones.Count].Name,
                    AccessPointNameSnapshot = accessPoints[Math.Abs(i + day) % accessPoints.Count].Name,
                    LaneNameSnapshot = $"Lane {(i % 4) + 1}",
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
                        Reason = "Demo seed — manual test", RequestedByUserId = null,
                        RequestedAtUtc = now.AddDays(-i - 1), Result = "Success"
                    },
                    new BarrierCommandAudit
                    {
                        BarrierId = b.BarrierId, Command = i % 2 == 0 ? "Close" : "Open",
                        Reason = "Demo seed — end of test", RequestedByUserId = null,
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
        db.SaveChanges();
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
                new Campus3DObject { SiteId = hnSite.SiteId, ObjectType = "Building", Label = "HN Administration", PositionX = -30, PositionZ = 0, PositionY = 0, Width = 40, Length = 20, Height = 15, Floors = 3, Rotation = 0, Color = "#2563eb", PropertiesJson = "{\"zone\":\"Office Zone\",\"level\":\"Normal\"}" },
                // Ops Building — 2 floors
                new Campus3DObject { SiteId = hnSite.SiteId, ObjectType = "Building", Label = "HN Operations", PositionX = 20, PositionZ = 5, PositionY = 0, Width = 30, Length = 18, Height = 10, Floors = 2, Rotation = 0, Color = "#7c3aed", PropertiesJson = "{\"zone\":\"Office Zone\",\"level\":\"Normal\"}" },
                // SOC & Server Room
                new Campus3DObject { SiteId = hnSite.SiteId, ObjectType = "Building", Label = "HN SOC & Server", PositionX = 25, PositionZ = -12, PositionY = 0, Width = 15, Length = 12, Height = 4, Floors = 1, Rotation = 0, Color = "#dc2626", PropertiesJson = "{\"zone\":\"SOC and Server Room\",\"level\":\"Critical\"}" },
                // Parking B1
                new Campus3DObject { SiteId = hnSite.SiteId, ObjectType = "ParkingArea", Label = "HN Tầng hầm B1", PositionX = -25, PositionZ = -22, PositionY = -0.5m, Width = 35, Length = 15, Height = 0.5m, Rotation = 0, Color = "#64748b" },
                // Outdoor parking
                new Campus3DObject { SiteId = hnSite.SiteId, ObjectType = "ParkingArea", Label = "HN Bãi ngoài trời", PositionX = 15, PositionZ = -22, PositionY = -0.5m, Width = 30, Length = 20, Height = 0.5m, Rotation = 0, Color = "#94a3b8" },
                // Main Gate marker
                new Campus3DObject { SiteId = hnSite.SiteId, ObjectType = "GateMarker", Label = "HN Main Gate", PositionX = -35, PositionZ = 16, PositionY = 0, Width = 6, Length = 2, Height = 3, Rotation = 0, Color = "#0f766e" },
                // Basement gate marker
                new Campus3DObject { SiteId = hnSite.SiteId, ObjectType = "GateMarker", Label = "HN Basement Parking", PositionX = -15, PositionZ = -18, PositionY = -1, Width = 6, Length = 2, Height = 3, Rotation = 0, Color = "#0f766e" },
                // Walkway: Admin → Main Gate
                new Campus3DObject { SiteId = hnSite.SiteId, ObjectType = "Path", Label = "HN Walkway Admin-Gate", PositionX = -32, PositionZ = 8, PositionY = -0.3m, Width = 3, Length = 8, Height = 0.3m, Rotation = 0, Color = "#475569" },
                // Walkway: Admin → Ops
                new Campus3DObject { SiteId = hnSite.SiteId, ObjectType = "Path", Label = "HN Walkway Admin-Ops", PositionX = -5, PositionZ = 2, PositionY = -0.3m, Width = 3, Length = 16, Height = 0.3m, Rotation = 0, Color = "#475569" },
                // Walkway: Ops → SOC
                new Campus3DObject { SiteId = hnSite.SiteId, ObjectType = "Path", Label = "HN Walkway Ops-SOC", PositionX = 28, PositionZ = -3, PositionY = -0.3m, Width = 2, Length = 9, Height = 0.3m, Rotation = 0, Color = "#475569" },
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
                new Campus3DObject { SiteId = bnSite.SiteId, ObjectType = "Building", Label = "BN Administration", PositionX = 0, PositionZ = 0, PositionY = 0, Width = 35, Length = 18, Height = 10, Floors = 2, Rotation = 0, Color = "#2563eb", PropertiesJson = "{\"zone\":\"Office Zone\",\"level\":\"Normal\"}" },
                new Campus3DObject { SiteId = bnSite.SiteId, ObjectType = "Building", Label = "BN Production", PositionX = 45, PositionZ = 5, PositionY = 0, Width = 50, Length = 25, Height = 8, Floors = 2, Rotation = 0, Color = "#f59e0b", PropertiesJson = "{\"zone\":\"Production Zone\",\"level\":\"Restricted\"}" },
                new Campus3DObject { SiteId = bnSite.SiteId, ObjectType = "Building", Label = "BN QA Lab", PositionX = 50, PositionZ = -15, PositionY = 0, Width = 20, Length = 12, Height = 5, Floors = 1, Rotation = 0, Color = "#eab308", PropertiesJson = "{\"zone\":\"Production Zone\",\"level\":\"Restricted\"}" },
                new Campus3DObject { SiteId = bnSite.SiteId, ObjectType = "ParkingArea", Label = "BN Nhà xe nhân viên", PositionX = 5, PositionZ = -22, PositionY = -0.5m, Width = 30, Length = 18, Height = 0.5m, Rotation = 0, Color = "#64748b" },
                new Campus3DObject { SiteId = bnSite.SiteId, ObjectType = "ParkingArea", Label = "BN Bãi xe tải", PositionX = 55, PositionZ = -22, PositionY = -0.5m, Width = 25, Length = 15, Height = 0.5m, Rotation = 0, Color = "#94a3b8" },
                new Campus3DObject { SiteId = bnSite.SiteId, ObjectType = "GateMarker", Label = "BN Employee Gate", PositionX = 0, PositionZ = 16, PositionY = 0, Width = 6, Length = 2, Height = 3, Rotation = 0, Color = "#0f766e" },
                new Campus3DObject { SiteId = bnSite.SiteId, ObjectType = "GateMarker", Label = "BN Truck Gate", PositionX = 60, PositionZ = 14, PositionY = 0, Width = 8, Length = 2, Height = 4, Rotation = 0, Color = "#0f766e" },
                new Campus3DObject { SiteId = bnSite.SiteId, ObjectType = "Path", Label = "BN Walkway Admin-Employee Gate", PositionX = -2, PositionZ = 8, PositionY = -0.3m, Width = 3, Length = 8, Height = 0.3m, Rotation = 0, Color = "#475569" },
                new Campus3DObject { SiteId = bnSite.SiteId, ObjectType = "Path", Label = "BN Walkway Admin-Production", PositionX = 22, PositionZ = 3, PositionY = -0.3m, Width = 3, Length = 22, Height = 0.3m, Rotation = 0, Color = "#475569" },
                new Campus3DObject { SiteId = bnSite.SiteId, ObjectType = "Path", Label = "BN Walkway Production-QA", PositionX = 52, PositionZ = -5, PositionY = -0.3m, Width = 2, Length = 10, Height = 0.3m, Rotation = 0, Color = "#475569" },
                new Campus3DObject { SiteId = bnSite.SiteId, ObjectType = "Landmark", Label = "Cây xanh BN", PositionX = -10, PositionZ = 12, PositionY = 0, Width = 4, Length = 4, Height = 6, Rotation = 0, Color = "#22c55e" },
                new Campus3DObject { SiteId = bnSite.SiteId, ObjectType = "Landmark", Label = "Cây xanh BN", PositionX = 30, PositionZ = 18, PositionY = 0, Width = 4, Length = 4, Height = 5, Rotation = 0, Color = "#16a34a" },
            });
        }

        // ── HP — Logistics Hub ──
        if (hpSite != null)
        {
            objects.AddRange(new[]
            {
                new Campus3DObject { SiteId = hpSite.SiteId, ObjectType = "Building", Label = "HP Administration", PositionX = 0, PositionZ = 0, PositionY = 0, Width = 25, Length = 15, Height = 6, Floors = 2, Rotation = 0, Color = "#2563eb", PropertiesJson = "{\"zone\":\"Office Zone\",\"level\":\"Normal\"}" },
                new Campus3DObject { SiteId = hpSite.SiteId, ObjectType = "Building", Label = "HP Warehouse", PositionX = 30, PositionZ = 5, PositionY = 0, Width = 40, Length = 30, Height = 8, Floors = 1, Rotation = 0, Color = "#f59e0b", PropertiesJson = "{\"zone\":\"Logistics\",\"level\":\"Normal\"}" },
                new Campus3DObject { SiteId = hpSite.SiteId, ObjectType = "ParkingArea", Label = "HP Kho bãi logistics", PositionX = 5, PositionZ = -20, PositionY = -0.5m, Width = 30, Length = 15, Height = 0.5m, Rotation = 0, Color = "#64748b" },
                new Campus3DObject { SiteId = hpSite.SiteId, ObjectType = "GateMarker", Label = "HP Warehouse Gate", PositionX = -5, PositionZ = 15, PositionY = 0, Width = 6, Length = 2, Height = 3, Rotation = 0, Color = "#0f766e" },
                new Campus3DObject { SiteId = hpSite.SiteId, ObjectType = "Path", Label = "HP Walkway Admin-Gate", PositionX = -3, PositionZ = 7, PositionY = -0.3m, Width = 3, Length = 8, Height = 0.3m, Rotation = 0, Color = "#475569" },
                new Campus3DObject { SiteId = hpSite.SiteId, ObjectType = "Path", Label = "HP Walkway Admin-Warehouse", PositionX = 15, PositionZ = 3, PositionY = -0.3m, Width = 3, Length = 12, Height = 0.3m, Rotation = 0, Color = "#475569" },
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
                    SecurityZoneNameSnapshot = "Restricted UEBA Scenario",
                    AccessPointNameSnapshot = $"{gate.GateName} Access Point",
                    LaneNameSnapshot = $"{gate.GateName} Demo Lane",
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
                    LaneName = "Cong A - Lane 1",
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
                    LaneName = "Bai xe - Lane 2",
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
                    LaneName = "Cong A - Lane 1",
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
                new SecurityDevice { Name = "Gate A QR Controller", DeviceType = "QRController", Vendor = "V-Shield", Model = "VS-Q1", SerialNumber = "DEMO-QR-001", Status = "Online", LastSeenAtUtc = now.AddSeconds(-15) },
                new SecurityDevice { Name = "Parking Barrier", DeviceType = "Barrier", Vendor = "V-Shield", Model = "VS-B2", SerialNumber = "DEMO-BAR-002", Status = "Degraded", LastSeenAtUtc = now.AddMinutes(-8) },
                new SecurityDevice { Name = "Restricted Zone Reader", DeviceType = "QRReader", Vendor = "V-Shield", Model = "VS-R1", SerialNumber = "DEMO-QR-003", Status = "Offline", LastSeenAtUtc = now.AddHours(-2) });
        }

        if (!db.Alarms.Any())
        {
            db.Alarms.AddRange(
                new Alarm { AlarmType = "DeviceHealth", Severity = "Low", State = "Acknowledged", Summary = "Gate A controller maintenance window", CreatedAtUtc = now.AddHours(-3), AcknowledgedAtUtc = now.AddHours(-2) },
                new Alarm { AlarmType = "DeviceOffline", Severity = "Critical", State = "New", Summary = "Restricted Zone Reader has been offline for two hours", CreatedAtUtc = now.AddMinutes(-20) });
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
                        ? "Dynamic QR accepted"
                        : "Dynamic QR rejected for policy or replay review",
                    ScannerDevice = log.CameraNameSnapshot ?? log.GateNameSnapshot ?? "Demo QR Scanner",
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
