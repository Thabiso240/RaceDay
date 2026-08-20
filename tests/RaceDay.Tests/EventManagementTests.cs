using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Controllers;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.Tests;

public class MockBlobService : IBlobStorageService
{
    public Task<string> UploadFileAsync(Stream fileStream, string originalFileName, string containerName, string contentType)
    {
        return Task.FromResult($"https://mockstorage.blob.core.windows.net/{containerName}/{Guid.NewGuid():N}.jpg");
    }

    public Task<bool> DeleteFileAsync(string fileUrl, string containerName)
    {
        return Task.FromResult(true);
    }
}

public class EventManagementTests
{
    private readonly IBlobStorageService _mockBlob = new MockBlobService();

    [Fact]
    public async Task CreateEvent_OrganiserRole_CreatesEventSuccessfully()
    {
        // Arrange
        using var context = TestDatabaseFixture.CreateInMemoryContext();
        var (organiser, _, _) = await TestDatabaseFixture.SeedOrganiserAndEventAsync(context);

        var controller = new EventsController(context, _mockBlob)
        {
            ControllerContext = TestDatabaseFixture.CreateControllerContext(organiser.UserId, "Organiser")
        };

        var dto = new CreateEventDto
        {
            Title = "Jarred Coastal 10k Classic",
            Description = "Fast-paced flat road race across the shore.",
            EventDate = DateTime.UtcNow.AddDays(20),
            Location = "Durban North Beach",
            DistanceKm = 10.00m,
            EventType = "Run",
            BannerImageUrl = "https://images.unsplash.com/photo-run"
        };

        // Act
        var result = await controller.CreateEvent(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var eventDto = Assert.IsType<EventDto>(createdResult.Value);
        Assert.Equal("Jarred Coastal 10k Classic", eventDto.Title);
        Assert.Equal(10.00m, eventDto.DistanceKm);
        Assert.Equal("Run", eventDto.EventType);
        Assert.Equal(organiser.UserId, eventDto.OrganiserId);
    }

    [Fact]
    public async Task GetEvents_FilterByEventType_ReturnsOnlyMatchingEvents()
    {
        // Arrange
        using var context = TestDatabaseFixture.CreateInMemoryContext();
        var (organiser, _, _) = await TestDatabaseFixture.SeedOrganiserAndEventAsync(context);

        // Add Walk and Cycle events
        context.Events.Add(new Event
        {
            OrganiserId = organiser.UserId,
            Title = "Promenade Morning Walk",
            Description = "Leisure stroll",
            EventDate = DateTime.UtcNow.AddDays(5),
            Location = "Beach Promenade",
            DistanceKm = 5.0m,
            EventType = "Walk",
            Status = "Upcoming"
        });
        context.Events.Add(new Event
        {
            OrganiserId = organiser.UserId,
            Title = "Valley Cycling Tour",
            Description = "Mountain pass",
            EventDate = DateTime.UtcNow.AddDays(10),
            Location = "Drakensberg",
            DistanceKm = 80.0m,
            EventType = "Cycle",
            Status = "Upcoming"
        });
        await context.SaveChangesAsync();

        var controller = new EventsController(context, _mockBlob);

        // Act
        var result = await controller.GetEvents("Cycle", null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<EventDto>>(okResult.Value).ToList();
        Assert.Single(list);
        Assert.Equal("Cycle", list[0].EventType);
        Assert.Equal("Valley Cycling Tour", list[0].Title);
    }

    [Fact]
    public async Task UpdateEvent_OwnEvent_UpdatesSuccessfully()
    {
        // Arrange
        using var context = TestDatabaseFixture.CreateInMemoryContext();
        var (organiser, raceEvent, _) = await TestDatabaseFixture.SeedOrganiserAndEventAsync(context);

        var controller = new EventsController(context, _mockBlob)
        {
            ControllerContext = TestDatabaseFixture.CreateControllerContext(organiser.UserId, "Organiser")
        };

        var updateDto = new UpdateEventDto
        {
            Title = "Updated Championship Title",
            Description = "Updated Description",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "New Stadium Track",
            DistanceKm = 21.10m,
            EventType = "Run",
            Status = "Upcoming"
        };

        // Act
        var result = await controller.UpdateEvent(raceEvent.EventId, updateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updated = Assert.IsType<EventDto>(okResult.Value);
        Assert.Equal("Updated Championship Title", updated.Title);
        Assert.Equal("New Stadium Track", updated.Location);
    }

    [Fact]
    public async Task UpdateEvent_DifferentOrganiser_ReturnsForbiddenStatusCode403()
    {
        // Arrange
        using var context = TestDatabaseFixture.CreateInMemoryContext();
        var (organiser1, raceEvent, _) = await TestDatabaseFixture.SeedOrganiserAndEventAsync(context);

        var secondOrganiser = await TestDatabaseFixture.SeedParticipantAsync(context, "Second Organiser");
        secondOrganiser.Role = "Organiser";
        await context.SaveChangesAsync();

        var controller = new EventsController(context, _mockBlob)
        {
            // Logged in as secondOrganiser trying to modify organiser1's event
            ControllerContext = TestDatabaseFixture.CreateControllerContext(secondOrganiser.UserId, "Organiser")
        };

        var updateDto = new UpdateEventDto
        {
            Title = "Unauthorized Takeover",
            Description = "Illegal change",
            EventDate = DateTime.UtcNow.AddDays(10),
            Location = "Venue",
            DistanceKm = 10m,
            EventType = "Run"
        };

        // Act
        var result = await controller.UpdateEvent(raceEvent.EventId, updateDto);

        // Assert
        var forbiddenResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, forbiddenResult.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_WithActiveConfirmedEnrolments_ReturnsConflictStatusCode409()
    {
        // Arrange
        using var context = TestDatabaseFixture.CreateInMemoryContext();
        var (organiser, raceEvent, category) = await TestDatabaseFixture.SeedOrganiserAndEventAsync(context);
        var participant = await TestDatabaseFixture.SeedParticipantAsync(context);

        // Add active enrolment
        context.Enrolments.Add(new Enrolment
        {
            ParticipantId = participant.UserId,
            EventId = raceEvent.EventId,
            CategoryId = category.CategoryId,
            Status = "Confirmed",
            BibNumber = 1001
        });
        await context.SaveChangesAsync();

        var controller = new EventsController(context, _mockBlob)
        {
            ControllerContext = TestDatabaseFixture.CreateControllerContext(organiser.UserId, "Organiser")
        };

        // Act
        var result = await controller.DeleteEvent(raceEvent.EventId);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflictResult.StatusCode);
    }
}
