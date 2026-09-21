namespace Domain.Tickets;

public static class TicketSlaCalculator
{
    public static (DateTime FirstResponseDueAt, DateTime ResolutionDueAt) CalculateDueDates(
        DateTime createdAt,
        TicketPriority priority)
    {
        return priority switch
        {
            TicketPriority.Urgent => (
                createdAt.AddHours(1),
                createdAt.AddHours(4)),

            TicketPriority.High => (
                createdAt.AddHours(4),
                createdAt.AddHours(24)),

            TicketPriority.Medium => (
                createdAt.AddHours(8),
                createdAt.AddHours(48)),

            TicketPriority.Low => (
                createdAt.AddHours(24),
                createdAt.AddHours(72)),

            _ => (
                createdAt.AddHours(8),
                createdAt.AddHours(48))
        };
    }
}
