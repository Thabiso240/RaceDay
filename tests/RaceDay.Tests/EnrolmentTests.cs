using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Controllers;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.Tests;

public class EnrolmentTests
{
    [Fact]
    public async Task Enrol_ParticipantRole_SuccessfullyEnrolsAndAssignsBib()
    {
        // Arrange
        using var context = TestDatabaseFixture.CreateInMemoryContext();
        var (_, raceEvent, category) = await TestDatabaseFixture.SeedOrganiserAndEventAsync(context);
        var participant = await TestDatabaseFixture.SeedParticipantAsync(context, "Jarred Sprinter");

        var controller = new EnrolmentsController(context)
        {
            ControllerContext = TestDatabaseFixture.CreateControllerContext(participant.UserId, "Participant", participant.FullName, participant.Email)
        };

        var dto = new CreateEnrolmentDto
        {
            EventId = raceEvent.EventId,
            CategoryId = category.CategoryId
        };

        // Act
        var result = await controller.Enrol(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var enrolmentDto = Assert.IsType<EnrolmentDto>(createdResult.Value);
        Assert.Equal(participant.UserId, enrolmentDto.ParticipantId);
        Assert.Equal(raceEvent.EventId, enrolmentDto.EventId);
        Assert.Equal(category.CategoryId, enrolmentDto.CategoryId);
        Assert.Equal("Confirmed", enrolmentDto.Status);
        Assert.True(enrolmentDto.BibNumber >= 1001);
    }

    [Fact]
    public async Task Enrol_DuplicateEntry_ReturnsConflictStatusCode409()
    {
        // Arrange
        using var context = TestDatabaseFixture.CreateInMemoryContext();
        var (_, raceEvent, category) = await TestDatabaseFixture.SeedOrganiserAndEventAsync(context);
        var participant = await TestDatabaseFixture.SeedParticipantAsync(context);

        var controller = new EnrolmentsController(context)
        {
            ControllerContext = TestDatabaseFixture.CreateControllerContext(participant.UserId, "Participant", participant.FullName, participant.Email)
        };

        var dto = new CreateEnrolmentDto
        {
            EventId = raceEvent.EventId,
            CategoryId = category.CategoryId
        };

        // First enrolment succeeds
        await controller.Enrol(dto);

        // Act: second attempt to enter same event
        var duplicateResult = await controller.Enrol(dto);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(duplicateResult);
        Assert.Equal(409, conflictResult.StatusCode);
    }

    [Fact]
    public async Task Enrol_InvalidCategoryForEvent_ReturnsBadRequest()
    {
        // Arrange
        using var context = TestDatabaseFixture.CreateInMemoryContext();
        var (_, raceEvent, _) = await TestDatabaseFixture.SeedOrganiserAndEventAsync(context);
        var participant = await TestDatabaseFixture.SeedParticipantAsync(context);

        var controller = new EnrolmentsController(context)
        {
            ControllerContext = TestDatabaseFixture.CreateControllerContext(participant.UserId, "Participant")
        };

        var dto = new CreateEnrolmentDto
        {
            EventId = raceEvent.EventId,
            CategoryId = 99999 // Non-existent category
        };

        // Act
        var result = await controller.Enrol(dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task GetMyEnrolments_ReturnsParticipantEnrolments()
    {
        // Arrange
        using var context = TestDatabaseFixture.CreateInMemoryContext();
        var (_, raceEvent, category) = await TestDatabaseFixture.SeedOrganiserAndEventAsync(context);
        var participant = await TestDatabaseFixture.SeedParticipantAsync(context);

        context.Enrolments.Add(new Enrolment
        {
            ParticipantId = participant.UserId,
            EventId = raceEvent.EventId,
            CategoryId = category.CategoryId,
            Status = "Confirmed",
            BibNumber = 1055,
            EnrolmentDate = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = new EnrolmentsController(context)
        {
            ControllerContext = TestDatabaseFixture.CreateControllerContext(participant.UserId, "Participant", participant.FullName, participant.Email)
        };

        // Act
        var result = await controller.GetMyEnrolments();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<EnrolmentDto>>(okResult.Value).ToList();
        Assert.Single(list);
        Assert.Equal(1055, list[0].BibNumber);
        Assert.Equal("Confirmed", list[0].Status);
    }

    [Fact]
    public async Task UpdateStatus_OrganiserRole_UpdatesEnrolmentStatus()
    {
        // Arrange
        using var context = TestDatabaseFixture.CreateInMemoryContext();
        var (organiser, raceEvent, category) = await TestDatabaseFixture.SeedOrganiserAndEventAsync(context);
        var participant = await TestDatabaseFixture.SeedParticipantAsync(context);

        var enrolment = new Enrolment
        {
            ParticipantId = participant.UserId,
            EventId = raceEvent.EventId,
            CategoryId = category.CategoryId,
            Status = "Pending",
            BibNumber = 1099
        };
        context.Enrolments.Add(enrolment);
        await context.SaveChangesAsync();

        var controller = new EnrolmentsController(context)
        {
            ControllerContext = TestDatabaseFixture.CreateControllerContext(organiser.UserId, "Organiser")
        };

        // Act: Organiser marks enrolment as Confirmed
        var result = await controller.UpdateStatus(enrolment.EnrolmentId, new UpdateEnrolmentStatusDto { Status = "Confirmed" });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<EnrolmentDto>(okResult.Value);
        Assert.Equal("Confirmed", dto.Status);
    }
}
