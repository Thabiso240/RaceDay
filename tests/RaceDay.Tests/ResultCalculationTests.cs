using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Controllers;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.Tests;

public class ResultCalculationTests
{
    [Fact]
    public async Task CaptureResults_MultipleFinishers_CalculatesPositionsAscendingByTime()
    {
        // Arrange
        using var context = TestDatabaseFixture.CreateInMemoryContext();
        var (organiser, raceEvent, category) = await TestDatabaseFixture.SeedOrganiserAndEventAsync(context);

        var athlete1 = await TestDatabaseFixture.SeedParticipantAsync(context, "Runner Swift");
        var athlete2 = await TestDatabaseFixture.SeedParticipantAsync(context, "Runner Medium");
        var athlete3 = await TestDatabaseFixture.SeedParticipantAsync(context, "Runner Steady");

        var enr1 = new Enrolment { ParticipantId = athlete1.UserId, EventId = raceEvent.EventId, CategoryId = category.CategoryId, BibNumber = 101, Status = "Confirmed" };
        var enr2 = new Enrolment { ParticipantId = athlete2.UserId, EventId = raceEvent.EventId, CategoryId = category.CategoryId, BibNumber = 102, Status = "Confirmed" };
        var enr3 = new Enrolment { ParticipantId = athlete3.UserId, EventId = raceEvent.EventId, CategoryId = category.CategoryId, BibNumber = 103, Status = "Confirmed" };
        context.Enrolments.AddRange(enr1, enr2, enr3);
        await context.SaveChangesAsync();

        var controller = new ResultsController(context)
        {
            ControllerContext = TestDatabaseFixture.CreateControllerContext(organiser.UserId, "Organiser")
        };

        // Note: athlete2 submitted 00:45:00, athlete1 submitted 00:39:30 (fastest), athlete3 submitted 00:52:10
        var batchDto = new CaptureResultsBatchDto
        {
            EventId = raceEvent.EventId,
            Results = new List<CaptureResultItemDto>
            {
                new() { EnrolmentId = enr2.EnrolmentId, FinishTime = "00:45:00" },
                new() { EnrolmentId = enr1.EnrolmentId, FinishTime = "00:39:30" }, // Fastest
                new() { EnrolmentId = enr3.EnrolmentId, FinishTime = "00:52:10" }
            }
        };

        // Act
        var actionResult = await controller.CaptureResults(batchDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult);
        var results = Assert.IsAssignableFrom<IEnumerable<ResultDto>>(createdResult.Value).ToList();

        Assert.Equal(3, results.Count);

        // 1st place should be athlete1 (00:39:30)
        Assert.Equal(enr1.EnrolmentId, results[0].EnrolmentId);
        Assert.Equal(1, results[0].FinishingPosition);
        Assert.Equal(3, results[0].TotalFinishers);

        // 2nd place should be athlete2 (00:45:00)
        Assert.Equal(enr2.EnrolmentId, results[1].EnrolmentId);
        Assert.Equal(2, results[1].FinishingPosition);

        // 3rd place should be athlete3 (00:52:10)
        Assert.Equal(enr3.EnrolmentId, results[2].EnrolmentId);
        Assert.Equal(3, results[2].FinishingPosition);
    }

    [Fact]
    public async Task GetMyResults_FormatsPositionDisplayAccurately()
    {
        // Arrange
        using var context = TestDatabaseFixture.CreateInMemoryContext();
        var (organiser, raceEvent, category) = await TestDatabaseFixture.SeedOrganiserAndEventAsync(context);
        var athlete = await TestDatabaseFixture.SeedParticipantAsync(context, "Jarred Podium Star");

        var enrolment = new Enrolment
        {
            ParticipantId = athlete.UserId,
            EventId = raceEvent.EventId,
            CategoryId = category.CategoryId,
            BibNumber = 1047,
            Status = "Confirmed"
        };
        context.Enrolments.Add(enrolment);
        await context.SaveChangesAsync();

        // Seed 47th out of 312 finishers result as specified in the POE brief
        var resultRecord = new Result
        {
            EnrolmentId = enrolment.EnrolmentId,
            ParticipantId = athlete.UserId,
            EventId = raceEvent.EventId,
            FinishTime = "01:24:35",
            FinishSeconds = 5075,
            FinishingPosition = 47,
            TotalFinishers = 312,
            Notes = "47th out of 312 finishers - Great Pace!",
            PublishedAt = DateTime.UtcNow
        };
        context.Results.Add(resultRecord);
        await context.SaveChangesAsync();

        var controller = new ResultsController(context)
        {
            ControllerContext = TestDatabaseFixture.CreateControllerContext(athlete.UserId, "Participant")
        };

        // Act
        var result = await controller.GetMyResults();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<PersonalResultDto>>(okResult.Value).ToList();
        Assert.Single(list);

        var personal = list[0];
        Assert.Equal(47, personal.FinishingPosition);
        Assert.Equal(312, personal.TotalFinishers);
        Assert.Equal("47th out of 312 finishers", personal.PositionDisplay);
        Assert.Equal("01:24:35", personal.FinishTime);
    }

    [Fact]
    public async Task CaptureResults_InvalidTimeFormat_ReturnsBadRequest()
    {
        // Arrange
        using var context = TestDatabaseFixture.CreateInMemoryContext();
        var (organiser, raceEvent, _) = await TestDatabaseFixture.SeedOrganiserAndEventAsync(context);

        var controller = new ResultsController(context)
        {
            ControllerContext = TestDatabaseFixture.CreateControllerContext(organiser.UserId, "Organiser")
        };

        var batchDto = new CaptureResultsBatchDto
        {
            EventId = raceEvent.EventId,
            Results = new List<CaptureResultItemDto>
            {
                new() { EnrolmentId = 1, FinishTime = "Not-A-Time" }
            }
        };

        // Act
        var result = await controller.CaptureResults(batchDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }
}
