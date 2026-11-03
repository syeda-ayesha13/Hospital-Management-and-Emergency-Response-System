# HMERS: Core & Identity Module — API Contract v2 (for FINAL REVIEW)

Owner: Member 1 (Core & Identity) · Branch: `feature/core-identity` → PR into `main`
Status: **v2 incorporates the 8 required changes. No implementation has started.**
Everything lives in the existing `ERS` project (and `ERS.Core/Models` for shared types). No new project, no duplicate tables.

---

## 0. How each required change is addressed

| # | Required change | Resolution | Where |
|---|---|---|---|
| 1 | Nullable patient details + `is_Temporary` | `FName, LName, Gender, Date_Of_Birth, contact_No, pt_Address` → `NULL`; new `is_Temporary BIT NOT NULL DEFAULT 0` | `database/schema.sql`, `migrations/001_core_identity.sql` |
| 2 | CHECK for complete permanent patients | `CK_Patients_PermanentComplete`: `is_Temporary = 1 OR (all six fields NOT NULL)`; `Gender IN ('M','F')` still enforced | same |
| 3 | Keep imported IDs, sequence above max | `dbo.seq_Patient_Id` as column default (explicit IDs still allowed). Dataset IDs are **100–1599**, so the sequence starts at **1600**. `dbo.usp_ResyncPatientSequence` re-aligns it; the loader and the migration both run it | same + `import/load_data.py` |
| 4 | One official schema at `database/schema.sql` | Done for the schema we built. **Pending:** comparison with any `.sql` already in the repo (see §8) | §8 |
| 5 | Separate creation / migrations / import | `schema.sql` (empty DB only, refuses to run otherwise) · `migrations/` (+ `SchemaMigration` table) · `import/load_data.py` · `dev/reset.sql` (dev only) | `database/README.md` |
| 6 | Staff ↔ user links | `AppUser.doct_Id`, `AppUser.nurse_Id` (FKs) + `CK_AppUser_StaffLink` (Doctor ⇒ doctor link only, Nurse ⇒ nurse link only, others ⇒ none) + unique filtered indexes (one login per doctor / nurse) | schema + migration |
| 7 | Role-based access to sensitive patient data | Four response views, see §3.1 | §3.1 |
| 8 | `CreateTemporaryAsync` in-process only | No HTTP route for it; service method only | §3.3, §6 |

Constraint logic was tested (14 cases: complete / temporary / incomplete patients, all staff-link combinations, duplicate logins). **The T-SQL itself has not been run on SQL Server by the author of these files; it must be executed on a scratch database before approval is final.**

---

## 1. Conventions

- Base path `/api`, JSON, camelCase. Errors: RFC 7807 `ProblemDetails`; validation errors `400` with field → messages.
- Secured routes need `Authorization: Bearer <JWT>`. Roles: `Admin`, `Doctor`, `Nurse`, `Receptionist`, `EmergencyCoordinator`.
- Passwords / hashes never leave the service (not in responses, not in logs). Contact numbers and addresses are never logged.
- All date-times are hospital local time (no UTC conversion), like the dataset.
- No hard deletes in this module.
- Status codes: `200`, `201`, `400` validation, `401` no/invalid token or bad login, `403` wrong role, `404`, `409` duplicate/overlap.
- Services throw `NotFoundException`, `ConflictException`, `ValidationException`; one middleware in `Middleware/` maps them to ProblemDetails (reuse an existing one if the repo already has it).

## 2. Authentication and users

### POST `/api/auth/login` (public) → `200 LoginResponseDto`
`400` invalid fields · `401` wrong username **or** password (same message for both).
JWT claims: `userId`, `username`, `role`, plus `doctorId` / `nurseId` when linked.

### Users (Admin only)
| Route | Success | Errors |
|---|---|---|
| POST `/api/users` | `201 UserResponseDto` | 400, 401, 403, 404 (staff id not found), 409 (username taken, or staff member already has a login) |
| GET `/api/users` | `200 UserResponseDto[]` | 401, 403 |

Rules: role `Doctor` ⇒ `doctorId` required and `nurseId` absent; role `Nurse` ⇒ `nurseId` required and `doctorId` absent; any other role ⇒ neither. Password ≥ 8 chars with a letter and a digit, hashed with `PasswordHasher<T>`. First Admin comes from a startup seed whose credentials come from configuration (never committed).

## 3. Patients

| Route | Roles | Success | Errors |
|---|---|---|---|
| GET `/api/patients?search=&page=1&pageSize=20` | all five roles | `200 PagedResult<PatientResponseDto>` | 401 |
| GET `/api/patients/{id}` | all five roles | `200 PatientResponseDto` | 401, 404 |
| POST `/api/patients` | Admin, Receptionist | `201` + `Location` | 400, 401, 403, 409 |
| PUT `/api/patients/{id}` | Admin, Receptionist | `200` | 400, 401, 403, 404 |

`pageSize` ≤ 100. PUT is a full update and **always requires a complete record**. If the patient was temporary, a successful PUT sets `is_Temporary = 0`. Clients can never set `isTemporary`; there is no way to turn a permanent patient back into a temporary one. `409` on POST if a patient with the same first name, last name, date of birth and contact number exists.

### 3.1 Role-based views of patient data

| View | Roles | Fields returned |
|---|---|---|
| **Full** | Admin, Receptionist, Doctor | all |
| **Clinical** | Nurse | no `contactNo`, no `address` |
| **Coordination** | EmergencyCoordinator | no `address`, no `dateOfBirth` (age is shown); `contactNo` included |
| **Basic** | no authenticated user (in-process public emergency flow) | only `patientId`, `isTemporary` |

- Fields a viewer may not see are **omitted** from the JSON (not blanked).
- A field omitted because it is unknown (temporary patient) looks the same as one omitted by role; clients must not treat omission as meaning either.
- `search` matches names for everyone; it matches `contactNo` only for roles that may see `contactNo` (otherwise search would leak numbers).
- Filtering happens inside `PatientService`, using an injected `ICurrentUser`. This keeps the approved signatures (`GetByIdAsync(id)`) unchanged and means other modules automatically get the view that matches the logged-in user. With no user, the least-privilege Basic view applies.

### 3.2 Validation (same for create and update)
| Field | Rule |
|---|---|
| firstName, lastName | required, 1–50, letters, spaces, `'` and `-` only |
| gender | required, `M` or `F` |
| dateOfBirth | required, not in the future, not more than 120 years ago |
| contactNo | required, 7–20 chars, `^\+?[0-9][0-9 \-]{6,19}$` |
| address | required, 1–200 |

Checked against the imported data: all 1,500 patients and all staff phone numbers already satisfy these rules, so existing records can be edited without being rejected.

### 3.3 Temporary patients (in-process only)
`IPatientService.CreateTemporaryAsync(CreateTemporaryPatientDto)` returns the new `patientId`. **No HTTP endpoint exists for it**, and the caller needs no login (the Emergency module's public endpoint calls it).
- All four fields optional. Blank strings are stored as `NULL`. Nothing is invented: no "Unknown", no placeholder dates.
- Fields that *are* provided follow §3.2.
- Sets `is_Temporary = 1`; the ID comes from `seq_Patient_Id`.

## 4. Staff and departments

| Route | Roles | Query | Success | Errors |
|---|---|---|---|---|
| GET `/api/staff/doctors` | all five | `departmentId?`, `search?` | `200 DoctorResponseDto[]` | 401, 404 (unknown department) |
| GET `/api/staff/nurses` | all five | same | `200 NurseResponseDto[]` | same |
| GET `/api/staff/helpers` | all five | same | `200 HelperResponseDto[]` | same |
| GET `/api/departments` | all five | none | `200 DepartmentResponseDto[]` | 401 |

Proposed additions (need approval): `GET /api/staff/{doctors|nurses|helpers}/{id}` and `GET /api/departments/{id}` (both `200`/`404`).
**Proposal:** staff `contactNo` is returned only to Admin and EmergencyCoordinator (omitted for others). Staff creation/editing is not in the contract and is not planned.

## 5. Staff shifts

| Route | Roles | Success | Errors |
|---|---|---|---|
| GET `/api/staff/shifts?date=&staffType=&staffId=` | Admin, Doctor, Nurse | `200 ShiftResponseDto[]` | 400, 401, 403 |
| POST `/api/staff/shifts` | Admin | `201 ShiftResponseDto` | 400, 401, 403, 404 (staff not found), 409 (overlap) |

**Overnight shifts are normal in the data** (630 of 2,058 shifts, e.g. 20:00–08:00). Rule: a shift runs from `shiftDate + shiftStart` to `shiftDate + shiftEnd`, and **ends the next day when `shiftEnd <= shiftStart`**. Validation: exactly one of `doctorId` / `nurseId` / `helperId` (matches the DB CHECK), staff member exists, `shiftEnd != shiftStart`, and no overlap with another shift of the same person, calculated on these full intervals (so a night shift that ends at 08:00 does not clash with a shift starting at 08:00 the next day). The imported data has no overlaps under this rule.

`GetOnDutyAsync(staffType, at, departmentId?)` returns people whose interval contains `at` (`start <= at < end`), **including the overnight shift that began the previous day**.

## 6. Shared service contracts (approved list; `CancellationToken` is an optional trailing parameter, so call sites stay `GetByIdAsync(id)`)

```csharp
namespace ERS.Services;

public interface IPatientService
{
    // shared with other modules (approved)
    Task<PatientResponseDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
    Task<int> CreateTemporaryAsync(CreateTemporaryPatientDto dto, CancellationToken ct = default);

    // used by PatientsController only
    Task<PagedResult<PatientResponseDto>> SearchAsync(PatientQuery query, CancellationToken ct = default);
    Task<PatientResponseDto> CreateAsync(CreatePatientDto dto, CancellationToken ct = default);
    Task<PatientResponseDto?> UpdateAsync(int id, UpdatePatientDto dto, CancellationToken ct = default);
}

public interface IStaffService
{
    // shared with other modules (approved)
    Task<bool> DoctorExistsAsync(int id, CancellationToken ct = default);
    Task<bool> NurseExistsAsync(int id, CancellationToken ct = default);
    Task<bool> HelperExistsAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<OnDutyStaffDto>> GetOnDutyAsync(StaffType staffType, DateTime at,
                                                       int? departmentId = null, CancellationToken ct = default);
    // used by StaffController only
    Task<IReadOnlyList<DoctorResponseDto>> GetDoctorsAsync(StaffQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<NurseResponseDto>>  GetNursesAsync(StaffQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<HelperResponseDto>> GetHelpersAsync(StaffQuery query, CancellationToken ct = default);
}

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentResponseDto>> GetAllAsync(CancellationToken ct = default);   // approved
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);                          // approved
}

// internal to this module
public interface IAuthService  { Task<LoginResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken ct = default); }
public interface IUserService  { Task<UserResponseDto> CreateAsync(CreateUserDto dto, CancellationToken ct = default);
                                 Task<IReadOnlyList<UserResponseDto>> ListAsync(CancellationToken ct = default); }
public interface IShiftService { Task<IReadOnlyList<ShiftResponseDto>> SearchAsync(ShiftQuery q, CancellationToken ct = default);
                                 Task<ShiftResponseDto> CreateAsync(CreateShiftDto dto, CancellationToken ct = default); }
public interface ICurrentUser  { bool IsAuthenticated { get; } int? UserId { get; } string? Role { get; } }
```

Ownership boundary: this module is the **only writer** of `AppUser, Patients, Doctor, Nurse, Helpers, Department, StaffShift`. It never reads or writes `Emergency, Dispatch, Ambulance, Alert, StatusHistory, Appointment, MedicalRecord, BedRecords, RoomRecords, SurgeryRecord, Ward, Bed, Room, Location, Road`. Other modules call the services above instead of querying these tables.

## 7. DTO definitions (`ERS/DTOs`, shared enum in `ERS.Core/Models`)

```csharp
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

// ---- ERS.Core/Models ----
public enum StaffType { Doctor, Nurse, Helper }
public static class Roles {
    public const string Admin = "Admin", Doctor = "Doctor", Nurse = "Nurse",
                        Receptionist = "Receptionist", EmergencyCoordinator = "EmergencyCoordinator";
}

// ---- Common ----
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

// ---- Auth ----
public sealed record LoginRequestDto(
    [property: Required, StringLength(50, MinimumLength = 3)]  string Username,
    [property: Required, StringLength(100, MinimumLength = 8)] string Password);

public sealed record AuthUserDto(int UserId, string Username, string Role, int? DoctorId, int? NurseId);
public sealed record LoginResponseDto(string Token, DateTime ExpiresAt, AuthUserDto User);

// ---- Users (no password or hash anywhere in a response) ----
public sealed record CreateUserDto(
    [property: Required, StringLength(50, MinimumLength = 3)]  string Username,
    [property: Required, StringLength(100, MinimumLength = 8)] string Password,
    [property: Required] string Role,          // one of Roles.*, checked in the service
    int? DoctorId,                             // required iff Role == Doctor
    int? NurseId);                             // required iff Role == Nurse

public sealed record UserResponseDto(int UserId, string Username, string Role,
                                     int? DoctorId, int? NurseId, DateTime CreatedAt);

// ---- Patients ----
public sealed record CreatePatientDto(
    [property: Required, StringLength(50), RegularExpression(@"^[\p{L}](?:[\p{L}]|[ '\-]){0,49}$")] string FirstName,
    [property: Required, StringLength(50), RegularExpression(@"^[\p{L}](?:[\p{L}]|[ '\-]){0,49}$")] string LastName,
    [property: Required, RegularExpression("^[MF]$")]                                              string Gender,
    [property: Required] DateOnly? DateOfBirth,                      // nullable so [Required] can detect a missing value; service: not future, <= 120 years
    [property: Required, RegularExpression(@"^\+?[0-9][0-9 \-]{6,19}$")]                           string ContactNo,
    [property: Required, StringLength(200, MinimumLength = 1)]                                     string Address);

public sealed record UpdatePatientDto(      // full update, same rules as create
    [property: Required, StringLength(50), RegularExpression(@"^[\p{L}](?:[\p{L}]|[ '\-]){0,49}$")] string FirstName,
    [property: Required, StringLength(50), RegularExpression(@"^[\p{L}](?:[\p{L}]|[ '\-]){0,49}$")] string LastName,
    [property: Required, RegularExpression("^[MF]$")]                                              string Gender,
    [property: Required] DateOnly? DateOfBirth,
    [property: Required, RegularExpression(@"^\+?[0-9][0-9 \-]{6,19}$")]                           string ContactNo,
    [property: Required, StringLength(200, MinimumLength = 1)]                                     string Address);

public sealed record CreateTemporaryPatientDto(   // in-process only; every field optional
    [property: StringLength(50), RegularExpression(@"^[\p{L}](?:[\p{L}]|[ '\-]){0,49}$")] string? FirstName,
    [property: StringLength(50), RegularExpression(@"^[\p{L}](?:[\p{L}]|[ '\-]){0,49}$")] string? LastName,
    [property: RegularExpression("^[MF]$")]                                              string? Gender,
    [property: RegularExpression(@"^\+?[0-9][0-9 \-]{6,19}$")]                           string? ContactNo);

public sealed record PatientQuery(string? Search = null,
    [property: Range(1, int.MaxValue)] int Page = 1,
    [property: Range(1, 100)] int PageSize = 20);

// Null members are omitted from JSON. Which ones are null depends on the viewer's role (section 3.1).
public sealed record PatientResponseDto(
    int PatientId,
    bool IsTemporary,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? FirstName,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? LastName,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Gender,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateOnly? DateOfBirth,   // Full, Clinical
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Age,                // Full, Clinical, Coordination
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ContactNo,       // Full, Coordination
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Address);        // Full only

// ---- Staff / departments ----
public sealed record StaffQuery(int? DepartmentId = null, string? Search = null);
public sealed record DepartmentResponseDto(int DepartmentId, string DepartmentName);

public sealed record DoctorResponseDto(int DoctorId, int DepartmentId, string DepartmentName,
    string FirstName, string LastName, string Gender,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ContactNo,   // Admin, EmergencyCoordinator only
    string? SurgeonType, int? OfficeNo);

public sealed record NurseResponseDto(int NurseId, int DepartmentId, string DepartmentName,
    string FirstName, string LastName, string Gender,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ContactNo);

public sealed record HelperResponseDto(int HelperId, int DepartmentId, string DepartmentName,
    string FirstName, string LastName, string Gender,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ContactNo);

public sealed record OnDutyStaffDto(StaffType StaffType, int StaffId, string FirstName, string LastName,
    int DepartmentId, int ShiftId, DateTime ShiftStartsAt, DateTime ShiftEndsAt);

// ---- Shifts ----
public sealed record CreateShiftDto(int? DoctorId, int? NurseId, int? HelperId,   // exactly one (checked in the service)
    [property: Required] DateOnly? ShiftDate,                          // nullable so [Required] can detect a missing value
    [property: Required] TimeOnly? ShiftStart,
    [property: Required] TimeOnly? ShiftEnd);                                      // <= start means "ends next day"

public sealed record ShiftQuery(DateOnly? Date = null, StaffType? StaffType = null, int? StaffId = null);

public sealed record ShiftResponseDto(int ShiftId, StaffType StaffType, int StaffId, string StaffName,
    DateOnly ShiftDate, TimeOnly ShiftStart, TimeOnly ShiftEnd, bool EndsNextDay);
```

## 8. Items still open before implementation starts

0. The DTO code in §7 is a design draft that has **not been compiled** (no .NET SDK was available to the author). It must compile cleanly inside `ERS` before approval is final.

1. **Schema comparison (required change 4).** The repo's existing `.sql` file(s) have not been compared yet. Before this schema becomes the single official `database/schema.sql`, every existing schema version must be diffed against it (table list, columns, types, keys), and any difference resolved with the other members.
2. Run `schema.sql` (fresh) **and** `migrations/001_core_identity.sql` (on an old-schema copy) on SQL Server; both must end with the same objects.
3. Confirm the role-view matrix in §3.1 and the staff-contact proposal in §4.
4. Confirm where JWT settings, DI registration and the exception middleware live in `ERS` once the existing code has been inspected (the repo's existing database-access approach is used for all queries; no new ORM).
5. Confirm the packages: `Microsoft.AspNetCore.Authentication.JwtBearer` and `Microsoft.Extensions.Identity.Core` (`PasswordHasher<T>`, approved).
