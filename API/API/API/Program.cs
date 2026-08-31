using System.Security.Claims;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using API.Data;
using API.Hubs;
using API.Middleware;
using API.Models;
using API.Services;
using API.Services.AI;
using API.Services.Abstractions;
using API.Services.FaceRecognition;
using API.Services.AccessPolicyComparison;
using API.Services.AccessCredentials;
using API.Services.FaceCredentialBindings;
using API.Services.Sync;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Threading;
using System.Threading.RateLimiting;

namespace API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            ConfigureDataProtection(builder);

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var jwtSecretOverride = Environment.GetEnvironmentVariable("VSHIELD_JWT_SECRET");
            var secretKey = (jwtSecretOverride ?? jwtSettings["Secret"] ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
            {
                throw new InvalidOperationException("JWT secret must be configured and at least 32 characters long.");
            }
            if (builder.Environment.IsProduction() && string.IsNullOrWhiteSpace(jwtSecretOverride))
            {
                throw new InvalidOperationException("Production requires VSHIELD_JWT_SECRET. Do not use repo-backed JWT settings in production.");
            }

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    NameClaimType = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.UniqueName,
                    RoleClaimType = ClaimTypes.Role
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken) &&
                            context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var userIdClaim = context.Principal?.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
                        var tokenVersionClaim = context.Principal?.FindFirst("token_version")?.Value;
                        if (!int.TryParse(userIdClaim, out var userId) ||
                            !int.TryParse(tokenVersionClaim, out var tokenVersion))
                        {
                            context.Fail("Invalid access token session.");
                            return;
                        }

                        var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthenticationService>();
                        if (!await authService.ValidateAccessTokenVersionAsync(userId, tokenVersion))
                        {
                            context.Fail("Access token session has been revoked.");
                        }
                    }
                };
            });

            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("RuntimeOperator", policy => policy.RequireRole("Admin", "BaoVe"));
                options.AddPolicy("SecurityOperator", policy => policy.RequireRole("Admin", "BaoVe", "LeTan"));
            });

            builder.Services.AddMemoryCache();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            builder.Services.AddScoped<IStepUpService, StepUpService>();
            builder.Services.AddScoped<TotpService>();
            builder.Services.AddScoped<IVehicleManagementService, VehicleManagementService>();
            builder.Services.AddScoped<ILocalNetworkCameraDiscoveryService, LocalNetworkCameraDiscoveryService>();
            builder.Services.AddScoped<StaticVisitorQrService>();
            builder.Services.AddScoped<IAttendanceCalculationService, AttendanceCalculationService>();
            builder.Services.AddScoped<IAttendancePermissionService, AttendancePermissionService>();
            builder.Services.AddScoped<IZoneTransitService, ZoneTransitService>();
            builder.Services.AddScoped<IAttendanceZoneService, AttendanceZoneService>();
            builder.Services.AddScoped<IAttendanceAnomalyService, AttendanceAnomalyService>();
            builder.Services.AddScoped<IUebaService, UebaService>();
            builder.Services.AddScoped<IPlateFuzzyService, PlateFuzzyService>();
            builder.Services.AddScoped<IDashboardIntelligenceService, DashboardIntelligenceService>();
            builder.Services.AddScoped<ISocIntelligenceService, SocIntelligenceService>();
            builder.Services.AddScoped<ISocIncidentCopilotService, SocIncidentCopilotService>();
            builder.Services.AddScoped<IUebaRiskGraphService, UebaRiskGraphService>();
            builder.Services.AddScoped<IEvidenceAiAssistantService, EvidenceAiAssistantService>();
            builder.Services.AddScoped<IDeviceHealthIntelligenceService, DeviceHealthIntelligenceService>();
            builder.Services.AddScoped<IDeviceSimulator, DeviceSimulatorService>();
            builder.Services.AddScoped<IVisitorVehicleRiskScreeningService, VisitorVehicleRiskScreeningService>();
            builder.Services.AddScoped<IAiRecommendationService, AiRecommendationService>();
            builder.Services.AddScoped<IPolicySimulationService, PolicySimulationService>();
            builder.Services.AddScoped<INaturalLanguageQueryService, NaturalLanguageQueryService>();
            builder.Services.AddScoped<IAiGateway, AiGateway>();
            builder.Services.AddSingleton<IAiRedactionService, AiRedactionService>();
            builder.Services.AddSingleton<IAiPromptTemplateService, AiPromptTemplateService>();
            builder.Services.Configure<API.Services.AI.AiProviderOptions>(builder.Configuration.GetSection("AiProvider"));
            builder.Services.AddScoped<ICompanyHierarchyBackfillService, CompanyHierarchyBackfillService>();
            builder.Services.AddSingleton<ISecurityConfigurationHealthService, SecurityConfigurationHealthService>();
            builder.Services.AddSingleton<ISecretService, EnvironmentSecretService>();
            builder.Services.AddSingleton<IDistributedRateCounter>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var backend = config.GetValue<string>("RateLimiting:Backend") ?? "Memory";
                if (string.Equals(backend, "SqlServer", StringComparison.OrdinalIgnoreCase))
                {
                    var logger = sp.GetRequiredService<ILogger<SqlServerRateCounter>>();
                    return new SqlServerRateCounter(config, logger);
                }
                return new MemoryRateCounter();
            });
            builder.Services.AddScoped<ICampusMapRealtimeService, CampusMapRealtimeService>();
            builder.Services.AddScoped<EvidenceCaptureService>();
            builder.Services.AddScoped<LostFoundMatchingService>();
            builder.Services.AddScoped<LockerService>();
            builder.Services.AddScoped<ZoneAuthorityService>();
            builder.Services.AddScoped<UserOperationalScopeService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.Configure<FaceStorageOptions>(
                builder.Configuration.GetSection(FaceStorageOptions.SectionName));
            builder.Services.AddSingleton<IFaceStoragePathResolver, FaceStoragePathResolver>();
            builder.Services.Configure<SyncRuntimeOptions>(builder.Configuration.GetSection(SyncRuntimeOptions.SectionName));
            builder.Services.AddSingleton<ISyncExecutionContext, SyncExecutionContext>();
            builder.Services.AddSingleton<ISyncSignalNotifier, SyncSignalNotifier>();
            builder.Services.AddScoped<SyncEntityEventFactory>();
            builder.Services.AddScoped<SyncSystemConfigStore>();
            builder.Services.AddScoped<SyncEventApplier>();
            builder.Services.AddScoped<CentralSyncService>();
            builder.Services.AddScoped<RemoteFaceEnrollmentQueueService>();
            builder.Services.AddSingleton<SyncRealtimeNotifier>();
            builder.Services.AddSingleton<API.Services.ChatRelay.ChatPresenceRegistry>();
            builder.Services.AddSingleton<API.Services.ChatRelay.ChatRelayNodeRegistry>();
            builder.Services.AddSingleton<API.Services.ChatRelay.AreaNodeChatRelayWorker>();
            builder.Services.AddSingleton<API.Services.ChatRelay.ChatRelayGateway>();
            if (!builder.Environment.IsEnvironment("Testing"))
            {
                builder.Services.AddHostedService(serviceProvider =>
                    serviceProvider.GetRequiredService<API.Services.ChatRelay.AreaNodeChatRelayWorker>());
            }
            builder.Services.AddScoped<IRoutingService, RoutingService>();
            builder.Services.AddTransient<API.Services.ImportExport.IFileParser, API.Services.ImportExport.CsvFileParser>();
            builder.Services.AddTransient<API.Services.ImportExport.IFileParser, API.Services.ImportExport.ExcelFileParser>();
            builder.Services.AddTransient<API.Services.ImportExport.IFileParser, API.Services.ImportExport.JsonFileParser>();
            builder.Services.AddTransient<API.Services.ImportExport.IFileParser, API.Services.ImportExport.XmlFileParser>();
            builder.Services.AddSingleton<API.Services.ImportExport.FileParserFactory>();

            // AI Import/Export services
            builder.Services.AddSingleton<API.Services.ImportExport.AI.IFileAnalyzer, API.Services.ImportExport.AI.FileAnalyzer>();
            builder.Services.AddSingleton<API.Services.ImportExport.Validation.SynonymRegistry>();
            builder.Services.AddSingleton<API.Services.ImportExport.Validation.SynonymDetector>();
            builder.Services.AddSingleton<API.Services.ImportExport.Validation.IStructureValidator, API.Services.ImportExport.Validation.StructureValidator>();
            builder.Services.AddSingleton<API.Services.ImportExport.AI.IOcrService, API.Services.ImportExport.AI.OcrService>();
            builder.Services.AddSingleton<API.Services.ImportExport.AI.IAiNormalizationService, API.Services.ImportExport.AI.AiNormalizationService>();
            builder.Services.AddScoped<API.Services.ImportExport.AI.IAiImportService, API.Services.ImportExport.AI.AiImportService>();
            builder.Services.AddScoped<API.Services.ImportExport.IEntityImportHandler, API.Services.ImportExport.EmployeeImportHandler>();
            builder.Services.AddScoped<API.Services.ImportExport.IEntityImportHandler, API.Services.ImportExport.VehicleImportHandler>();
            builder.Services.AddScoped<API.Services.ImportExport.IEntityImportHandler, API.Services.ImportExport.DepartmentImportHandler>();
            builder.Services.AddScoped<API.Services.ImportExport.IEntityImportHandler, API.Services.ImportExport.PositionImportHandler>();
            builder.Services.AddScoped<API.Services.ImportExport.IEntityImportHandler, API.Services.ImportExport.UserImportHandler>();
            builder.Services.AddScoped<API.Services.ImportExport.IEntityImportHandler, API.Services.ImportExport.GateImportHandler>();
            builder.Services.AddScoped<API.Services.ImportExport.IEntityImportHandler, API.Services.ImportExport.CameraImportHandler>();
            builder.Services.AddScoped<API.Services.ImportExport.IEntityImportHandler, API.Services.ImportExport.VehicleTypeImportHandler>();
            builder.Services.AddScoped<API.Services.ImportExport.IEntityImportHandler, API.Services.ImportExport.ExceptionReasonImportHandler>();
            builder.Services.AddScoped<API.Services.ImportExport.IEntityImportHandler, API.Services.ImportExport.ShiftImportHandler>();
            builder.Services.AddScoped<API.Services.ImportExport.IEntityImportHandler, API.Services.ImportExport.WorkScheduleImportHandler>();
            builder.Services.AddScoped<API.Services.ImportExport.IEntityImportHandler, API.Services.ImportExport.GuestProfileImportHandler>();
            builder.Services.AddScoped<API.Services.ImportExport.IEntityImportHandler, API.Services.ImportExport.PreRegistrationImportHandler>();
            builder.Services.AddScoped<API.Services.ImportExport.IEntityImportHandler, API.Services.ImportExport.AccessLogImportHandler>();
            builder.Services.AddScoped<API.Services.ImportExport.IEntityImportHandler, API.Services.ImportExport.CompanyImportHandler>();
            builder.Services.AddScoped<API.Services.ImportExport.IEntityImportHandler, API.Services.ImportExport.SiteImportHandler>();
            builder.Services.AddScoped<API.Services.ImportExport.IEntityImportHandler, API.Services.ImportExport.BuildingImportHandler>();
            builder.Services.AddScoped<API.Services.ImportExport.IEntityImportHandler, API.Services.ImportExport.FacilityFloorImportHandler>();
            builder.Services.AddScoped<API.Services.ImportExport.IEntityImportHandler, API.Services.ImportExport.SecurityZoneImportHandler>();
            builder.Services.AddScoped<API.Services.ImportExport.IImportExportService, API.Services.ImportExport.ImportExportService>();
            builder.Services.AddSingleton<API.Services.IPublicPortalService, API.Services.PublicPortalService>();
            builder.Services.AddSingleton<RuntimeOrchestrator>();
            if (!builder.Environment.IsEnvironment("Testing"))
            {
                builder.Services.AddHostedService<RuntimeAutoStartHostedService>();
                builder.Services.AddHostedService<EnterpriseOperationsWorker>();
                builder.Services.AddHostedService<CameraRecordingService>();
                builder.Services.AddHostedService<AreaNodeSyncWorker>();
                builder.Services.AddHostedService<CentralSyncInboxWorker>();
                builder.Services.AddHostedService<RemoteFaceEnrollmentWorker>();
            }
            builder.Services.AddHttpClient();
            var faceRecognitionClientOptions =
                FaceRecognitionClientOptions.FromConfiguration(builder.Configuration);
            builder.Services.AddSingleton(faceRecognitionClientOptions);
            builder.Services.AddHttpClient<IFaceRecognitionClient, FaceRecognitionClient>(client =>
            {
                client.BaseAddress = faceRecognitionClientOptions.BaseAddress;
                client.Timeout = faceRecognitionClientOptions.Timeout;
            });
            var faceReconcileOptions = new FaceCameraReconcileOptions();
            builder.Configuration.GetSection(FaceCameraReconcileOptions.SectionName)
                .Bind(faceReconcileOptions);
            if (faceReconcileOptions.ReconcileIntervalSeconds <= 0)
            {
                throw new InvalidOperationException(
                    "FaceRecognition:ReconcileIntervalSeconds must be greater than zero.");
            }
            builder.Services.AddSingleton(faceReconcileOptions);
            builder.Services.AddScoped<FaceCameraConfigurationService>();
            builder.Services.AddScoped<IFaceCameraConfigurationService>(serviceProvider =>
                serviceProvider.GetRequiredService<FaceCameraConfigurationService>());
            builder.Services.AddScoped<IFaceCameraConfigurationStore>(serviceProvider =>
                serviceProvider.GetRequiredService<FaceCameraConfigurationService>());
            builder.Services.AddScoped<FaceCameraReconciliationCycle>();
            builder.Services.AddScoped<IFaceModelMetadataService, FaceModelMetadataService>();
            var faceEnrollmentOptions = new FaceEnrollmentOptions();
            builder.Configuration.GetSection(FaceEnrollmentOptions.SectionName).Bind(faceEnrollmentOptions);
            if (faceEnrollmentOptions.PollIntervalSeconds <= 0 ||
                faceEnrollmentOptions.MaxConcurrentJobs <= 0 ||
                faceEnrollmentOptions.MaxAttempts <= 0)
                throw new InvalidOperationException("FaceEnrollment worker settings must be greater than zero.");
            builder.Services.AddSingleton(faceEnrollmentOptions);
            builder.Services.AddScoped<IFaceEnrollmentService, FaceEnrollmentService>();
            var faceRecognitionEventOptions = new FaceRecognitionEventOptions();
            builder.Configuration.GetSection(FaceRecognitionEventOptions.SectionName)
                .Bind(faceRecognitionEventOptions);
            if (faceRecognitionEventOptions.PollIntervalMilliseconds < 250 ||
                faceRecognitionEventOptions.BatchSize is < 1 or > 200 ||
                faceRecognitionEventOptions.MaxParallelCameras <= 0 ||
                faceRecognitionEventOptions.RetentionDays <= 0)
            {
                throw new InvalidOperationException(
                    "FaceRecognitionEvents settings are invalid.");
            }
            builder.Services.AddSingleton(faceRecognitionEventOptions);
            builder.Services.AddSingleton<FaceRecognitionEventCollector>();
            builder.Services.AddSingleton<IFaceRecognitionEventCollector>(serviceProvider =>
                serviceProvider.GetRequiredService<FaceRecognitionEventCollector>());
            var comparisonOptions = new FaceAccessPolicyComparisonOptions();
            builder.Configuration.GetSection(FaceAccessPolicyComparisonOptions.SectionName)
                .Bind(comparisonOptions);
            if (comparisonOptions.PollIntervalMilliseconds < 250 ||
                comparisonOptions.BatchSize is < 1 or > 500 ||
                comparisonOptions.MaxParallelism <= 0 ||
                comparisonOptions.EvaluationVersion <= 0)
                throw new InvalidOperationException("FaceAccessPolicyComparison settings are invalid.");
            try { comparisonOptions.TimeZone = TimeZoneInfo.FindSystemTimeZoneById(comparisonOptions.TimeZoneId); }
            catch (TimeZoneNotFoundException ex) {
                throw new InvalidOperationException("FaceAccessPolicyComparison:TimeZoneId is invalid.", ex);
            }
            builder.Services.AddSingleton(comparisonOptions);
            builder.Services.AddScoped<ILegacyGateAccessEvaluator, LegacyGateAccessEvaluator>();
            builder.Services.AddScoped<IEnterpriseAccessPolicyEvaluator, EnterpriseAccessPolicyEvaluator>();
            var accessCredentialOptions = new AccessCredentialOptions();
            builder.Configuration.GetSection(AccessCredentialOptions.SectionName)
                .Bind(accessCredentialOptions);
            builder.Services.AddSingleton(accessCredentialOptions);
            builder.Services.AddSingleton<IAccessCredentialStateEvaluator, AccessCredentialStateEvaluator>();
            builder.Services.AddSingleton<IAccessCredentialIdentifierProtector,
                AccessCredentialIdentifierProtector>();
            builder.Services.AddScoped<AccessCredentialService>();
            builder.Services.AddScoped<IAccessCredentialService>(sp =>
                sp.GetRequiredService<AccessCredentialService>());
            builder.Services.AddScoped<IAccessCredentialContextResolver>(sp =>
                sp.GetRequiredService<AccessCredentialService>());
            builder.Services.AddScoped<IFaceCredentialBindingService, FaceCredentialBindingService>();
            builder.Services.AddScoped<FaceCredentialBindingManifestService>();
            builder.Services.AddSingleton<FaceAccessPolicyComparisonProcessor>();
            builder.Services.AddSingleton<IFaceAccessPolicyComparisonProcessor>(sp =>
                sp.GetRequiredService<FaceAccessPolicyComparisonProcessor>());
            builder.Services.AddSingleton<FaceAccessDecisionProcessor>();
            builder.Services.AddSingleton<IFaceAccessDecisionProcessor>(sp =>
                sp.GetRequiredService<FaceAccessDecisionProcessor>());
            builder.Services.AddSingleton<FaceCameraSessionReconciler>();
            builder.Services.AddSingleton<IFaceCameraSessionReconciler>(serviceProvider =>
                serviceProvider.GetRequiredService<FaceCameraSessionReconciler>());
            if (!builder.Environment.IsEnvironment("Testing"))
            {
                builder.Services.AddHostedService(serviceProvider =>
                    serviceProvider.GetRequiredService<FaceCameraSessionReconciler>());
                if (faceEnrollmentOptions.WorkerEnabled)
                    builder.Services.AddHostedService<FaceEnrollmentWorker>();
                if (faceRecognitionEventOptions.CollectorEnabled)
                    builder.Services.AddHostedService(serviceProvider =>
                        serviceProvider.GetRequiredService<FaceRecognitionEventCollector>());
                if (comparisonOptions.ProcessorEnabled)
                {
                    builder.Services.AddHostedService(serviceProvider =>
                        serviceProvider.GetRequiredService<FaceAccessPolicyComparisonProcessor>());
                    builder.Services.AddHostedService(serviceProvider =>
                        serviceProvider.GetRequiredService<FaceAccessDecisionProcessor>());
                }
            }
            builder.Services.AddHttpClient("AiGateway", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(35);
                client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
            });
            builder.Services.AddHttpClient("AiChat", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(180);
            });
            builder.Services.Configure<API.Services.Agent.MailOptions>(
                builder.Configuration.GetSection(API.Services.Agent.MailOptions.SectionName));
            builder.Services.AddSingleton<API.Services.Agent.AgentLlmClient>();
            builder.Services.AddSingleton<API.Services.Agent.IMailService, API.Services.Agent.MailService>();
            builder.Services.AddScoped<API.Services.Agent.MemoryService>();
            builder.Services.AddScoped<API.Services.Agent.AgentAuditService>();
            builder.Services.AddScoped<API.Services.Agent.AgentTools>();
            builder.Services.AddScoped<API.Services.Agent.AgentRunner>();
            builder.Services.AddScoped<API.Services.Agent.CompanyEmailService>();
            builder.Services.AddHostedService<API.Services.Agent.MailDeliveryTracker>();
            builder.Services.AddSignalR();
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
                });
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto |
                    ForwardedHeaders.XForwardedHost;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.CustomSchemaIds(type => type.FullName);
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "V-Shield API",
                    Version = "v1",
                    Description = "API quan ly he thong V-Shield voi phan quyen Admin/QuanLy/BaoVe/LeTan"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Nhap JWT token. Vi du: Bearer {token}"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            var allowedOrigins = ResolveAllowedOrigins(builder.Configuration);
            ValidateProductionSecurityConfiguration(builder.Configuration, builder.Environment, allowedOrigins);

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowVue", policy =>
                {
                    policy
                        .WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                var configuredAuthPermit = builder.Configuration["RateLimits:AuthPermitLimit"];
                var authPermitLimit = int.TryParse(configuredAuthPermit, out var ap) && ap > 0
                    ? ap
                    : ((builder.Environment.IsEnvironment("Testing") || builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Docker")) ? 300 : 15);
                options.AddFixedWindowLimiter("auth", limiter =>
                {
                    limiter.Window = TimeSpan.FromMinutes(1);
                    limiter.PermitLimit = authPermitLimit;
                    limiter.QueueLimit = 0;
                    limiter.AutoReplenishment = true;
                });
                options.AddFixedWindowLimiter("public", limiter =>
                {
                    limiter.Window = TimeSpan.FromMinutes(1);
                    limiter.PermitLimit = 30;
                    limiter.QueueLimit = 0;
                    limiter.AutoReplenishment = true;
                });
                options.AddFixedWindowLimiter("ops", limiter =>
                {
                    limiter.Window = TimeSpan.FromMinutes(1);
                    limiter.PermitLimit = 60;
                    limiter.QueueLimit = 0;
                    limiter.AutoReplenishment = true;
                });
                // Riêng luồng gửi xe QR: generate + verify + scan = 3 request/lượt.
                // Trần cao (600/phút) để không nghẽn cổng lúc cao điểm; có thể chỉnh qua RateLimits__QrOpsPermitLimit.
                var qrOpsPermitLimit = builder.Configuration["RateLimits:QrOpsPermitLimit"];
                var qrPermit = int.TryParse(qrOpsPermitLimit, out var qp) && qp > 0 ? qp : 600;
                options.AddFixedWindowLimiter("qr-ops", limiter =>
                {
                    limiter.Window = TimeSpan.FromMinutes(1);
                    limiter.PermitLimit = qrPermit;
                    limiter.QueueLimit = 0;
                    limiter.AutoReplenishment = true;
                });
            });
            var app = builder.Build();
            if (args.Length > 0 &&
                string.Equals(args[0], "access-credentials", StringComparison.OrdinalIgnoreCase))
            {
                Environment.ExitCode = await RunAccessCredentialCommandAsync(
                    app.Services, args, CancellationToken.None);
                return;
            }
            if (args.Length > 0 &&
                string.Equals(args[0], "face-models", StringComparison.OrdinalIgnoreCase))
            {
                Environment.ExitCode = await RunFaceModelCommandAsync(
                    app.Services,
                    args,
                    CancellationToken.None);
                return;
            }
            if (args.Length > 0 &&
                string.Equals(args[0], "face-credentials", StringComparison.OrdinalIgnoreCase))
            {
                Environment.ExitCode = await RunFaceCredentialCommandAsync(
                    app.Services,
                    args,
                    CancellationToken.None);
                return;
            }
            if (!app.Environment.IsEnvironment("Testing"))
            {
                EnsureSeedAdminUser(app.Services, builder.Configuration, app.Environment);
                DemoDataSeeder.EnsureSeeded(app.Services, builder.Configuration, app.Environment);
                EnsureCompanyEmailBackfill(app.Services);
                EnsureGo2RtcRuntimeSynchronized(app.Services);
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseForwardedHeaders();
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<SafeExceptionHandlingMiddleware>();
            app.UseSecurityHeaders();
            if (app.Environment.IsProduction())
            {
                app.UseHsts();
            }
            if (ShouldUseHttpsRedirection(app.Configuration, app.Environment))
            {
                app.UseHttpsRedirection();
            }
            app.UseAuthentication();
            app.Use(async (context, next) =>
            {
                var isSensitiveUpload =
                    context.Request.Path.StartsWithSegments("/uploads", StringComparison.OrdinalIgnoreCase);
                var isRecordedVideo =
                    context.Request.Path.StartsWithSegments("/uploads/recordings", StringComparison.OrdinalIgnoreCase);
                // Bằng chứng FaceID/evidence là ảnh <img> trong trang, không kèm
                // header token nên cho public (giống ảnh demo QR đang dùng).
                var isEvidenceImage =
                    context.Request.Path.StartsWithSegments("/uploads/evidence", StringComparison.OrdinalIgnoreCase);

                if (isSensitiveUpload &&
                    !isRecordedVideo &&
                    !isEvidenceImage &&
                    context.User?.Identity?.IsAuthenticated != true)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                await next();
            });
            app.UseStaticFiles();
            app.UseCors("AllowVue");
            app.UseRateLimiter();
            app.UseAuthorization();
            app.UseMiddleware<SystemRequestAuditMiddleware>();

            app.MapControllers();
            app.MapHub<EmployeeStatsHub>("/hubs/employee-stats").RequireAuthorization();
            app.MapHub<ChatHub>("/hubs/chat").RequireAuthorization();
            app.MapHub<NotificationHub>("/hubs/notifications").RequireAuthorization();
            app.MapHub<ChatRelayHub>("/hubs/chat-relay").AllowAnonymous();
            app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "v-shield-api" })).AllowAnonymous();
            app.MapGet("/health/live", () => Results.Ok(new
            {
                status = "ok",
                service = "v-shield-api",
                checkedAtUtc = DateTime.UtcNow
            })).AllowAnonymous();
            app.MapGet("/health/ready", async (ApplicationDbContext dbContext) =>
            {
                var checks = new Dictionary<string, object>();
                var ready = true;

                try
                {
                    var canConnect = await dbContext.Database.CanConnectAsync();
                    checks["database"] = new { status = canConnect ? "ok" : "unavailable" };
                    ready = ready && canConnect;
                }
                catch (Exception ex)
                {
                    checks["database"] = new { status = "error", message = ex.GetType().Name };
                    ready = false;
                }

                var payload = new
                {
                    status = ready ? "ready" : "not_ready",
                    service = "v-shield-api",
                    checkedAtUtc = DateTime.UtcNow,
                    checks
                };

                return ready
                    ? Results.Ok(payload)
                    : Results.Json(payload, statusCode: StatusCodes.Status503ServiceUnavailable);
            }).AllowAnonymous();

            app.MapGet("/health/degraded", async (ApplicationDbContext dbContext, RuntimeOrchestrator runtimeOrchestrator) =>
            {
                var checks = new Dictionary<string, object>();
                var databaseReady = true;

                try
                {
                    databaseReady = await dbContext.Database.CanConnectAsync();
                    checks["database"] = new { status = databaseReady ? "ok" : "unavailable" };
                }
                catch (Exception ex)
                {
                    databaseReady = false;
                    checks["database"] = new { status = "error", message = ex.GetType().Name };
                }

                var runtimeServices = runtimeOrchestrator.GetServices()
                    .Select(service => new
                    {
                        service.Name,
                        service.DisplayName,
                        service.Enabled,
                        service.AutoStart,
                        service.ManagedMode,
                        service.Running,
                        status = !service.Enabled
                            ? "disabled"
                            : service.Running
                                ? "ok"
                                : service.AutoStart
                                    ? "degraded"
                                    : "manual"
                    })
                    .ToList();

                var runtimeDegraded = runtimeServices.Any(service => service.status == "degraded");
                checks["runtime"] = runtimeServices;

                var payload = new
                {
                    status = !databaseReady ? "not_ready" : runtimeDegraded ? "degraded" : "ok",
                    service = "v-shield-api",
                    checkedAtUtc = DateTime.UtcNow,
                    checks
                };

                return Results.Ok(payload);
            }).AllowAnonymous();

            if (app.Environment.IsEnvironment("Testing"))
            {
                app.MapGet("/__test/throw", () =>
                {
                    throw new InvalidOperationException("Sensitive test exception detail");
                }).AllowAnonymous();
            }

            app.Run();
        }

        private static async Task<int> RunAccessCredentialCommandAsync(
            IServiceProvider services, string[] args, CancellationToken cancellationToken)
        {
            if (args.Length != 2 ||
                !string.Equals(args[1], "inventory", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine("Usage: access-credentials inventory");
                return 2;
            }
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var qrRows = await db.EmployeeDynamicQrs.AsNoTracking()
                .Select(x => new { x.Id, x.EmployeeId, x.IsActive }).ToListAsync(cancellationToken);
            var credentialRows = await db.AccessCredentials.AsNoTracking()
                .Select(x => new
                {
                    x.Id, x.EmployeeId, x.CredentialType, x.Status,
                    x.EffectiveFromUtc, x.ExpiresAtUtc, x.RevokedAtUtc,
                    x.EmployeeDynamicQrId, x.MaskedIdentifier
                }).ToListAsync(cancellationToken);
            var policyTypes = await db.AccessRules.AsNoTracking()
                .GroupBy(x => x.CredentialType)
                .Select(x => new { credentialType = x.Key, count = x.Count() })
                .OrderBy(x => x.credentialType).ToListAsync(cancellationToken);
            var employeeIds = new[] { 1, 2, 3, 4, 5 };
            var report = new
            {
                generatedAtUtc = DateTime.UtcNow,
                employeeDynamicQr = new
                {
                    total = qrRows.Count,
                    active = qrRows.Count(x => x.IsActive),
                    inactive = qrRows.Count(x => !x.IsActive),
                    employeesWithQr = qrRows.Select(x => x.EmployeeId).Distinct().Count(),
                    employeesWithMultipleQr = qrRows.GroupBy(x => x.EmployeeId).Count(x => x.Count() > 1),
                    employees1To5 = employeeIds.Select(id => new
                    {
                        employeeId = id,
                        count = qrRows.Count(x => x.EmployeeId == id),
                        active = qrRows.Count(x => x.EmployeeId == id && x.IsActive),
                        inactive = qrRows.Count(x => x.EmployeeId == id && !x.IsActive)
                    })
                },
                accessCredentials = new
                {
                    total = credentialRows.Count,
                    byType = credentialRows.GroupBy(x => x.CredentialType)
                        .ToDictionary(x => x.Key, x => x.Count()),
                    byStoredStatus = credentialRows.GroupBy(x => x.Status)
                        .ToDictionary(x => x.Key, x => x.Count())
                },
                policyCredentialTypes = policyTypes,
                unmappedPolicyCredentialTypes = policyTypes
                    .Where(x => x.credentialType != "Any" &&
                        AccessCredentialTypes.Normalize(x.credentialType) is null)
                    .Select(x => x.credentialType).ToArray()
            };
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            var root = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (root.Parent is not null &&
                   !Directory.Exists(Path.Combine(root.FullName, "runtime", "face-data")))
                root = root.Parent;
            var path = Path.Combine(root.FullName, "runtime", "face-data", "manifests",
                "access-credential-inventory.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, json, cancellationToken);
            Console.WriteLine(json);
            return 0;
        }

        private static async Task<int> RunFaceModelCommandAsync(
            IServiceProvider services,
            string[] args,
            CancellationToken cancellationToken)
        {
            if (args.Length < 2 ||
                !string.Equals(
                    args[1],
                    "bootstrap-metadata",
                    StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine(
                    "Usage: face-models bootstrap-metadata [--apply --confirm-bootstrap]");
                return 2;
            }

            var apply = args.Contains("--apply", StringComparer.Ordinal);
            var confirm = args.Contains("--confirm-bootstrap", StringComparer.Ordinal);
            var supported = new HashSet<string>(StringComparer.Ordinal)
            {
                "face-models",
                "bootstrap-metadata",
                "--dry-run",
                "--apply",
                "--confirm-bootstrap"
            };
            if (args.Any(argument => !supported.Contains(argument)))
            {
                Console.Error.WriteLine("Unsupported face model bootstrap argument.");
                return 2;
            }

            using var scope = services.CreateScope();
            var bootstrap = scope.ServiceProvider
                .GetRequiredService<IFaceModelMetadataService>();
            var result = await bootstrap.BootstrapAsync(
                apply,
                confirm,
                cancellationToken);
            Console.WriteLine(JsonSerializer.Serialize(result));
            return result.Success ? 0 : 3;
        }

        private static async Task<int> RunFaceCredentialCommandAsync(
            IServiceProvider services,
            string[] args,
            CancellationToken cancellationToken)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Usage: face-credentials generate-binding-template | validate-bindings --manifest <path> | apply-bindings --manifest <path> [--apply --confirm-bindings --actor-user-id <id>] | reconcile-binding-audits --manifest <path> [--apply --confirm-audit-reconciliation --actor-user-id <id>]");
                return 2;
            }

            using var scope = services.CreateScope();
            var manifestService = scope.ServiceProvider.GetRequiredService<FaceCredentialBindingManifestService>();
            var command = args[1];

            if (string.Equals(command, "generate-binding-template", StringComparison.OrdinalIgnoreCase))
            {
                var template = await manifestService.GenerateTemplateAsync(cancellationToken);
                Console.WriteLine(JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true }));
                return 0;
            }

            if (string.Equals(command, "validate-bindings", StringComparison.OrdinalIgnoreCase))
            {
                var manifestPath = ReadManifestPath(args);
                var result = await manifestService.ValidateManifestAsync(
                    manifestPath,
                    requireApproval: false,
                    cancellationToken);
                Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
                return result.Success ? 0 : 3;
            }

            if (string.Equals(command, "apply-bindings", StringComparison.OrdinalIgnoreCase))
            {
                var manifestPath = ReadManifestPath(args);
                var apply = args.Contains("--apply", StringComparer.Ordinal);
                var confirm = args.Contains("--confirm-bindings", StringComparer.Ordinal);
                var actorUserId = ReadOptionalIntArgument(args, "--actor-user-id");
                var result = await manifestService.ApplyManifestAsync(
                    manifestPath,
                    apply,
                    confirm,
                    actorUserId,
                    cancellationToken);
                Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
                return result.Success ? 0 : 3;
            }

            if (string.Equals(command, "reconcile-binding-audits", StringComparison.OrdinalIgnoreCase))
            {
                var manifestPath = ReadManifestPath(args);
                var result = await manifestService.ReconcileBindingAuditsAsync(
                    manifestPath,
                    args.Contains("--apply", StringComparer.Ordinal),
                    args.Contains("--confirm-audit-reconciliation", StringComparer.Ordinal),
                    ReadOptionalIntArgument(args, "--actor-user-id"),
                    cancellationToken);
                Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
                return result.Success ? 0 : 3;
            }

            Console.Error.WriteLine("Unsupported face-credentials command.");
            return 2;
        }

        private static string ReadManifestPath(string[] args)
        {
            var index = Array.FindIndex(args, x => string.Equals(x, "--manifest", StringComparison.Ordinal));
            if (index < 0 || index + 1 >= args.Length)
                throw new InvalidOperationException("--manifest <path> is required.");
            return args[index + 1];
        }

        private static int? ReadOptionalIntArgument(string[] args, string name)
        {
            var index = Array.FindIndex(args, x => string.Equals(x, name, StringComparison.Ordinal));
            if (index < 0) return null;
            if (index + 1 >= args.Length || !int.TryParse(args[index + 1], out var value) || value <= 0)
                throw new InvalidOperationException($"{name} requires a positive integer.");
            return value;
        }

        private static void EnsureCompanyEmailBackfill(IServiceProvider services)
        {
            ExecuteSqlStartupAction("backfill company email", () =>
            {
                using var scope = services.CreateScope();
                var mailOptions = scope.ServiceProvider
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<API.Services.Agent.MailOptions>>().Value;
                if (string.IsNullOrWhiteSpace(mailOptions.Domain)) return;

                var companyEmailService = scope.ServiceProvider.GetRequiredService<API.Services.Agent.CompanyEmailService>();
                companyEmailService.EnsureBackfillAsync(mailOptions.Domain, CancellationToken.None)
                    .GetAwaiter().GetResult();
            });
        }

        private static void EnsureSeedAdminUser(IServiceProvider services, IConfiguration configuration, IHostEnvironment environment)
        {
            ExecuteSqlStartupAction("seed admin user", () =>
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                try
                {
                    db.Database.Migrate();
                }
                catch (SqlException ex) when (ex.Number == 2714) // object already exists
                {
                    Console.WriteLine($"[WARN] Bo qua loi migrate do bang da ton tai: {ex.Message}");
                }

                var seedSection = configuration.GetSection("SeedAdmin");
                var adminUsernameOverride = Environment.GetEnvironmentVariable("VSHIELD_SEED_ADMIN_USERNAME");
                var adminPasswordOverride = Environment.GetEnvironmentVariable("VSHIELD_SEED_ADMIN_PASSWORD");
                var adminFullNameOverride = Environment.GetEnvironmentVariable("VSHIELD_SEED_ADMIN_FULLNAME");
                var adminUsername = (adminUsernameOverride ?? seedSection["Username"] ?? "admin").Trim();
                var adminPassword = adminPasswordOverride ?? seedSection["Password"] ?? "Admin@123";
                var adminFullName = (adminFullNameOverride ?? seedSection["FullName"] ?? "Quan tri vien").Trim();
                var resetPasswordOnStartup = seedSection.GetValue("ResetPasswordOnStartup", false);
                var hasProductionSeedOverrides =
                    !string.IsNullOrWhiteSpace(adminUsernameOverride) &&
                    !string.IsNullOrWhiteSpace(adminPasswordOverride);
                var unsafeDefaultSeed = string.Equals(adminUsername, "admin", StringComparison.OrdinalIgnoreCase) &&
                                        string.Equals(adminPassword, "Admin@123", StringComparison.Ordinal);
                var normalizedAdminUsername = NormalizeUsernameInvariant(adminUsername);

                var adminUser = db.AppUsers.FirstOrDefault(u =>
                    u.Username.Trim().ToUpper() == normalizedAdminUsername);

                if (adminUser == null)
                {
                    if (!db.AppUsers.Any())
                    {
                        if (environment.IsProduction() && (!hasProductionSeedOverrides || unsafeDefaultSeed))
                        {
                            throw new InvalidOperationException("Production requires explicit VSHIELD_SEED_ADMIN_* overrides before bootstrap seeding can create the admin account.");
                        }

                        db.AppUsers.Add(new AppUser
                        {
                            Username = adminUsername,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                            FullName = adminFullName,
                            Role = "Admin",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            LastPasswordChangedAtUtc = DateTime.UtcNow,
                            EmployeeId = null
                        });
                    }

                    db.SaveChanges();
                    return;
                }

                var hasChanges = false;

                if (!string.Equals(adminUser.Username, adminUsername, StringComparison.Ordinal))
                {
                    adminUser.Username = adminUsername;
                    hasChanges = true;
                }

                if (!string.Equals(adminUser.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    adminUser.Role = "Admin";
                    hasChanges = true;
                }

                if (!adminUser.IsActive)
                {
                    adminUser.IsActive = true;
                    hasChanges = true;
                }

                if (string.IsNullOrWhiteSpace(adminUser.FullName))
                {
                    adminUser.FullName = adminFullName;
                    hasChanges = true;
                }

                if (resetPasswordOnStartup && !BCrypt.Net.BCrypt.Verify(adminPassword, adminUser.PasswordHash))
                {
                    if (environment.IsProduction() && (!hasProductionSeedOverrides || unsafeDefaultSeed))
                    {
                        Console.WriteLine("[WARN] Skipping seed admin password reset in Production. Provide VSHIELD_SEED_ADMIN_* overrides to enable reset.");
                    }
                    else
                    {
                        adminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
                        adminUser.LastPasswordChangedAtUtc = DateTime.UtcNow;
                        adminUser.TokenVersion++;
                        hasChanges = true;
                    }
                }

                if (hasChanges)
                {
                    db.SaveChanges();
                }
            });
        }

        private static void ExecuteSqlStartupAction(string actionName, Action action, int maxAttempts = 8, int delaySeconds = 5)
        {
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception ex) when (attempt < maxAttempts && IsTransientSqlStartupException(ex))
                {
                    Console.WriteLine($"[WARN] Startup action '{actionName}' failed on attempt {attempt}/{maxAttempts}. Retrying in {delaySeconds}s. Error: {ex.Message}");
                    Thread.Sleep(TimeSpan.FromSeconds(delaySeconds));
                }
            }

            action();
        }

        private static bool IsTransientSqlStartupException(Exception ex)
        {
            if (ex is SqlException)
            {
                return true;
            }

            if (ex is TimeoutException)
            {
                return true;
            }

            return ex.InnerException != null && IsTransientSqlStartupException(ex.InnerException);
        }

        private static void ConfigureDataProtection(WebApplicationBuilder builder)
        {
            var configuredPath = builder.Configuration["DataProtection:KeyRingPath"];
            var keyRingPath = string.IsNullOrWhiteSpace(configuredPath)
                ? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection")
                : configuredPath;

            Directory.CreateDirectory(keyRingPath);
            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keyRingPath))
                .SetApplicationName("VShield");
        }

        private static string NormalizeUsernameInvariant(string username) =>
            username.Trim().ToUpperInvariant();

        private static string[] ResolveAllowedOrigins(IConfiguration configuration)
        {
            var configuredOrigins = configuration.GetSection("AppSettings:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            var frontendUrl = configuration["AppSettings:FrontendUrl"];
            var allowedOrigins = configuredOrigins
                .Append(frontendUrl ?? string.Empty)
                .Where(origin => !string.IsNullOrWhiteSpace(origin))
                .Select(origin => origin.Trim().TrimEnd('/'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (allowedOrigins.Length > 0)
                return allowedOrigins;

            return new[]
            {
                "http://localhost:5173",
                "http://localhost:5174",
                "http://localhost:5175"
            };
        }

        private static void ValidateProductionSecurityConfiguration(
            IConfiguration configuration,
            IHostEnvironment environment,
            IReadOnlyCollection<string> allowedOrigins)
        {
            var jwtSecretOverride = Environment.GetEnvironmentVariable("VSHIELD_JWT_SECRET");
            var report = SecurityConfigurationHealthService.Evaluate(
                configuration,
                environment,
                allowedOrigins,
                jwtSecretOverride,
                jwtSecretOverride ?? configuration["JwtSettings:Secret"]);

            if (!environment.IsProduction() || report.Status != SecurityConfigurationHealthStatuses.Blocked)
                return;

            var failures = report.Findings
                .Where(finding => finding.Status == SecurityConfigurationFindingStatuses.Fail)
                .Select(finding => $"{finding.Key}: {finding.Message}")
                .ToArray();

            throw new InvalidOperationException("Production security configuration is unsafe. " + string.Join(" | ", failures));
        }

        private static void EnsureGo2RtcRuntimeSynchronized(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

            try
            {
                var cameras = db.Cameras.Where(c => !string.IsNullOrWhiteSpace(c.StreamUrl)).ToList();
                if (!cameras.Any()) return;

                var yaml = new StringBuilder();
                yaml.AppendLine("streams:");
                foreach (var cam in cameras)
                {
                    var streamUrl = cam.StreamUrl?.Trim();
                    if (string.IsNullOrWhiteSpace(streamUrl)) continue;

                    cam.UrlView = BuildStartupCameraViewUrl(config, streamUrl, cam.CameraId);

                    var isDirectWeb = IsStartupDirectWebStream(streamUrl) ||
                                      streamUrl.StartsWith("/", StringComparison.Ordinal) ||
                                      (Uri.TryCreate(streamUrl, UriKind.Absolute, out var uri) &&
                                       (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));
                    if (isDirectWeb) continue;

                    yaml.AppendLine($"  cam{cam.CameraId}:");
                    yaml.AppendLine($"    - {streamUrl}#transport=tcp");
                    if (streamUrl.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase))
                    {
                        yaml.AppendLine($"    - ffmpeg:{streamUrl}#video=mjpeg");
                    }
                    if (!streamUrl.Contains("#transport=", StringComparison.OrdinalIgnoreCase))
                    {
                        yaml.AppendLine($"    - {streamUrl}");
                    }
                }
                yaml.AppendLine("api:");
                yaml.AppendLine("  origin: \"*\"");
                yaml.AppendLine("webrtc:");
                yaml.AppendLine("  listen: \":8555\"");
                var candidates = ResolveStartupGo2RtcCandidates(config).ToList();
                if (candidates.Count > 0)
                {
                    yaml.AppendLine("  candidates:");
                }
                foreach (var candidate in candidates)
                {
                    yaml.AppendLine($"    - {candidate}");
                }
                yaml.AppendLine("  ice_servers:");
                yaml.AppendLine("    - urls:");
                yaml.AppendLine("        - stun:stun.l.google.com:19302");

                var yamlPath = ResolveStartupGo2RtcYamlPath(config);
                var yamlDirectory = Path.GetDirectoryName(yamlPath);
                if (!string.IsNullOrWhiteSpace(yamlDirectory) && !Directory.Exists(yamlDirectory))
                {
                    Directory.CreateDirectory(yamlDirectory);
                }
                File.WriteAllText(yamlPath, yaml.ToString());
                db.SaveChanges();

                if (IsDockerRuntimeMode(config))
                {
                    TryReloadGo2RtcByHttp(httpClientFactory, config);
                    return;
                }

                var go2rtcPath = Path.GetDirectoryName(yamlPath) ?? string.Empty;
                var exePath = Path.Combine(go2rtcPath, "go2rtc.exe");
                if (!Directory.Exists(go2rtcPath) || !File.Exists(exePath)) return;

                foreach (var proc in Process.GetProcessesByName("go2rtc")) proc.Kill();
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = go2rtcPath,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Startup should not crash if go2rtc is unavailable.
            }
        }

        private static bool IsDockerRuntimeMode(IConfiguration configuration)
        {
            var mode = (configuration["Runtime:Mode"] ?? "local").Trim().ToLowerInvariant();
            return mode == "docker";
        }

        private static string ResolveStartupGo2RtcYamlPath(IConfiguration configuration)
        {
            var configured = configuration["Go2Rtc:ConfigPath"];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured.Trim();
            }

            var basePath = Directory.GetCurrentDirectory();
            var aiRootFolderName = configuration["RuntimePaths:AiRootFolderName"] ?? "AI_Runtime";
            var go2rtcPath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", aiRootFolderName, "cam", "go2rtc_win64"));
            return Path.Combine(go2rtcPath, "go2rtc.yaml");
        }

        private static IEnumerable<string> ResolveStartupGo2RtcCandidates(IConfiguration configuration)
        {
            var configured = configuration["Go2Rtc:WebRtcCandidates"];
            if (string.IsNullOrWhiteSpace(configured))
            {
                return Array.Empty<string>();
            }

            return configured
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(value => !string.IsNullOrWhiteSpace(value));
        }

        private static bool IsStartupDirectWebStream(string streamUrl) =>
            streamUrl.Equals("rtsp://demo.local/qr", StringComparison.OrdinalIgnoreCase) ||
            streamUrl.Equals("rtsp://demo.local/plate", StringComparison.OrdinalIgnoreCase);

        private static string? BuildStartupCameraViewUrl(IConfiguration configuration, string? streamUrl, int cameraId)
        {
            var normalized = streamUrl?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return null;
            }

            var publicBaseUrl = ResolveStartupPublicAppBaseUrl(configuration);
            if (normalized.Equals("rtsp://demo.local/qr", StringComparison.OrdinalIgnoreCase))
            {
                return $"{publicBaseUrl}/qr-api/qr/frame.jpg";
            }

            if (normalized.Equals("rtsp://demo.local/plate", StringComparison.OrdinalIgnoreCase))
            {
                return $"{publicBaseUrl}/plate-api/api/camera/stream";
            }

            if (normalized.StartsWith("/", StringComparison.Ordinal))
            {
                return $"{publicBaseUrl}{normalized}";
            }

            if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return normalized;
            }

            var configuredGo2RtcBase = configuration["AppSettings:Go2RtcPublicBaseUrl"];
            var go2RtcBase = !string.IsNullOrWhiteSpace(configuredGo2RtcBase)
                ? configuredGo2RtcBase.Trim().TrimEnd('/')
                : $"{publicBaseUrl}/go2rtc";
            return $"{go2RtcBase}/stream.html?src=cam{cameraId}&mode=webrtc,mse";
        }

        private static string ResolveStartupPublicAppBaseUrl(IConfiguration configuration)
        {
            var configuredFrontendUrl = configuration["AppSettings:FrontendUrl"];
            if (!string.IsNullOrWhiteSpace(configuredFrontendUrl))
            {
                return configuredFrontendUrl.Trim().TrimEnd('/');
            }

            return "http://localhost:5173";
        }

        private static void TryReloadGo2RtcByHttp(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            var reloadUrl = (configuration["Go2Rtc:ReloadUrl"] ?? "http://go2rtc:1984/api/restart").Trim();
            if (string.IsNullOrWhiteSpace(reloadUrl))
            {
                return;
            }

            try
            {
                using var http = httpClientFactory.CreateClient();
                http.Timeout = TimeSpan.FromSeconds(5);
                using var req = new HttpRequestMessage(HttpMethod.Post, reloadUrl);
                using var _ = http.Send(req);
            }
            catch
            {
                // Docker mode: do not crash startup if go2rtc reload endpoint is unavailable.
            }
        }

        private static bool ShouldUseHttpsRedirection(IConfiguration configuration, IHostEnvironment environment)
        {
            if (environment.IsEnvironment("Testing"))
                return false;

            return configuration.GetValue("Security:EnableHttpsRedirection", true);
        }
    }

    internal static class SecurityHeadersApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    var headers = context.Response.Headers;
                    headers.TryAdd("X-Content-Type-Options", "nosniff");
                    headers.TryAdd("X-Frame-Options", "DENY");
                    headers.TryAdd("Referrer-Policy", "no-referrer");
                    headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
                    headers.TryAdd("Content-Security-Policy",
                        "default-src 'self'; " +
                        "script-src 'self'; " +
                        "style-src 'self' 'unsafe-inline'; " +
                        "img-src 'self' data: blob:; " +
                        "connect-src 'self'; " +
                        "font-src 'self'; " +
                        "frame-ancestors 'none'; " +
                        "base-uri 'self'; " +
                        "form-action 'self'");
                    return Task.CompletedTask;
                });

                await next();
            });
        }
    }

    internal sealed class UtcDateTimeJsonConverter : System.Text.Json.Serialization.JsonConverter<DateTime>
    {
        public override DateTime Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
        {
            var value = reader.GetDateTime();
            if (value.Kind == DateTimeKind.Unspecified)
            {
                return DateTime.SpecifyKind(value, DateTimeKind.Utc);
            }

            return value;
        }

        public override void Write(System.Text.Json.Utf8JsonWriter writer, DateTime value, System.Text.Json.JsonSerializerOptions options)
        {
            if (value.Kind == DateTimeKind.Unspecified)
            {
                value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
            }

            writer.WriteStringValue(value);
        }
    }
}



