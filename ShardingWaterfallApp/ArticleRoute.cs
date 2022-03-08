using System;
using System.Globalization;
using System.Linq.Expressions;
using ShardingCore.Core.EntityMetadatas;
using ShardingCore.Core.VirtualRoutes;
using ShardingCore.Helpers;
using ShardingCore.Sharding.EntityQueryConfigurations;
using ShardingCore.VirtualRoutes.Days;

/*
* @Author: xjm
* @Description:
* @Date: DATE TIME
* @Email: 326308290@qq.com
*/
namespace ShardingWaterfallApp
{
    public class ArticleRoute:AbstractSimpleShardingDayKeyDateTimeVirtualTableRoute<Article>
    {
        public override void Configure(EntityMetadataTableBuilder<Article> builder)
        {
            builder.ShardingProperty(o => o.PublishTime);
            builder.ShardingExtraProperty(o => o.Id);
        }

        public override bool AutoCreateTableByTime()
        {
            return true;
        }

        public override DateTime GetBeginTime()
        {
            return new DateTime(2022, 3, 1);
        }

        public override IEntityQueryConfiguration<Article> CreateEntityQueryConfiguration()
        {
            return new ArticleEntityQueryConfiguration();
        }

        public override Expression<Func<string, bool>> GetExtraRouteFilter(object shardingKey, ShardingOperatorEnum shardingOperator, string shardingPropertyName)
        {
            switch (shardingPropertyName)
            {
                case nameof(Article.Id): return GetArticleIdRouteFilter(shardingKey, shardingOperator);
            }

          return base.GetExtraRouteFilter(shardingKey, shardingOperator, shardingPropertyName);
        }
        /// <summary>
        /// 文章id的路由
        /// </summary>
        /// <param name="shardingKey"></param>
        /// <param name="shardingOperator"></param>
        /// <returns></returns>
        private Expression<Func<string, bool>> GetArticleIdRouteFilter(object shardingKey,
            ShardingOperatorEnum shardingOperator)
        {
            //将分表字段转成订单编号
            var id = shardingKey?.ToString() ?? string.Empty;
            //判断订单编号是否是我们符合的格式
            if (!CheckArticleId(id, out var orderTime))
            {
                //如果格式不一样就直接返回false那么本次查询因为是and链接的所以本次查询不会经过任何路由,可以有效的防止恶意攻击
                return tail => false;
            }

            //当前时间的tail
            var currentTail = TimeFormatToTail(orderTime);
            //因为是按月分表所以获取下个月的时间判断id是否是在临界点创建的
            //var nextMonthFirstDay = ShardingCoreHelper.GetNextMonthFirstDay(DateTime.Now);//这个是错误的
            var nextMonthFirstDay = ShardingCoreHelper.GetNextMonthFirstDay(orderTime);
            if (orderTime.AddSeconds(10) > nextMonthFirstDay)
            {
                var nextTail = TimeFormatToTail(nextMonthFirstDay);
                return DoArticleIdFilter(shardingOperator, orderTime, currentTail, nextTail);
            }
            //因为是按月分表所以获取这个月月初的时间判断id是否是在临界点创建的
            //if (orderTime.AddSeconds(-10) < ShardingCoreHelper.GetCurrentMonthFirstDay(DateTime.Now))//这个是错误的
            if (orderTime.AddSeconds(-10) < ShardingCoreHelper.GetCurrentMonthFirstDay(orderTime))
            {
                //上个月tail
                var previewTail = TimeFormatToTail(orderTime.AddSeconds(-10));

                return DoArticleIdFilter(shardingOperator, orderTime, previewTail, currentTail);
            }

            return DoArticleIdFilter(shardingOperator, orderTime, currentTail, currentTail);

        }

        private Expression<Func<string, bool>> DoArticleIdFilter(ShardingOperatorEnum shardingOperator, DateTime shardingKey, string minTail, string maxTail)
        {
            switch (shardingOperator)
            {
                case ShardingOperatorEnum.GreaterThan:
                case ShardingOperatorEnum.GreaterThanOrEqual:
                    {
                        return tail => String.Compare(tail, minTail, StringComparison.Ordinal) >= 0;
                    }

                case ShardingOperatorEnum.LessThan:
                    {
                        var currentMonth = ShardingCoreHelper.GetCurrentMonthFirstDay(shardingKey);
                        //处于临界值 o=>o.time < [2021-01-01 00:00:00] 尾巴20210101不应该被返回
                        if (currentMonth == shardingKey)
                            return tail => String.Compare(tail, maxTail, StringComparison.Ordinal) < 0;
                        return tail => String.Compare(tail, maxTail, StringComparison.Ordinal) <= 0;
                    }
                case ShardingOperatorEnum.LessThanOrEqual:
                    return tail => String.Compare(tail, maxTail, StringComparison.Ordinal) <= 0;
                case ShardingOperatorEnum.Equal:
                    {
                        var isSame = minTail == maxTail;
                        if (isSame)
                        {
                            return tail => tail == minTail;
                        }
                        else
                        {
                            return tail => tail == minTail || tail == maxTail;
                        }
                    }
                default:
                    {
                        return tail => true;
                    }
            }
        }

        private bool CheckArticleId(string orderNo, out DateTime orderTime)
        {
            //yyyyMMddHHmmss
            if (orderNo.Length == 14)
            {
                if (DateTime.TryParseExact(orderNo, "yyyyMMddHHmmss", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var parseDateTime))
                {
                    orderTime = parseDateTime;
                    return true;
                }
            }

            orderTime = DateTime.MinValue;
            return false;
        }
    }
}