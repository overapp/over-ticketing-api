# Piano di Implementazione: Ticket Categories Enhancement

## 1. Visione d'Insieme & Obiettivi

Consentire la categorizzazione dei ticket all'interno della piattaforma di ticketing. Il sistema supporta due livelli di categorie:
1. **Categorie Globali (Generiche)**: visibili e utilizzabili trasversalmente da tutti i progetti (es. *Bug, Feature Request, Domanda Generale*).
2. **Categorie di Progetto**: create specificamente per un singolo progetto e visibili solo nel contesto di quel progetto.

Ogni ticket può avere **al massimo una categoria** associata (opzionale sia alla creazione che a database).

---

## 2. Decisioni di Design Consolidate

| Aspetto | Decisione | Razionale |
|---|---|---|
| **Ambito (Scope)** | 2 Livelli: Globali (`ProjectId == null`) e Progetto (`ProjectId != null`) | Nessuna complessità inutile di ereditarietà ad albero a livello di Organizzazione; i ticket appartengono direttamente ai progetti. |
| **Cardinalità** | 1 a N (`Ticket.CategoryId` nullable) | Chiara classificazione primaria per triage, statistiche e SLA. Eventuali tag multipli rimandati a feature futura. |
| **Obbligatorietà** | Facoltativa (`Guid? CategoryId`) | Massima flessibilità, nessuna migrazione forzata o retrocompatibilità rotta sui ticket preesistenti. |
| **Ciclo di Vita** | Stato (`Active` / `Archived`) | Nessuna perdita di storico sui ticket passati; le categorie archiviate rimangono visibili sui vecchi ticket ma non sono più selezionabili per nuovi ticket. |
| **Governance / Permessi** | Solo ruolo `Admin` può creare/modificare/archiviare categorie | Governance centralizzata per evitare proliferazione disordinata di categorie ridondanti. |
| **Colori UI** | `BackgroundColor` e `ForegroundColor` | Supporto per badge colorati personalizzabili. Default di fallback se omessi (`#64748B` / `#FFFFFF`). Validazione formato colore CSS HEX (`#RGB`, `#RRGGBB`, `#RRGGBBAA`). |
| **Vincolo di Unicità** | Indici univoci distinti per scope: Globali su `Name`, Progetto su `(ProjectId, Name)` | Evita duplicati nella stessa tendina senza impedire a progetti diversi di usare nomi simili se opportuno. |
| **Cancellazione Progetto** | `Cascade Delete` sulle categorie appartenenti a quel progetto | Pulizia automatica delle categorie orfane di progetto quando il progetto viene rimosso. |
| **Cancellazione Categoria vs Ticket** | `DeleteBehavior.Restrict` | Le categorie con ticket collegati non possono essere eliminate fisicamente dal DB, devono essere archiviate. |

---

## 3. Modello di Dominio

### 3.1. Entità `TicketCategory` (`src/Domain/Tickets/TicketCategory.cs`)

```csharp
namespace Domain.Tickets;

public sealed class TicketCategory : Entity
{
    public Guid Id { get; set; }
    public Guid? ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = "#64748B";
    public string ForegroundColor { get; set; } = "#FFFFFF";
    public TicketCategoryStatus Status { get; set; } = TicketCategoryStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigazione
    public Project? Project { get; set; }
}

public enum TicketCategoryStatus
{
    Active = 0,
    Archived = 1
}
```

### 3.2. Aggiornamento `Ticket` (`src/Domain/Tickets/Ticket.cs`)

- Aggiunta proprietà:
  ```csharp
  public Guid? CategoryId { get; set; }
  public TicketCategory? Category { get; set; }
  ```

### 3.3. Domain Events
- `TicketCategoryCreatedDomainEvent(Guid CategoryId)`
- `TicketCategoryUpdatedDomainEvent(Guid CategoryId)`
- `TicketCategoryArchivedDomainEvent(Guid CategoryId)`
- `TicketCategoryUnarchivedDomainEvent(Guid CategoryId)`
- `TicketCategoryChangedDomainEvent(Guid TicketId, Guid? PreviousCategoryId, Guid? NewCategoryId)`

### 3.4. Catalogo Errori (`src/Domain/Tickets/TicketCategoryErrors.cs`)
- `NotFound(Guid id)`: "Ticket category with ID '{id}' was not found."
- `NameNotUnique(string name)`: "A ticket category with name '{name}' already exists in this scope."
- `InvalidForProject(Guid categoryId, Guid projectId)`: "The specified category is not available for project '{projectId}'."
- `CategoryArchived(Guid categoryId)`: "The ticket category is archived and cannot be assigned to tickets."
- `InvalidColor(string fieldName)`: "The field '{fieldName}' must be a valid CSS hex color code (e.g. #FFFFFF or #FFF)."

---

## 4. API Endpoints & Contratti

### 4.1. Categorie

| Metodo | Route | Permesso / Auth | Descrizione |
|---|---|---|---|
| `GET` | `/api/projects/{projectId}/categories` | `Tickets.Read` (o membro progetto) | Ritorna categorie attive globali + attive del progetto per la selezione |
| `POST` | `/api/projects/{projectId}/categories` | `Categories.Manage` (Admin) | Crea una nuova categoria specifica per il progetto |
| `GET` | `/api/categories` | `Categories.Manage` (Admin) | Ritorna tutte le categorie globali (supporta `?includeArchived=bool`) |
| `POST` | `/api/categories` | `Categories.Manage` (Admin) | Crea una nuova categoria globale |
| `PUT` | `/api/categories/{id}` | `Categories.Manage` (Admin) | Aggiorna nome, descrizione, colori di una categoria |
| `PUT` | `/api/categories/{id}/archive` | `Categories.Manage` (Admin) | Archivia la categoria |
| `PUT` | `/api/categories/{id}/unarchive` | `Categories.Manage` (Admin) | Riattiva la categoria archiviata |

### 4.2. Ticket Integration

| Metodo | Route | Modifica |
|---|---|---|
| `POST` | `/api/tickets` | Aggiunto `Guid? CategoryId` al body di creazione ticket |
| `GET` | `/api/tickets` | Aggiunto query parameter `Guid? categoryId` per filtrare la lista |
| `PATCH` | `/api/tickets/{id}/category` | Endpoint dedicato per riassegnare/rimuovere categoria (Support / Admin) |
| `GET` | `/api/tickets/{id}` | Risposta include oggetto DTO annidato `Category` (o `null`) |
| `GET` | `/api/tickets` | Ogni elemento di `TicketSummaryResponse` include DTO `Category` (o `null`) |

### 4.3. DTO Contract `TicketCategoryResponse`

```csharp
public sealed record TicketCategoryResponse(
    Guid Id,
    Guid? ProjectId,
    string Name,
    string Description,
    string BackgroundColor,
    string ForegroundColor,
    TicketCategoryStatus Status);
```

---

## 5. Requisiti Non Funzionali & Sicurezza

1. **Autorizzazione & RBAC**:
   - Nuova costante di autorizzazione: `Permissions.Categories.Manage = "categories:manage"`.
   - Assegnato al ruolo `Admin` tramite il meccanismo di permessi Identity già presente in `RoleNames`.
2. **Validazione FluentValidation**:
   - Regex colore esadecimale: `^#([A-Fa-f0-9]{3}|[A-Fa-f0-9]{6}|[A-Fa-f0-9]{8})$`.
   - `Name`: obbligatorio, max 100 caratteri.
   - `Description`: opzionale, max 500 caratteri.
3. **Integrità Transazionale & Performance**:
   - Query `GetTicketsQuery` e `GetTicketByIdQuery` devono includere `.Include(t => t.Category)` / proiezione `.Select()` diretta per evitare problemi N+1.
   - Indice DB: `CREATE INDEX IX_Tickets_CategoryId ON Tickets(CategoryId);`
   - Indici univoci condizionali su PostgreSQL:
     - Globali: `CREATE UNIQUE INDEX IX_TicketCategories_Name_Global ON TicketCategories(Name) WHERE ProjectId IS NULL;`
     - Progetto: `CREATE UNIQUE INDEX IX_TicketCategories_ProjectId_Name ON TicketCategories(ProjectId, Name) WHERE ProjectId IS NOT NULL;`

---

## 6. Casi Limite ed Error Conditions

1. **Assegnazione di Categoria di Altro Progetto**:
   - Se l'utente tenta di associare al Ticket del Progetto A una categoria del Progetto B, il comando fallisce con `Result.Failure(TicketCategoryErrors.InvalidForProject)`.
2. **Assegnazione Categoria Archiviata**:
   - Se la categoria richiesta esiste ma `Status == Archived`, la creazione o aggiornamento fallisce con `Result.Failure(TicketCategoryErrors.CategoryArchived)`.
3. **Cancellazione Progetto con Categorie e Ticket**:
   - EF Core config: `Project` -> `Categories` con `Cascade Delete`.
   - `Ticket` -> `Category` con `Restrict` (la cancellazione del progetto elimina prima i ticket a cascata, poi le categorie).
4. **Ticket con Categoria Archiviata Successivamente**:
   - I vecchi ticket continuano a mostrare la categoria archiviata nei dettagli storici (il DTO include `Status`). Non compare più nelle opzioni di selezione dei nuovi ticket.

---

## 7. Fuori Scopo (Out of Scope)

- Assegnazione di categorie multiple (tag / multi-label).
- Categorie ereditate a livello di Organizzazione (solo Globali + Progetto).
- SLA personalizzati calcolati dinamicamente in base alla categoria (la logica SLA attuale rimane invariata basata su priorità).

---

## 8. Piano di Rollout delle Fasi di Implementazione

- [x] **Fase 1: Domain & Database**:
  - Creazione entità `TicketCategory`, enum `TicketCategoryStatus`, eventi di dominio ed errori.
  - Aggiornamento di `Ticket` con `CategoryId` e navigation property.
  - Configurazione EF Core (`TicketCategoryConfiguration`, `TicketConfiguration`) e aggiunta a `IApplicationDbContext`.
  - Generazione migrazione EF Core e script SQL.
- [x] **Fase 2: Application Slices (Categories CRUD)**:
  - Slice per Creazione Categoria Globale e Categoria di Progetto.
  - Slice per Lista Categorie (Globali Admin e combinata Progetto).
  - Slice per Aggiornamento e Archiviazione/Riattivazione Categoria.
- [x] **Fase 3: Application Slices (Tickets Integration)**:
  - Integrazione `CategoryId` in `CreateTicketCommand` con validatore e business rule check.
  - Creazione `UpdateTicketCategoryCommand` per modifica/rimozione categoria su ticket esistente.
  - Aggiornamento `GetTicketsQuery` (filtro `categoryId` e DTO `Category`) e `GetTicketByIdQuery`.
- [x] **Fase 4: Web.Api Endpoints**:
  - Implementazione endpoint REST minimi in `src/Web.Api/Endpoints/Categories/` e `Tickets/`.
  - Configurazione autorizzazioni e tag Swagger/OpenAPI.
- [x] **Fase 5: Testing Completo**:
  - Unit tests su validatori di colore e comandi di creazione.
  - Integration tests per verificare scope globale/progetto, autorizzazioni e permessi.
  - Unit tests su validatori di colore e comandi di creazione.
  - Integration tests per verificare scope globale/progetto, unicità nomi e regole di non-misappropriazione tra progetti.
