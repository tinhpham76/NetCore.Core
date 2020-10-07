using System;
using System.Collections.Generic;
using System.Reflection;
using NetCore.Core.MongoDb;
using MongoDB.Driver;

namespace NetCore.Core.MongoDb
{
    public class SequenceRepository : BaseRepository<SequenceEntity, string>
    {
        public SequenceRepository(IMongoDatabase db) : base(db) { }

        public long GetId<TEntity>() where TEntity : class
        {
            return this.getId<TEntity>();
        }

        public List<long> GetId<TEntity>(int size) where TEntity : class
        {
            var maxId = this.getId<TEntity>(size);

            var ids = new List<long>();

            for (var i = maxId - size; i < maxId; i++)
                ids.Add(i + 1);

            return ids;
        }

        private long getId<TEntity>(int num = 1)
        {
            var entityName = this.getEntityName<TEntity>();

            if (entityName == null)
                throw new ArgumentNullException("entityName");

            var result = base.Collection().FindOneAndUpdate(
                Builders<SequenceEntity>.Filter.Eq(a => a._id, entityName),
                Builders<SequenceEntity>.Update.Inc(a => a.sequence, num),
                new FindOneAndUpdateOptions<SequenceEntity, long>()
                {
                    IsUpsert = true,
                    ReturnDocument = ReturnDocument.After,
                    Projection = Builders<SequenceEntity>.Projection.Expression(a => a.sequence)
                });

            return result;
        }

        private string getEntityName<TEntity>()
        {
            var type = typeof(TEntity);

            var customAttr = type.GetTypeInfo().GetCustomAttribute<EntityAttribute>();

            if (customAttr != null)
            {
                if (!string.IsNullOrEmpty(customAttr.Sequence))
                    return customAttr.Sequence;
                else if (!string.IsNullOrEmpty(customAttr.Name))
                    return customAttr.Name;
            }

            return type.Name;
        }
    }
}