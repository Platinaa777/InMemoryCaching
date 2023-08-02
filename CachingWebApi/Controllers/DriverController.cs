using CachingWebApi.Data;
using CachingWebApi.Models;
using CachingWebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CachingWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DriverController : ControllerBase
{
    private readonly ILogger<DriverController> _logger;
    private readonly ICacheService _cacheService;
    private readonly AppDbContext _context;


    public DriverController(ILogger<DriverController> logger,
            ICacheService cacheService,
            AppDbContext context)
    {
        _logger = logger;
        _cacheService = cacheService;
        _context = context;
    }

    [HttpGet("drivers")]
    public async Task<IActionResult> Get()
    {
        // check cache data
        var cacheData = _cacheService.GetData<IEnumerable<Driver>>("drivers");

        if (cacheData != null && cacheData.Count() > 0)
            return Ok(cacheData);

        cacheData = await _context.Drivers.ToListAsync();
        
        // set expiry time

        var expiryTime = DateTimeOffset.Now.AddSeconds(35);
        _cacheService.SetData<IEnumerable<Driver>>("driver", cacheData, expiryTime);

        return Ok(cacheData);
    }


    [HttpPost("addDrivers")]
    public async Task<IActionResult> Post(Driver value)
    {
        var obj = await _context.Drivers.AddAsync(value);
        
        var expiryTime = DateTimeOffset.Now.AddSeconds(35);
        _cacheService.SetData<Driver>($"driver{value.Id}", obj.Entity, expiryTime);

        await _context.SaveChangesAsync();

        return Ok(obj.Entity);
    }
    
    [HttpDelete("delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var exist = await _context.Drivers.FirstOrDefaultAsync(x => x.Id == id);

        if (exist == null)
            return NotFound();

        _context.Drivers.Remove(exist);
        _cacheService.RemoveData($"driver{id}");

        await _context.SaveChangesAsync();
        return NoContent();
    }
}