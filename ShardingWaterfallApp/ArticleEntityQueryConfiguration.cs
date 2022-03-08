using System;
using ShardingCore.Sharding.EntityQueryConfigurations;

/*
* @Author: xjm
* @Description:
* @Date: DATE TIME
* @Email: 326308290@qq.com
*/
namespace ShardingWaterfallApp
{
    
    public class TailDayReverseComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            //程序默认使用的是正序也就是按时间正序排序我们需要使用倒序所以直接调用原生的比较器然后乘以负一即可
            return Comparer<string>.Default.Compare(x, y) * -1;
        }
    }
    //当前查询满足的复核条件必须是单个分片对象的查询,可以join普通非分片表
    public class ArticleEntityQueryConfiguration:IEntityQueryConfiguration<Article>
    {
        public void Configure(EntityQueryBuilder<Article> builder)
        {
            //设置默认的框架针对Article的排序顺序,这边设置的是倒序
            builder.ShardingTailComparer(new TailDayReverseComparer());
            ////如下设置和上述是一样的效果让框架真对Article的后缀排序使用倒序
            //builder.ShardingTailComparer(Comparer<string>.Default, false);
            
            //简单解释一下下面这个配置的意思
            //第一个参数表名Article的哪个属性是顺序排序和Tail按天排序是一样的这边使用了PublishTime
            //第二个参数表示对属性PublishTime asc时是否和上述配置的ShardingTailComparer一致,true表示一致,很明显这边是相反的因为默认已经设置了tail排序是倒序
            //第三个参数表示是否是Article属性才可以,这边设置的是名称一样也可以,因为考虑到匿名对象的select
            builder.AddOrder(o => o.PublishTime, false,SeqOrderMatchEnum.Owner);
            //这边为了演示使用的id是简单的时间格式化所以和时间的配置一样
            builder.AddOrder(o => o.Id, false,SeqOrderMatchEnum.Owner);
            //这边设置如果本次查询默认没有带上述配置的order的时候才用何种排序手段
            //第一个参数表示是否和ShardingTailComparer配置的一样,目前配置的是倒序,也就是从最近时间开始查询,如果是false就是从最早的时间开始查询
            //后面配置的是熔断器,也就是复核熔断条件的比如FirstOrDefault只需要满足一个就可以熔断
            builder.AddDefaultSequenceQueryTrip(true, CircuitBreakerMethodNameEnum.Enumerator, CircuitBreakerMethodNameEnum.FirstOrDefault);

            //这边配置的是当使用顺序查询配置的时候默认开启的连接数限制是多少,startup一开始可以设置一个默认是当前cpu的线程数,这边优化到只需要一个线程即可,当然如果跨表那么就是串行执行
            builder.AddConnectionsLimit(1, LimitMethodNameEnum.Enumerator, LimitMethodNameEnum.FirstOrDefault);
        }
    }
}