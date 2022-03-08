using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/*
* @Author: xjm
* @Description:
* @Date: DATE TIME
* @Email: 326308290@qq.com
*/
namespace ShardingWaterfallApp
{
    [Table(nameof(Article))]
    public class Article
    {
        [MaxLength(128)]
        [Key]
        public string Id { get; set; }
        [MaxLength(128)]
        [Required]
        public string Title { get; set; }
        [MaxLength(256)]
        [Required]
        public string Content { get; set; }
        
        public DateTime PublishTime { get; set; }
    }
}