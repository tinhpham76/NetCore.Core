using MongoDB.Bson.Serialization.Attributes;

namespace NetCore.Core.MongoDb.Test.Models
{
    [Entity(name: "Books")]
    [BsonIgnoreExtraElements]
    public class BookEntity : BaseMongoNumberEntity
    {
        public string BookName { get; set; }

        public decimal Price { get; set; }

        public string Category { get; set; }

        public string Author { get; set; }
    }
}