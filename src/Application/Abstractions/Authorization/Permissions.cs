namespace Application.Abstractions.Authorization;

public static class Permissions
{
    public static class Organizations
    {
        public const string Read = "organizations:read";
        public const string Create = "organizations:create";
        public const string Edit = "organizations:edit";
        public const string Delete = "organizations:delete";
        public const string Archive = "organizations:archive";

        public static IReadOnlyCollection<string> All { get; } = [Read, Create, Edit, Delete, Archive];
    }

    public static class Projects
    {
        public const string Read = "projects:read";
        public const string Create = "projects:create";
        public const string Edit = "projects:edit";
        public const string Delete = "projects:delete";
        public const string Archive = "projects:archive";

        public static IReadOnlyCollection<string> All { get; } = [Read, Create, Edit, Delete, Archive];
    }

    public static class Users
    {
        public const string Read = "users:read";
        public const string Create = "users:create";
        public const string Edit = "users:edit";
        public const string Delete = "users:delete";

        public static IReadOnlyCollection<string> All { get; } = [Read, Create, Edit, Delete];
    }

    public static class Tickets
    {
        public const string Read = "tickets:read";
        public const string Create = "tickets:create";
        public const string Reply = "tickets:reply";
        public const string Assign = "tickets:assign";
        public const string StatusUpdate = "tickets:status:update";
        public const string PriorityUpdate = "tickets:priority:update";

        public static IReadOnlyCollection<string> All { get; } =
            [Read, Create, Reply, Assign, StatusUpdate, PriorityUpdate];
    }
}
