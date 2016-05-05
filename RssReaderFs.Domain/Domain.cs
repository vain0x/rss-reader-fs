using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RssReaderFs
{
    public interface IEntity
    {
    }

    public class EntityWithId : IEntity
    {
        [Key]
        public long Id { get; set; }
    }

    public class Article : EntityWithId
    {
        [Required]
        public string Title { get; set; }
        [Required, Index]
        public string Url { get; set; }

        public string Desc { get; set; }

        public string Link { get; set; }

        [Required, Index]
        public DateTime Date { get; set; }
    }

    public class ReadLog : IEntity
    {
        [Key]
        public long ArticleId { get; set; }

        public DateTime Date { get; set; }
    }

    public class TwitterUser : IEntity
    {
        [Key]
        public string ScreenName { get; set; }
        
        public DateTime ReadDate { get; set; }
    }

    public class RssFeed : EntityWithId
    {
        [Required, Index(IsUnique = true)]
        public string Name { get; set; }
        [Required, Index(IsUnique = true)]
        public string Url { get; set; }
    }

    public class Tag : EntityWithId
    {
        [Required, Index]
        public string TagName { get; set; }
        [Required, Index]
        public string SourceName { get; set; }
    }

    public class Config : IEntity
    {
        [Key]
        public string Name { get; set; }

        public string BearToken { get; set; }
    }
}
