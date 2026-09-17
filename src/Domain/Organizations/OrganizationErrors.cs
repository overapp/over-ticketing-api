using SharedKernel;

namespace Domain.Organizations;

public static class OrganizationErrors
{
    public static Error NotFound(Guid organizationId) => Error.NotFound(
        "Organizations.NotFound",
        $"The organization with the Id = '{organizationId}' was not found");

    public static Error AlreadyArchived(Guid organizationId) => Error.Problem(
        "Organizations.AlreadyArchived",
        $"The organization with the Id = '{organizationId}' is already archived");

    public static Error AlreadyActive(Guid organizationId) => Error.Problem(
        "Organizations.AlreadyActive",
        $"The organization with the Id = '{organizationId}' is already active");
}
