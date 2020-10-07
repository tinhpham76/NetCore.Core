using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NetCore.Core.MongoDb.Utils;

namespace NetCore.Core.MongoDb
{
    public abstract class BaseRepository<TEntity, TId> where TEntity : BaseEntity<TId>
    {
        private readonly IMongoDatabase db;
        private readonly SequenceRepository seq;
        private readonly IMongoCollection<TEntity> collection;
        private readonly IMongoCollection<TEntity> collectionReadSecondary;

        protected IMongoCollection<TEntity> Collection(bool isReadSecondary = false)
        {
            return isReadSecondary ? this.collectionReadSecondary : this.collection;
        }

        internal readonly IBsonSerializerRegistry serializerRegistry;
        internal readonly IBsonSerializer<TEntity> documentSerializer;

        public BaseRepository(IMongoDatabase db, SequenceRepository seq = null)
        {
            if (typeof(TEntity).IsAssignableFrom(typeof(BaseMongoNumberEntity)) && seq == null)
                throw new NullReferenceException("seq");

            this.db = db;
            this.seq = seq;

            var entityName = this.getEntityName();

            this.collection = this.db.GetCollection<TEntity>(entityName);
            this.collectionReadSecondary = this.db.GetCollection<TEntity>(entityName)
                .WithReadPreference(new ReadPreference(ReadPreferenceMode.SecondaryPreferred));

            this.serializerRegistry = BsonSerializer.SerializerRegistry;
            this.documentSerializer = this.serializerRegistry.GetSerializer<TEntity>();
        }

        public Task<TEntity> GetByIdAsync(
            TId id,
            Expression<Func<TEntity, TEntity>> projection = null,
            bool isReadSecondary = false)
        {
            if (id == null)
                throw new ArgumentNullException("id");

            var find = this.Collection(isReadSecondary).Find(Builders<TEntity>.Filter.Eq(a => a._id, id));

            if (projection != null)
                find = find.Project(projection);

            return find.FirstOrDefaultAsync();
        }

        public Task<List<TEntity>> GetByIdAsync(
            IEnumerable<TId> listId,
            Expression<Func<TEntity, TEntity>> projection = null,
            bool isReadSecondary = false)
        {
            if (listId == null)
                throw new ArgumentNullException("id");

            return Task.Run(async () =>
            {
                if (!listId.Any())
                    return new List<TEntity>();

                var result = new List<TEntity>();

                foreach (var batch in listId.SplitToBatchs())
                {
                    var find = this.Collection(isReadSecondary).Find(Builders<TEntity>.Filter.In(a => a._id, batch));

                    if (projection != null)
                        find = find.Project(projection);

                    var data = await find.ToListAsync();

                    result.AddRange(data);
                }

                return result;
            });
        }

        protected Task<List<TEntity>> GetByAsync(
            Expression<Func<TEntity, bool>> filter = null,
            Expression<Func<TEntity, TEntity>> projection = null,
            Expression<Func<TEntity, object>> sort = null, SortBy sortBy = SortBy.Ascending,
            bool isReadSecondary = false)
        {
            if (filter == null)
                filter = a => true;

            var find = this.Collection(isReadSecondary).Find(filter);

            if (projection != null)
                find = find.Project(projection);

            if (sort != null)
                find = sortBy == SortBy.Ascending ? find.SortBy(sort) : find.SortByDescending(sort);

            return find.ToListAsync();
        }

        protected Task<List<TEntity>> GetPagingAsync(
            int page, int pageSize,
            Expression<Func<TEntity, bool>> filter = null,
            Expression<Func<TEntity, TEntity>> projection = null,
            Expression<Func<TEntity, object>> sort = null, SortBy sortBy = SortBy.Ascending,
            FilterDefinition<TEntity> filterDefinition = null,
            bool isReadSecondary = false)
        {
            IFindFluent<TEntity, TEntity> find;

            if (filter != null)
                find = this.Collection(isReadSecondary).Find(filter);
            else if (filterDefinition != null)
                find = this.Collection(isReadSecondary).Find(filterDefinition);
            else
                find = this.Collection(isReadSecondary).Find(a => true);

            find = find
                .Skip((page - 1) * pageSize)
                .Limit(pageSize);

            if (projection != null)
                find = find.Project(projection);

            if (sort != null)
                find = sortBy == SortBy.Ascending ? find.SortBy(sort) : find.SortByDescending(sort);

            return find.ToListAsync();
        }

        protected async Task<BasePaginationEntity<TEntity, TId>> GetPaginationAsync(
            int page, int pageSize,
            FilterDefinition<TEntity> filter = null,
            Expression<Func<TEntity, TEntity>> projection = null,
            Expression<Func<TEntity, object>> sort = null, SortBy sortBy = SortBy.Ascending,
            bool isReadSecondary = false,
            bool allowDiskUse = false)
        {
            if (page < 1)
                page = 1;

            if (pageSize < 1 || pageSize > 200)
                pageSize = 20;

            var pipeline = new List<BsonDocument>();

            if (filter != null)
                pipeline.Add(new BsonDocument { { "$match", filter.Render(this.documentSerializer, this.serializerRegistry) } });

            if (sort != null)
            {
                var sortBuilder = sortBy == SortBy.Ascending
                    ? Builders<TEntity>.Sort.Ascending(sort)
                    : Builders<TEntity>.Sort.Descending(sort);

                pipeline.Add(new BsonDocument { { "$sort", sortBuilder.Render(this.documentSerializer, this.serializerRegistry) } });
            }

            if (projection != null)
            {
                var projectionBuilder = Builders<TEntity>.Projection.Expression(projection);

                pipeline.Add(new BsonDocument { { "$project", projectionBuilder.Render(this.documentSerializer, this.serializerRegistry).Document } });
            }

            pipeline.Add(new BsonDocument
            {
                {
                    "$group", new BsonDocument
                    {
                        { "_id", 0 },
                        { "total", new BsonDocument { { "$sum", 1 } } },
                        { "datas", new BsonDocument { { "$push", "$$ROOT" } } }
                    }
                }
            });

            pipeline.Add(new BsonDocument
            {
                {
                    "$project", new BsonDocument
                    {
                        { "_id", 0 },
                        { "total", 1 },
                        { "datas", new BsonDocument { { "$slice", new BsonArray(new object[] { "$datas", (page - 1) * pageSize, pageSize }) } } }
                    }
                }
            });

            var option = new AggregateOptions() { AllowDiskUse = false };

            if (allowDiskUse)
                option.AllowDiskUse = true;

            var aggregate = this.Collection(isReadSecondary).Aggregate<BsonDocument>(pipeline, option);

            var data = await aggregate.FirstOrDefaultAsync();

            var result = data == null
                ? new BasePaginationEntity<TEntity, TId>() { datas = new List<TEntity>(), total = 0 }
                : BsonSerializer.Deserialize<BasePaginationEntity<TEntity, TId>>(data);

            return result;
        }

        protected Task<long> GetCountAsync(
            Expression<Func<TEntity, bool>> filter = null,
            FilterDefinition<TEntity> filterDefinition = null,
            bool isReadSecondary = false)
        {
            Task<long> count;

            if (filter != null)
                count = this.Collection(isReadSecondary).CountDocumentsAsync(filter);
            else if (filterDefinition != null)
                count = this.Collection(isReadSecondary).CountDocumentsAsync(filterDefinition);
            else
                count = this.Collection(isReadSecondary).CountDocumentsAsync(a => true);

            return count;
        }

        public long GetId()
        {
            return this.GetId(1)[0];
        }
        public List<long> GetId(int size)
        {
            if (this.seq == null)
                throw new NullReferenceException("seq");

            if (size < 0)
                throw new ArgumentOutOfRangeException("size < 1");

            if (size == 0)
                return new List<long>();

            return this.seq.GetId<TEntity>(size);
        }

        public Task AddAsync(TEntity entity)
        {
            this.checkData(entity, true);

            return this.collection.InsertOneAsync(entity);
        }
        public Task AddAsync(IEnumerable<TEntity> listEntity)
        {
            this.checkData(listEntity);

            return this.collection.InsertManyAsync(listEntity);
        }

        public Task UpdateAsync(TEntity entity, long updatedBy, params Expression<Func<TEntity, object>>[] properties)
        {
            var now = DateTime.UtcNow;

            if (entity._id == null)
                return AddAsync(entity);

            this.checkData(entity);

            if (properties == null || properties.Length == 0)
            {
                entity.updated_at = now;
                entity.updated_by = updatedBy;

                return this.collection.ReplaceOneAsync(Builders<TEntity>.Filter.Eq(a => a._id, entity._id), entity);
            }

            UpdateDefinition<TEntity> update = null;

            var type = typeof(TEntity);
            var hasUpdatedAt = false;
            var hasUpdatedBy = false;

            foreach (var prop in properties)
            {
                var propertyName = prop.GetPropertyName();
                
                if (propertyName == "updated_at")
                    hasUpdatedAt = true;
                if (propertyName == "updated_by")
                    hasUpdatedBy = true;

                update = this.invoke(propertyName, type.GetProperty(propertyName).GetValue(entity), update);
            }

            if (!hasUpdatedAt)
            {
                entity.updated_at = now;
                update = this.invoke("updated_at", type.GetProperty("updated_at").GetValue(entity), update);
            }
            if (!hasUpdatedBy)
            {
                entity.updated_by = updatedBy;
                update = this.invoke("updated_by", type.GetProperty("updated_by").GetValue(entity), update);
            }

            return this.collection.UpdateOneAsync(Builders<TEntity>.Filter.Eq(a => a._id, entity._id), update);
        }

        public Task DeleteAsync(TEntity entity, long deletedBy)
        {
            if (entity == null)
                throw new NullReferenceException("entity");

            if (entity._id == null)
                throw new NullReferenceException("entity._id");

            if (deletedBy == 0)
                throw new NullReferenceException("deletedBy");

            var now = DateTime.UtcNow;

            return this.collection.UpdateOneAsync(
                Builders<TEntity>.Filter.Eq(a => a._id, entity._id),
                Builders<TEntity>.Update
                    .Set(a => a.is_deleted, true)
                    .Set(a => a.updated_at, now)
                    .Set(a => a.updated_by, deletedBy));
        }
        public Task DeleteAsync(TId id, long deletedBy)
        {
            if (id == null)
                throw new NullReferenceException("id");

            if (deletedBy == 0)
                throw new NullReferenceException("deletedBy");

            var now = DateTime.UtcNow;

            return this.collection.UpdateOneAsync(
                Builders<TEntity>.Filter.Eq(a => a._id, id),
                Builders<TEntity>.Update
                    .Set(a => a.is_deleted, true)
                    .Set(a => a.updated_at, now)
                    .Set(a => a.updated_by, deletedBy));
        }
        public Task DeleteAsync(IEnumerable<TId> ids, long deletedBy)
        {
            if (ids == null)
                throw new NullReferenceException("ids");

            if (deletedBy == 0)
                throw new NullReferenceException("deletedBy");

            var now = DateTime.UtcNow;

            return this.collection.UpdateManyAsync(
                Builders<TEntity>.Filter.In(a => a._id, ids),
                Builders<TEntity>.Update
                    .Set(a => a.is_deleted, true)
                    .Set(a => a.updated_at, now)
                    .Set(a => a.updated_by, deletedBy));
        }

        public Task HardDeleteAsync(TEntity entity)
        {
            if (entity == null)
                throw new NullReferenceException("entity");

            if (entity._id == null)
                throw new NullReferenceException("entity._id");

            return this.collection.DeleteOneAsync(
                Builders<TEntity>.Filter.Eq(a => a._id, entity._id));
        }
        public Task HardDeleteAsync(TId id)
        {
            if (id == null)
                throw new NullReferenceException("id");

            return this.collection.DeleteOneAsync(
                Builders<TEntity>.Filter.Eq(a => a._id, id));
        }
        public Task HardDeleteAsync(IEnumerable<TId> ids)
        {
            if (ids == null)
                throw new NullReferenceException("ids");

            return this.collection.DeleteManyAsync(
                Builders<TEntity>.Filter.In(a => a._id, ids));
        }

        protected async Task ScrollAsync(
            FilterDefinition<TEntity> filter,
            int pageSize = 500,
            Expression<Func<TEntity, TEntity>> projection = null,
            Func<IEnumerable<TEntity>, Task> funcPerPage = null,
            bool isReadSecondary = false)
        {
            if (filter == null)
                throw new NullReferenceException("filter");

            if (pageSize < 1)
                pageSize = 500;

            var cursor = null as IAsyncCursor<TEntity>;

            try
            {
                var find = this.Collection(isReadSecondary)
                    .Find(filter, new FindOptions() { NoCursorTimeout = true, BatchSize = pageSize });

                if (projection != null)
                    find = find.Project(projection);

                cursor = await find.ToCursorAsync();

                while (await cursor.MoveNextAsync())
                    if (funcPerPage != null)
                        await funcPerPage(cursor.Current);
            }
            catch { }
            finally
            {
                if (cursor != null)
                    cursor.Dispose();
            }
        }

        private UpdateDefinition<TEntity> invoke(
            string propertyName, object value, UpdateDefinition<TEntity> update)
        {
            var type = typeof(object);

            if (value != null)
                type = value.GetType();

            return (UpdateDefinition<TEntity>)GetType()
                .GetMethod("Set", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .MakeGenericMethod(new Type[] { type })
                .Invoke(this, new object[] { propertyName, value, update });
        }
        protected UpdateDefinition<TEntity> Set<T>(
            string propertyName, T obj, UpdateDefinition<TEntity> update)
        {
            if (update == null)
                update = Builders<TEntity>.Update.Set(propertyName, obj);
            else
                update = UpdateDefinitionExtensions.Set(update, propertyName, obj);

            return update;
        }

        private void checkData(TEntity entity, bool isAdding = false)
        {
            if (entity is BaseMongoStringEntity)
            {
                var stringEntity = entity as BaseMongoStringEntity;

                if (stringEntity._id == null)
                    stringEntity._id = NetCore.Core.MongoDb.Utils.TypeExtensions.Guid();
            }
            else if (entity is BaseMongoObjectEntity)
            {

            }
            else if (entity is BaseMongoNumberEntity)
            {
                var numberEntity = entity as BaseMongoNumberEntity;

                if (numberEntity._id == 0)
                    numberEntity._id = this.seq.GetId<TEntity>();
            }
            else
                throw new NotSupportedException();

            entity.updated_at = DateTime.UtcNow;

            if (entity.updated_by == 0)
                throw new InvalidCastException("entity.updated_by == 0");

            if (isAdding)
            {
                entity.created_at = entity.updated_at;

                if (entity.created_by == 0)
                    throw new InvalidCastException("entity.created_by == 0");
            }
        }
        private void checkData(IEnumerable<TEntity> listEntity, bool isAdding = false)
        {
            foreach (var entity in listEntity)
                this.checkData(entity, isAdding);
        }
        protected void CheckData(TEntity entity, bool isAdding = false)
        {
            this.checkData(entity, isAdding);
        }
        protected void CheckData(IEnumerable<TEntity> listEntity, bool isAdding = false)
        {
            this.checkData(listEntity, isAdding);
        }
    
        private string getEntityName()
        {
            var type = typeof(TEntity);

            var customAttr = type.GetTypeInfo().GetCustomAttribute<EntityAttribute>();

            if (!string.IsNullOrEmpty(customAttr?.Name))
                return customAttr.Name;

            return type.Name;
        }
    }
}