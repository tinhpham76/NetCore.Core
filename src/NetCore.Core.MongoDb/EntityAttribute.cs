using System;

namespace NetCore.Core.MongoDb
{
    public class EntityAttribute : Attribute
    {
        public string Name { get; private set; }
        public string Sequence { get; private set; }

        public EntityAttribute(string name = null, string sequence = null)
        {
            if (name != null)
                name = name.Trim();
                
            if (sequence != null)
                sequence = sequence.Trim();

            this.Name = name;
            this.Sequence = sequence;
        }
    }
}
