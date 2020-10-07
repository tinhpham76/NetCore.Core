using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace NetCore.Core.MongoDb
{
    [BsonIgnoreExtraElements]
    public class BasePaginationEntity<TEntity, TId> where TEntity : BaseEntity<TId>
    {
        public long total { get; set; }
        public List<TEntity> datas { get; set; }
    }
}