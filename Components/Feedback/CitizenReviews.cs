using System.Globalization;

namespace KalsadaWatchApp.Components.Feedback;

/// <summary>One resident rating plus comment, as shown in the homepage feedback section.</summary>
public sealed record CitizenReview(
    string Id,
    string DisplayName,      // first name + last initial, or "Anonymous resident"
    string Area,             // barangay or street
    DateTimeOffset PostedAt,
    int? Rating,             // 1 to 5, or null for a comment-only review
    string? Comment,         // null for a rating-only review (at least one of the two is present)
    string? TicketId,        // optional linked report, e.g. "#KW-8910"
    bool IsResolved,         // true when the linked report is closed
    bool IsApproved);        // moderation flag: only approved reviews are shown

/// <summary>Where the feedback section gets its reviews. Swap the implementation to connect real data.</summary>
public interface IReviewSource
{
    Task<IReadOnlyList<CitizenReview>> GetApprovedReviewsAsync(CancellationToken cancellationToken = default);
}

/// <summary>Average and star breakdown, always calculated from the reviews passed in. Only reviews with a rating count.</summary>
public sealed record ReviewSummary(double Average, int Count, IReadOnlyList<ReviewSummary.Bucket> Distribution)
{
    public sealed record Bucket(int Stars, int Count, double Percent);

    public string AverageText => Average.ToString("0.0", CultureInfo.InvariantCulture);
    public int RoundedStars => (int)Math.Round(Average, MidpointRounding.AwayFromZero);

    public static ReviewSummary From(IReadOnlyList<CitizenReview> reviews)
    {
        var rated = reviews.Where(r => r.Rating is >= 1 and <= 5).Select(r => r.Rating!.Value).ToList();
        if (rated.Count == 0)
            return new ReviewSummary(0, 0, Array.Empty<Bucket>());

        var counts = Enumerable.Range(1, 5).Reverse()
            .Select(stars => (Stars: stars, Count: rated.Count(r => r == stars)))
            .ToList();
        var max = Math.Max(1, counts.Max(c => c.Count));
        var buckets = counts.Select(c => new Bucket(c.Stars, c.Count, 100.0 * c.Count / max)).ToList();
        return new ReviewSummary(rated.Average(), rated.Count, buckets);
    }
}

/// <summary>Used outside Development until a real ratings/comments source exists. Shows the empty state.</summary>
public sealed class EmptyReviewSource : IReviewSource
{
    public Task<IReadOnlyList<CitizenReview>> GetApprovedReviewsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CitizenReview>>(Array.Empty<CitizenReview>());
}

// =====================================================================
// PLACEHOLDER DATA. These reviews are invented for design and development.
// They are NOT real residents. Program.cs only registers this class when
// the app runs in the Development environment, so it cannot ship to
// production by accident. Replace it with a real source before launch.
// =====================================================================
public sealed class MockReviewSource : IReviewSource
{
    public Task<IReadOnlyList<CitizenReview>> GetApprovedReviewsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var all = new List<CitizenReview>
        {
            new("mock-1", "Marisol B.", "Rizal Ave, Ermita", now.AddDays(-2), 5,
                "The pothole in front of the Civic Center was patched in two days. The crew even repainted the pedestrian crossing.",
                "#KW-8902", true, true),
            new("mock-2", "Jasmine T.", "South Expressway Service Rd", now.AddDays(-3), 5,
                "Smooth road now. I drive this every morning and it used to wreck my tires.",
                "#KW-8910", true, true),
            new("mock-3", "Carmela R.", "Juan Luna St, Binondo", now.AddDays(-4), 5,
                "Nag-report ako ng malalim na lubak sa Juan Luna. Naayos agad bago pa magka-aksidente. Salamat!",
                null, false, true),
            new("mock-4", "Anonymous resident", "Quezon Blvd", now.AddDays(-5), 4,
                "Fixed within the week after I reported it. Status updates could come faster, but the patch held up through last weekend's heavy rain.",
                "#KW-8898", true, true),
            new("mock-5", "Renato D.", "Sampaloc", now.AddDays(-8), 3,
                "The report went through fine, but it took longer than the 48 hours promised. I had to check the map myself to see progress.",
                null, false, true),
            new("mock-6", "Emmanuel B.", "Tondo", now.AddDays(-16), 4,
                "Easy to file a report from my phone. I would like SMS updates in Filipino too.",
                null, false, true),
            new("mock-8", "Anonymous resident", "Taft Ave, Malate", now.AddDays(-6), 4,
                null, null, false, true), // rating-only: counted in the average, not shown as a tile
            // Not approved by moderation: must never appear on the page. Kept to prove the filter works.
            new("mock-7", "Test user", "Test area", now.AddDays(-1), 1,
                "UNMODERATED MOCK REVIEW: this text must not render.",
                null, false, false),
        };
        IReadOnlyList<CitizenReview> approved = all.Where(r => r.IsApproved).ToList();
        return Task.FromResult(approved);
    }
}
