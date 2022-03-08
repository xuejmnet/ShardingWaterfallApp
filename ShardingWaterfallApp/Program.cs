using Microsoft.EntityFrameworkCore;
using ShardingCore;
using ShardingCore.Bootstrapers;
using ShardingCore.TableExists;
using ShardingWaterfallApp;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
ILoggerFactory efLogger = LoggerFactory.Create(builder =>
{
    builder.AddFilter((category, level) => category == DbLoggerCategory.Database.Command.Name && level == LogLevel.Information).AddConsole();
});
builder.Services.AddControllers();
builder.Services.AddShardingDbContext<MyDbContext>()
    .AddEntityConfig(o =>
    {
        o.CreateShardingTableOnStart = true;
        o.EnsureCreatedWithOutShardingTable = true;
        o.AddShardingTableRoute<ArticleRoute>();
    })
    .AddConfig(o =>
    {
        o.ConfigId = "c1";
        o.UseShardingQuery((conStr, b) =>
        {
            b.UseMySql(conStr, new MySqlServerVersion(new Version())).UseLoggerFactory(efLogger);
        });
        o.UseShardingTransaction((conn, b) =>
        {
            b.UseMySql(conn, new MySqlServerVersion(new Version())).UseLoggerFactory(efLogger);
        });
        o.AddDefaultDataSource("ds0", "server=127.0.0.1;port=3306;database=ShardingWaterfallDB;userid=root;password=root;");
        o.ReplaceTableEnsureManager(sp => new MySqlTableEnsureManager<MyDbContext>());
    }).EnsureConfig();

var app = builder.Build();

app.Services.GetRequiredService<IShardingBootstrapper>().Start();
using (var scope = app.Services.CreateScope())
{
    var myDbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();
    if (!myDbContext.Articles.Any())
    {
        List<Article> articles = new List<Article>();
        var beginTime = new DateTime(2022, 3, 1, 1, 1,1);
        for (int i = 0; i < 70; i++)
        {
            var article = new Article();
            article.Id = beginTime.ToString("yyyyMMddHHmmss");
            article.Title = "标题" + i;
            article.Content = "内容" + i;
            article.PublishTime = beginTime;
            articles.Add(article);
            beginTime= beginTime.AddHours(2).AddMinutes(3).AddSeconds(4);
        }
        myDbContext.AddRange(articles);
        myDbContext.SaveChanges();
    }
}
app.MapControllers();

app.Run();