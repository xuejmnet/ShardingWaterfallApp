using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShardingCore.Core;
using ShardingCore.Extensions.ShardingQueryableExtensions;
using ShardingCore.Sharding.Abstractions.ParallelExecutors;

namespace ShardingWaterfallApp.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    private readonly MyDbContext _myDbContext;

    public TestController(MyDbContext myDbContext)
    {
        _myDbContext = myDbContext;
    }

    public async Task<IActionResult> Waterfall([FromQuery] string? lastId,[FromQuery]int take)
    {
        Console.WriteLine($"-----------开始查询,lastId:[{lastId}],take:[{take}]-----------");
        var list = await _myDbContext.Articles.WhereIf(o => String.Compare(o.Id, lastId) < 0,!string.IsNullOrWhiteSpace(lastId)).Take(take).OrderByDescending(o => o.PublishTime).ToListAsync();
        return Ok(list);
    }
}