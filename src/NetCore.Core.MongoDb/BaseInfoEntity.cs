using System;
using System.Runtime.Serialization;

namespace NetCore.Core.MongoDb
{
    [DataContract(IsReference = true)]
    public abstract class BaseInfoEntity
    {
        [DataMember]
        public DateTime created_at { get; set; }
        [DataMember]
        public long created_by { get; set; }
        [DataMember]
        public DateTime updated_at { get; set; }
        [DataMember]
        public long updated_by { get; set; }
        [DataMember]
        public bool is_deleted { get; set; }
    }
}