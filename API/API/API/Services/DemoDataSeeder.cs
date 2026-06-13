using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

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
            Console.WriteLine("[INFO] Demo data already exists.");
            return;
        }

        Seed(db);
        Console.WriteLine("[OK] Demo data seeded for medium/large company scenario.");
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
            DoorMode = ap.Type == "Turnstile" ? "CardAndFace" : "Normal"
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
            cameras.Add(new Camera { CameraName = $"{gate.GateName} Face Camera", GateId = gate.GateId, CameraType = "Face", StreamUrl = "rtsp://demo.local/face", UrlView = "http://127.0.0.1:1984/stream.html?src=demo" });
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
            new ExceptionReason { ReasonCode = "NO_FACE_MATCH", Description = "Face recognition confidence below threshold" },
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
                Note = "Generated from demo access events",
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
                var camera = cameras.FirstOrDefault(c => c.GateId == gate.GateId);
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
                    CapturedFaceImageUrl = $"/uploads/demo/faces/emp-{employee.EmployeeId:000}.jpg",
                    EmployeeId = employee.EmployeeId,
                    ResultStatus = failed ? "Denied" : "Granted",
                    IsBypass = i % 89 == 0,
                    ExceptionReasonId = failed ? exceptionReasons[Math.Abs(i + day) % exceptionReasons.Count].ReasonId : null,
                    Note = failed ? "Demo anomaly event for SOC review" : "Demo access event",
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
}
