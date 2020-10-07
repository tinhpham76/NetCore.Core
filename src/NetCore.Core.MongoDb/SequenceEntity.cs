using MongoDB.Bson.Serialization.Attributes;

namespace NetCore.Core.MongoDb
{
    [Entity(name: "Sequence")]
    [BsonIgnoreExtraElements]
    public class SequenceEntity : BaseMongoStringEntity
    {
        public long sequence { get; set; }
    }
}
