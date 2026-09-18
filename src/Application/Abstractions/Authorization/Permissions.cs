namespace Application.Abstractions.Authorization;

public static class Permissions
{
    public static class Organizations
    {
        public const string Create = "organizations:create";
        public const string Edit = "organizations:edit";
        public const string Delete = "organizations:delete";
        public const string Archive = "organizations:archive";

        public static IReadOnlyCollection<string> All { get; } = [Create, Edit, Delete, Archive];
    }

    public static class Projects
    {
        public const string Create = "projects:create";
        public const string Edit = "projects:edit";
        public const string Delete = "projects:delete";
        public const string Archive = "projects:archive";

        public static IReadOnlyCollection<string> All { get; } = [Create, Edit, Delete, Archive];
    }
}
