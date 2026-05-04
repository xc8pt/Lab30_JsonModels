using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using HeroesApi.Models;
using HeroesApi.Data;
using Microsoft.AspNetCore.SignalR;

namespace HeroesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HeroesController : ControllerBase {
    // Метод GetAll() — получение всех героев
    // Метод GetAll() — получение всех героев с возможной фильтрацией по вселенной
    [HttpGet]
    public ActionResult<List<Hero>> GetAll([FromQuery] string? universe = null) {
        var heroes = HeroesStore.Heroes;

        if (!string.IsNullOrEmpty(universe)) {
            if (Enum.TryParse<Universe>(universe, ignoreCase: true, out var universeEnum)) {
                heroes = heroes.Where(h => h.Universe == universeEnum).ToList();
            }
            else {
                heroes = new List<Hero>();
            }
        }
        return Ok(heroes);
    }

    // GET /api/heroes/search?name=...
    [HttpGet("search")]
    public ActionResult<List<Hero>> Search([FromQuery] string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            // Можно вернуть пустой список или BadRequest — по заданию подходит пустой список
            return Ok(new List<Hero>());
        }

        var result = HeroesStore.Heroes
            .Where(h => h.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Ok(result);
    }

    // Метод GetById() — получение героя по ID
    [HttpGet("{id}")]
    public ActionResult<Hero> GetById(int id) {
        var hero = HeroesStore.Heroes.FirstOrDefault(h => h.Id == id);
        if (hero is null) {
            return NotFound(new { message = $"Герой с id = {id} не найден" });
        }
        return Ok(hero);
    }

    // Метод GetDemo() — сравнение настроек сериализации
    [HttpGet("demo")]
    public ActionResult GetDemo() {
        var hero = HeroesStore.Heroes.First();
        var defaultOptions = new JsonSerializerOptions {
            WriteIndented = true
        };
        var ourOptions = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
        return Ok(new {
            withDefaultSettings = JsonSerializer.Deserialize<object>(
                JsonSerializer.Serialize(hero, defaultOptions), defaultOptions),
            withOurSettings = JsonSerializer.Deserialize<object>(
                JsonSerializer.Serialize(hero, ourOptions), ourOptions),
            note = "Сравните имена полей и значение universe в двух"
        });
    }

    // 
    [HttpGet("serialize")]
    public ActionResult GetSerialize() {
        var options = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
        var hero = new Hero {
            Id = 99,
            Name = "Тестовый герой",
            RealName = "Student",
            Universe = Universe.Marvel,
            PowerLevel = 50,
            Powers = new() { "программирование", "дебаггинг" },
            Weapon = new() { Name = "Клавиатура", IsRanger = false },
            InternalNotes = "Это поле не попадёт в JSON"
        };
        string serialized = JsonSerializer.Serialize(hero, options);
        var deserialized = JsonSerializer.Deserialize<Hero>(serialized, options);
        return Ok(new {
            serializedJson = serialized,
            deserializedObject = deserialized,
            internalNotesAfterDeserialize = deserialized?.InternalNotes ?? "null - поле было проигнорировано"
        });
    }
}