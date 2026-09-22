# Domain Glossary: Over-Ticketing

A unified support ticket management platform serving multiple customers (Organizations) and internal teams. Multi-tenant, with role-based access control and SLA-driven workflows.

## Core Entities

### Organization

An isolated tenant (customer or internal team). Organizations are completely separate; users, projects, and tickets do not cross organization boundaries.

**Properties:**
- `Name`: display name
- `Status`: Active or Archived
- `Projects`: collection of projects within this org

**Lifecycle:**
- Can be archived (soft-delete: stays visible, locked, can reactivate)
- Can be deleted (hard-delete: permanent removal)
- Archiving unassigns all project members (they need reassignment)

---

### Project

A container within an Organization for organizing tickets and workflows. Examples: "Support", "HR", "Asset Requests", "Bug Reports".

**Properties:**
- `Name`, `Description`
- `Status`: Active or Archived
- `OrganizationId`: the parent org
- `ProjectAssignments`: collection of agents assigned to this project

**Lifecycle:**
- Can be archived (soft-delete: stays visible, locked, can reactivate)
- Can be deleted (hard-delete: permanent removal)
- Archiving unassigns all project members

---

### User

A person interacting with the system. Can have multiple roles across different projects and organizations.

**Roles:**

| Role | Scope | Characteristics |
|------|-------|-----------------|
| **Admin** | Global | Outside all orgs. Implicitly assigned to all projects. Can create/manage organizations, projects, users. Can respond to any ticket. Exclusive role (cannot also be agent or customer). |
| **Support Agent** | Project-scoped | Assigned to specific projects. Can respond to tickets in assigned projects. Can create tickets (for customers or themselves). Can self-assign or be reassigned tickets within project. Can see internal notes in assigned project. |
| **Customer** | One or more orgs | Belongs to one or more organizations (via actual ticket creation/usage, not explicit enrollment). Can create tickets. Can view/reply to/close their own tickets. Can see public wiki pages. Cannot see other customers' tickets or internal notes. |

**Multi-role users:** A single user can be an Agent in one project AND a Customer in another project/org (e.g., your internal employees supporting external customers). Admin is exclusive.

**Deletion constraint:** A user cannot be deleted if they created any tickets (maintains audit trail).

**Side effects of deletion:**
- Project assignments are deleted (user unassigned from projects)
- Tickets they are assigned to become unassigned (`AssignedToUserId` → null)
- Tickets they created cannot be deleted (constraint prevents user deletion)

---

### Ticket

A support request, bug report, or internal task. Belongs to a single Project.

**Properties:**
- `Title`: display name
- `ProjectId`, `CreatedByUserId`, `AssignedToUserId` (nullable)
- `Status`: one of the lifecycle states
- `Priority`: High, Medium, Low (drives SLA enforcement)
- `CategoryId` (nullable): can be changed after creation
- `CreatedAt`, `UpdatedAt`, `ClosedAt`
- `FirstResponseDueAt`, `FirstResponseAt`: SLA-tracked
- `ResolutionDueAt`, `ResolvedAt`: SLA-tracked
- `Messages`: replies and internal notes
- `Attachments`: files uploaded by customers or agents

**Key fields:**
- **CreatedBy**: the user who submitted the ticket (agent creating on behalf, or customer self-creating)
- **CreatedFor**: conceptually, who the ticket concerns (the customer); may be the same as CreatedBy or different. Currently tracked conceptually; database has only `CreatedByUserId`.
- **AssignedTo**: the agent responsible for responding; can be null (unassigned)

---

## Ticket Lifecycle

```
New
  ↓ (agent picks up / is assigned, or self-assigns)
InProgress
  ↓ (agent provides first response)
WaitingForCustomer
  ↓ (customer replies)
WaitingForSupport
  ↓ (back to agent for further action)
InProgress (or back to WaitingForCustomer)
  ↓ (agent resolves)
Resolved (waiting for customer confirmation)
  ↓ (customer confirms OK, or no response after timeout, or admin/agent closes, or customer closes)
Closed (terminal, cannot reopen)
```

**State transition rules:**
- **New → InProgress**: agent picks up or is assigned
- **InProgress → WaitingForCustomer**: agent responds
- **WaitingForCustomer → WaitingForSupport**: customer replies
- **WaitingForSupport → InProgress**: agent responds again
- **InProgress → Resolved**: agent marks as resolved
- **Resolved → Closed**: customer confirms, or timeout, or agent/admin closes, or customer closes
- **Closed**: terminal (cannot reopen once closed)

**Who can close a ticket:** Customer (own tickets only), Support Agent (in assigned project), Admin (any ticket).

---

## Priorities & SLAs

**Priority levels:** High, Medium, Low

**SLA fields:**
- `FirstResponseDueAt`: deadline for first agent response
- `ResolutionDueAt`: deadline for resolution

**Calculation:**
- SLAs are **calculated at ticket creation** based on Priority
- SLAs are **immutable from the UI** (cannot override via application); only changeable in database
- Clock starts regardless of who created the ticket (CreatedBy role doesn't matter)

**Enforcement:** Domain events fire when SLA breaches occur; application handles alerts/escalations.

---

## Messages & Communication

### TicketMessage

A reply or note on a ticket.

**Properties:**
- `TicketId`: parent ticket
- `AuthorUserId`: who wrote it
- `Content`: message body
- `IsInternal`: boolean visibility flag
- `CreatedAt`
- `Attachments` (optional)

**Visibility:**
- **Public** (`IsInternal = false`): visible to all ticket participants (customer, assigned agent, other agents in project, admins)
- **Internal** (`IsInternal = true`): visible only to admins and support agents in the assigned project (not to the customer)

**Immutability:** Messages cannot be edited after creation; only visible in history.

**Domain event:** `TicketMessageAddedDomainEvent` includes `IsInternal` flag so notifications can route correctly (internal messages don't notify customers).

**Deletion:** Orphaned user messages (from deleted users) stay in the audit trail; author is nulled. Cannot delete messages.

---

## Ticket Categories

Organizational groupings for tickets within a project (e.g., "Billing", "Technical", "Feature Request").

**Properties:**
- `Name`: display name
- `ProjectId`: parent project
- `Status`: Active or Archived

**Behavior:**
- Can be changed on a ticket after creation (e.g., support agent corrects misclassified category)
- Cannot delete a category if tickets are attached (constraint prevents it)

---

## Wiki Pages

Documentation, FAQs, and knowledge base articles.

**Scope:**
- **Global wiki**: visible to all users across all organizations (e.g., "How to clear browser cache")
- **Project-specific wiki**: visible within a specific project

**Visibility:**
- **Public**: visible to all users with access to the organization/project (customers included)
- **Internal**: visible only to support agents and admins in that project

**Use cases:**
- Self-service knowledge base for customers
- Internal procedures and runbooks for agents
- Solutions and resolutions posted after tickets are resolved

---

## Attachments

Files uploaded to tickets.

**Constraints:**
- **Size limit:** 10 MB (suggested; not yet enforced in UI)
- **File types:** Document types only (e.g., PDF, Word, images); executables and scripts blocked
- **Uploader:** both customers and support agents can attach files
- **Visibility:** visible to all ticket participants (customer, assigned agent, other project agents, admins)

---

## User Assignment & Project Membership

### ProjectAssignment

Connects a User to a Project with a specific Role.

**Properties:**
- `UserId`, `ProjectId`
- `Role`: string role name (e.g., "Agent", "Observer", expandable for future)

**Constraint:** A user can be assigned to a project at most once (unique index on `ProjectId` + `UserId`).

**Deletion:** When a user is deleted, their project assignments cascade-delete. When a project is deleted, all assignments to it are deleted.

---

## Archival & Deletion

### Organizations & Projects

- **Archive** (soft-delete):
  - Organization/project stays visible in history
  - Cannot be modified (read-only)
  - Can be reactivated (unarchive)
  - Unassigns all project members (they need reassignment if reactivated)
  
- **Delete** (hard-delete):
  - Permanent removal
  - Cannot be undone

**Cascade effects:**
- Deleting an org should cascade to its projects (if implemented)
- Deleting a project unassigns all members

### Tickets

- **Stays visible** after parent project/org is archived or deleted
- **Cannot be modified** if parent is archived
- **Cannot be deleted** if it has a CreatedBy user (database constraint)

### Categories

- Cannot be deleted if tickets reference it (constraint)
- Can be archived (soft-delete)

---

## Access Control Summary

| Action | Admin | Agent (in project) | Customer |
|--------|-------|-------------------|----------|
| Create ticket | ✓ | ✓ | ✓ (own) |
| View ticket | all | assigned project | own |
| Respond to ticket | ✓ (any) | ✓ (assigned project) | ✓ (own) |
| Assign ticket | ✓ | ✓ (in same project) | ✗ |
| Close ticket | ✓ (any) | ✓ (assigned project) | ✓ (own) |
| Create category | ✓ | ✗ | ✗ |
| Change category | ✓ | ✓ (assigned project) | ✗ |
| Write public message | ✓ | ✓ | ✓ |
| Write internal note | ✓ | ✓ (in project) | ✗ |
| See internal notes | ✓ | ✓ (in project) | ✗ |
| View public wiki | ✓ | ✓ | ✓ |
| View internal wiki | ✓ | ✓ (in project) | ✗ |
| Manage org/projects/users | ✓ | ✗ | ✗ |

---

## Key Design Decisions

1. **Customers cannot reopen closed tickets:** Once closed, a ticket is terminal.

2. **Multi-role users:** A user can be both an agent in one context and a customer in another (e.g., internal employees supporting external customers).

3. **Admin is exclusive:** An admin cannot also be an agent or customer.

4. **SLAs are immutable from UI:** Priority-based SLAs calculated at creation; no override button (database-only adjustment).

5. **Internal notes are project-scoped:** Only admins and agents in that project see them.

6. **Users cannot be deleted if they created tickets:** Maintains audit trail integrity.

7. **Tickets stay visible after archival:** Archived projects don't hide their tickets; just prevent modification.

8. **Messages are immutable:** Cannot edit messages; only view history.

---

## Future Considerations (not v1)

- **CreatedFor field:** Add explicit tracking of which customer a ticket was created for (currently implicit)
- **Linked tickets:** Support for marking duplicates, blockers, related issues
- **Time tracking:** Hours spent on tickets
- **Custom fields:** Project-specific metadata
- **Ticket templates:** Pre-fill creation form
- **Bulk actions:** Batch close, reassign, etc.
