using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace experiment_app.Data.Mongo
{
    public class JournalTransaction
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Memo { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public int Amount { get; set; }
        public List<JournalLine> Lines { get; set; }

        public JournalTransaction()
        {
            this.Lines = new List<JournalLine>();
        }
    }

    public class JournalLine
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Memo { get; set; }
        public string AccountId { get; set; }
        [BsonIgnore]
        public int OldAccountId { get; set; }
        public int Debit { get; set; }
        public int Credit { get; set; }
    }
}
