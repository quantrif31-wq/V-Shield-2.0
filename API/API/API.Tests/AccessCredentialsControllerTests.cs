using API.Controllers;
using API.Models;
using API.Services.AccessCredentials;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.Tests;

public sealed class AccessCredentialsControllerTests
{
    private static readonly AccessCredentialDto SampleDto = new(
        Id: 1,
        EmployeeId: 10,
        EmployeeName: "Khoi",
        CredentialType: AccessCredentialTypes.Card,
        StoredStatus: "Active",
        EffectiveStatus: "Active",
        EffectiveFromUtc: null,
        ExpiresAtUtc: null,
        RevokedAtUtc: null,
        MaskedIdentifier: "****",
        EmployeeDynamicQrId: null,
        CreatedAtUtc: DateTime.UtcNow,
        UpdatedAtUtc: null,
        Description: null,
        RowVersion: "row-1");

    private static Mock<IAccessCredentialService> MockService()
    {
        return new Mock<IAccessCredentialService>();
    }

    private static (AccessCredentialsController Controller, Mock<IAccessCredentialService> Service) Create()
    {
        var service = MockService();
        return (new AccessCredentialsController(service.Object), service);
    }

    [Fact]
    public async Task List_ReturnsServiceResult()
    {
        var (controller, service) = Create();
        service.Setup(s => s.ListAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { SampleDto });

        var result = await controller.List(10, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsType<AccessCredentialDto[]>(ok.Value);
        Assert.Single(items);
    }

    [Fact]
    public async Task Get_Found_ReturnsOk()
    {
        var (controller, service) = Create();
        service.Setup(s => s.GetAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleDto);

        var result = await controller.Get(1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(1, ((AccessCredentialDto)ok.Value!).Id);
    }

    [Fact]
    public async Task Get_Missing_ReturnsNotFound()
    {
        var (controller, service) = Create();
        service.Setup(s => s.GetAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccessCredentialDto?)null);

        var result = await controller.Get(99, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        var (controller, service) = Create();
        var request = new CreateAccessCredentialRequest(10, AccessCredentialTypes.Card, "1234",
            null, null, null, null);
        service.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleDto);

        var result = await controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(AccessCredentialsController.Get), created.ActionName);
    }

    [Fact]
    public async Task Create_WithTransition_ReturnsCreated()
    {
        var (controller, service) = Create();
        var request = new CreateAccessCredentialRequest(10, AccessCredentialTypes.Card, "1234",
            null, null, null, null, Activate: true);
        service.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleDto);

        var result = await controller.Create(request, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result.Result);
        service.Verify(s => s.CreateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Activate_ReturnsOk()
    {
        var (controller, service) = Create();
        service.Setup(s => s.TransitionAsync(1, AccessCredentialStatuses.Active, "row-1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleDto);

        var result = await controller.Activate(1, new AccessCredentialTransitionRequest("row-1", null), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(1, ((AccessCredentialDto)ok.Value!).Id);
    }

    [Fact]
    public async Task Deactivate_ReturnsOk()
    {
        var (controller, service) = Create();
        service.Setup(s => s.TransitionAsync(1, AccessCredentialStatuses.Inactive, "row-1", "reason", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleDto);

        var result = await controller.Deactivate(1, new AccessCredentialTransitionRequest("row-1", "reason"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        service.Verify(s => s.TransitionAsync(1, AccessCredentialStatuses.Inactive, "row-1", "reason", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Revoke_ReturnsOk()
    {
        var (controller, service) = Create();
        service.Setup(s => s.TransitionAsync(1, AccessCredentialStatuses.Revoked, "row-1", "lost", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleDto);

        var result = await controller.Revoke(1, new AccessCredentialTransitionRequest("row-1", "lost"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Execute_DomainException_ReturnsProblemDetailsWithStatus()
    {
        var (controller, service) = Create();
        service.Setup(s => s.TransitionAsync(1, AccessCredentialStatuses.Revoked, "row-1", null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AccessCredentialDomainException("CARD_LOCKED", "Card is locked", 409));

        var result = await controller.Revoke(1, new AccessCredentialTransitionRequest("row-1", null), CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(409, status.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(status.Value);
        Assert.Equal("CARD_LOCKED", problem.Title);
    }

    [Fact]
    public async Task EmployeeAccessCredentialsController_List_ReturnsItems()
    {
        var service = MockService();
        service.Setup(s => s.ListAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { SampleDto });
        var controller = new EmployeeAccessCredentialsController(service.Object);

        var result = await controller.List(10, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Single((AccessCredentialDto[])ok.Value!);
    }
}