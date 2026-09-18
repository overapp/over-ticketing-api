using SharedKernel;

namespace Domain.Projects;

public static class ProjectErrors
{
    public static Error NotFound(Guid projectId) => Error.NotFound(
        "Projects.NotFound",
        $"The project with the Id = '{projectId}' was not found");

    public static Error AlreadyArchived(Guid projectId) => Error.Problem(
        "Projects.AlreadyArchived",
        $"The project with the Id = '{projectId}' is already archived");

    public static Error AlreadyActive(Guid projectId) => Error.Problem(
        "Projects.AlreadyActive",
        $"The project with the Id = '{projectId}' is already active");
}
