using NetCore.Core.MongoDb.Test.Models;
using MongoDB.Driver;

namespace NetCore.Core.MongoDb.Test.Repositories
{
    public class BookRepository : BaseRepository<BookEntity, long>
    {
        public BookRepository(IMongoDatabase db, SequenceRepository seq) : base(db)
        {

        }
        
    }
}