namespace Application.Projects.AssignUser;

public sealed record ProjectAssignedEmailModel(
    string RecipientName,
    string ProjectName,
    string Role,
    Uri ProjectUrl);
