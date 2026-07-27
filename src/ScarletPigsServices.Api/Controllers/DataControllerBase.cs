using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScarletPigsServices.Data;

namespace ScarletPigsServices.Api.Controllers;

[ApiController]
public abstract class DataControllerBase<TEntity, TKey>(
    ScarletPigsDbContext db,
    IOptions<JsonOptions> jsonOptions) : ControllerBase
    where TEntity : class
    where TKey : notnull
{
    private static readonly IReadOnlyDictionary<string, PropertyInfo> WritableProperties =
        typeof(TEntity)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanWrite)
            .ToDictionary(
                property => property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                    ?? JsonNamingPolicy.SnakeCaseLower.ConvertName(property.Name),
                StringComparer.OrdinalIgnoreCase);

    protected ScarletPigsDbContext Db { get; } = db;
    protected DbSet<TEntity> Entities => Db.Set<TEntity>();
    protected JsonSerializerOptions SerializerOptions { get; } = jsonOptions.Value.JsonSerializerOptions;
    protected abstract IReadOnlySet<string> KeyPropertyNames { get; }

    protected abstract bool TryParseKey(string value, out TKey key);
    protected abstract TKey GetKey(TEntity entity);
    protected virtual string FormatKey(TKey key) => key.ToString()!;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public virtual async Task<ActionResult<IReadOnlyList<TEntity>>> GetAll(
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (offset < 0 || limit is < 1 or > 1000)
        {
            return BadRequest("offset must be non-negative and limit must be between 1 and 1000.");
        }

        return Ok(await Entities
            .AsNoTracking()
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public virtual async Task<ActionResult<TEntity>> GetById(string id, CancellationToken cancellationToken)
    {
        if (!TryParseKey(id, out var key))
        {
            return BadRequest("The resource id is invalid.");
        }

        var entity = await Entities.FindAsync([key], cancellationToken);
        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public virtual async Task<ActionResult<TEntity>> Create(TEntity entity, CancellationToken cancellationToken)
    {
        Entities.Add(entity);

        try
        {
            await Db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "The resource could not be created.",
                Detail = exception.GetBaseException().Message,
                Status = StatusCodes.Status409Conflict
            });
        }

        return CreatedAtAction(nameof(GetById), new { id = FormatKey(GetKey(entity)) }, entity);
    }

    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public virtual async Task<ActionResult<TEntity>> Patch(
        string id,
        [FromBody] JsonElement changes,
        CancellationToken cancellationToken)
    {
        if (!TryParseKey(id, out var key))
        {
            return BadRequest("The resource id is invalid.");
        }

        if (changes.ValueKind != JsonValueKind.Object)
        {
            return BadRequest("The patch body must be a JSON object.");
        }

        var entity = await Entities.FindAsync([key], cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        foreach (var change in changes.EnumerateObject())
        {
            if (KeyPropertyNames.Contains(change.Name))
            {
                return BadRequest($"The key property '{change.Name}' cannot be changed.");
            }

            if (!WritableProperties.TryGetValue(change.Name, out var property))
            {
                return BadRequest($"The property '{change.Name}' does not exist or cannot be changed.");
            }

            try
            {
                property.SetValue(entity, change.Value.Deserialize(property.PropertyType, SerializerOptions));
            }
            catch (Exception exception) when (exception is JsonException or InvalidOperationException or ArgumentException)
            {
                return BadRequest($"The value supplied for '{change.Name}' is invalid.");
            }
        }

        if (!TryValidateModel(entity))
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            await Db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "The resource could not be updated.",
                Detail = exception.GetBaseException().Message,
                Status = StatusCodes.Status409Conflict
            });
        }

        return Ok(entity);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public virtual async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        if (!TryParseKey(id, out var key))
        {
            return BadRequest("The resource id is invalid.");
        }

        var entity = await Entities.FindAsync([key], cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        Entities.Remove(entity);

        try
        {
            await Db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "The resource could not be deleted.",
                Detail = exception.GetBaseException().Message,
                Status = StatusCodes.Status409Conflict
            });
        }

        return NoContent();
    }
}

public abstract class GuidDataController<TEntity>(
    ScarletPigsDbContext db,
    IOptions<JsonOptions> jsonOptions,
    Func<TEntity, Guid> keySelector) : DataControllerBase<TEntity, Guid>(db, jsonOptions)
    where TEntity : class
{
    private static readonly IReadOnlySet<string> KeyNames =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "id" };

    protected override IReadOnlySet<string> KeyPropertyNames => KeyNames;
    protected override Guid GetKey(TEntity entity) => keySelector(entity);
    protected override bool TryParseKey(string value, out Guid key) => Guid.TryParse(value, out key);
}

public abstract class StringDataController<TEntity>(
    ScarletPigsDbContext db,
    IOptions<JsonOptions> jsonOptions,
    Func<TEntity, string> keySelector,
    string keyPropertyName) : DataControllerBase<TEntity, string>(db, jsonOptions)
    where TEntity : class
{
    private readonly IReadOnlySet<string> _keyNames =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { keyPropertyName };

    protected override IReadOnlySet<string> KeyPropertyNames => _keyNames;
    protected override string GetKey(TEntity entity) => keySelector(entity);

    protected override bool TryParseKey(string value, out string key)
    {
        key = value.Trim();
        return key.Length > 0;
    }
}
