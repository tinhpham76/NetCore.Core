using System.Runtime.Serialization;
using MongoDB.Bson;

namespace NetCore.Core.MongoDb
{
    [DataContract(IsReference = true)]
    public abstract class BaseEntity<T> : BaseInfoEntity
    {
        [DataMember]
        public T _id { get; set; }
    }
    
    [DataContract(IsReference = true)]
    public class BaseMongoObjectEntity : BaseEntity<ObjectId> { }

    [DataContract(IsReference = true)]
    public class BaseMongoStringEntity : BaseEntity<string> { }

    [DataContract(IsReference = true)]
    public class BaseMongoNumberEntity : BaseEntity<long> { }
}