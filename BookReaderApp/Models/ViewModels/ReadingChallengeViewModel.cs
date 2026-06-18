namespace BookReaderApp.Models.ViewModels;

// The annual reading challenge shown on a profile: the goal, how many books the user
// has finished this year, progress toward the goal, and fixed-count milestone badges.
public class ReadingChallengeViewModel
{
    public int Year { get; set; }

    // The user's yearly books-to-read target, or null when no goal is set.
    public int? Goal { get; set; }

    public int FinishedCount { get; set; }

    // Whether the viewer owns this profile; drives the "edit goal" affordance.
    public bool IsOwnProfile { get; set; }

    // Progress as a 0–100 percentage, or null when there is no usable goal.
    public int? PercentComplete =>
        Goal is > 0 ? Math.Min(100, FinishedCount * 100 / Goal.Value) : null;

    public IReadOnlyList<MilestoneViewModel> Milestones { get; set; } = [];
}

// A single fixed-count milestone (e.g. "10 books"), and whether it has been reached.
public class MilestoneViewModel
{
    public int Count { get; set; }

    public bool Unlocked { get; set; }
}
