using CsvHelper;
using experiment_app.Data;
using experiment_app.Data.Mongo;
using MongoDB.Driver;
using Npgsql;
using System;
using System.Globalization;
using System.Text.Json;

// 

namespace experiment_app
{
    internal class Program
    {
        private static MongoClient _mongoClient;
        internal static MongoClient MongoClient
        {
            get
            {
                if(_mongoClient == null)
                {
                    var mongoConnectionString = "mongodb://localhost:27017";
                    _mongoClient = new MongoClient(mongoConnectionString);
                }
                return _mongoClient;
            }
        }

        private static IMongoDatabase _mongoDatabase;
        internal static IMongoDatabase MongoDatabase
        {
            get
            {
                if(_mongoDatabase == null)
                {
                    var databaseName = "experiment_app";
                    _mongoDatabase = MongoClient.GetDatabase(databaseName);
                }
                return _mongoDatabase;
            }
        }

        internal static List<Account> _accounts;
        internal static async Task<List<Account>> GetAccountsAsync()
        {
            if(_accounts == null)
            {
                var result = await GetMongoAccountCollection().FindAsync<Account>(_ => true);
                _accounts = result.ToList();
            }

            return _accounts;
        }

        internal static NpgsqlConnection _pgConn;

        private static async Task<NpgsqlConnection> GetPgConnection()
        {
            if (_pgConn == null)
            {
                var connString = "Host=localhost;Username=experiment_app_user;Password=Lf6awDcpNo;Database=experiment_app";
                _pgConn = new NpgsqlConnection(connString);
                await _pgConn.OpenAsync();
            }
            return _pgConn;
        }

        static async Task Main(string[] args)
        {

            //await AltMain(args);
            var journalCollection = GetJournalCollection();

            for(var n = 0; n < 5; n++)
            {
                var transaction = await GenerateExpenseTransaction();
                Console.WriteLine(JsonSerializer.Serialize(transaction,new JsonSerializerOptions { WriteIndented = true }));
                await journalCollection.InsertOneAsync(transaction);
            }
        }

        static async Task<JournalTransaction> GenerateExpenseTransaction()
        {
            var rng = new Random();

            
            var transaction = new JournalTransaction()
            {
                Amount = rng.Next(100, 1000),
                Created = DateTime.Now,
                Updated = DateTime.Now,
                Memo = "Journal entry (several expenses, see line items)"
            };

            // We always use the same bank account for the cash side of the transaction.
            transaction.Lines.Add(new JournalLine
            {
                AccountId = "622e345be30b4f5dc0768b47",
                OldAccountId = 914,
                Credit = transaction.Amount,
                Memo = "Money leaving the bank account"
            });

            var balanceRemaining = transaction.Amount;
            var numberOfLines = rng.Next(1, 5);

            for(var n = 1; n <= numberOfLines; n++)
            {
                var linesRemaining = numberOfLines - n;
                var account = await GetRandomAccountAsync();

                int amount = balanceRemaining;

                if (n < numberOfLines)
                    amount = rng.Next(1, balanceRemaining - linesRemaining); ;
                var line = new JournalLine
                {
                    AccountId = account.Id,
                    OldAccountId = account.OldId,
                    Memo = await GetExpenseMemo(),
                    Debit = amount,
                    Credit = 0
                };
                transaction.Lines.Add(line);
                balanceRemaining -= amount;
            }

            return transaction;
        }

        static async Task AltMain(string[] args)
        {

            var pgConn = await GetPgConnection();

            var sql = "SELECT accounts.id ,name,type,debit FROM accounts LEFT OUTER JOIN account_types ON account_types.id = accounts.type_id";
            var mongoAccounts = new List<Account>();

            await using var cmd = new NpgsqlCommand(sql, pgConn);
            await using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                mongoAccounts.Add(new Account
                {
                    Name = Convert.ToString(reader["Name"]) ?? String.Empty,
                    Type = Convert.ToString(reader["Type"]) ?? String.Empty,
                    IsDebitAccount = Convert.ToBoolean(reader["debit"]),
                    OldId = Convert.ToInt32(reader["id"])
                });
            }

            IMongoCollection<Account> collection = GetMongoAccountCollection();
            await collection.InsertManyAsync(mongoAccounts);

            pgConn.Close();

        }

        private static IMongoCollection<Account> GetMongoAccountCollection()
        {
            var collection = MongoDatabase.GetCollection<Account>("accounts");
            return collection;
        }

        private static IMongoCollection<JournalTransaction> GetJournalCollection()
        {
            var collection = MongoDatabase.GetCollection<JournalTransaction>("journal");
            return collection;
        }


        private static async Task MongoDbInsert(IMongoCollection<Account> collection)
        {
            var account = new Account
            {
                Name = "Checking",
                Type = "Asset",
                IsDebitAccount = true
            };

            await collection.InsertOneAsync(account);

            var results = await collection.FindAsync(_ => true);

            foreach (var result in results.ToList())
            {
                Console.WriteLine($"{result.Id}: {result.Name} ({result.Type})");
            }
        }

        private static async Task<String> GetExpenseMemo()
        {
            var conn = await GetPgConnection();
            var sql = @"SELECT
                           AnimalName.value AS AnimalName,
                           AppName.value AS AppName,
                           Category.value AS Category,
                           Material.value AS Material
                    FROM
                    (SELECT * FROM random_data WHERE type = 'AnimalName' ORDER BY RANDOM() LIMIT 1) AS AnimalName,
                    (SELECT * FROM random_data WHERE type = 'AppName' ORDER BY RANDOM() LIMIT 1) AS AppName,
                    (SELECT * FROM random_data WHERE type = 'Category' ORDER BY RANDOM() LIMIT 1) AS Category,
                    (SELECT * FROM random_data WHERE type = 'Material' ORDER BY RANDOM() LIMIT 1) AS Material";
            var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = cmd.ExecuteReader();
            string memo = "";
            while(reader.Read())
            {
                memo = $"{Convert.ToString(reader["Material"])} {Convert.ToString(reader["AnimalName"])} for {Convert.ToString(reader["Category"])} on {Convert.ToString(reader["AppName"])} project";
            }

            return memo;
        }
        private static async Task<List<RandomDataRecord>> QueryForAccounts(NpgsqlConnection conn)
        {
            var sql = @"SELECT * FROM random_data WHERE type='Account' ORDER BY RANDOM() LIMIT 10";
            var accounts = new List<RandomDataRecord>();

            await using (var cmd = new NpgsqlCommand(sql, conn))
            {
                await using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    accounts.Add(new RandomDataRecord
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        Type = Convert.ToString(reader["type"]) ?? String.Empty,
                        Value = Convert.ToString(reader["value"]) ?? String.Empty
                    });
                }
            }

            return accounts;
        }

        private static async Task SeedRandomDataTable(NpgsqlConnection conn)
        {
            var rowsInserted = 0;
            var rowsAttempted = 0;

            using (var reader = new StreamReader(@"c:\data\temp\deleteme\deleteme2.csv"))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<CsvRandomDataRow>();
                foreach (var record in records)
                {
                    var json = JsonSerializer.Serialize(record);
                    var properties = record.GetType().GetProperties();
                    foreach (var property in properties)
                    {
                        if (property.Name == "Id")
                            continue;
                        //Console.WriteLine($"{property.Name}: {property.GetValue(record)}");
                        // create unique index random_data_type_value_uindex on random_data(type, value);
                        var sql = @"INSERT INTO random_data (type, value) VALUES (@type,@value)
                                    ON CONFLICT ON CONSTRAINT random_data_type_value_key DO NOTHING";

                        await using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.Add(new NpgsqlParameter
                            {
                                NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar,
                                Value = property.Name,
                                ParameterName = "@type"
                            });

                            cmd.Parameters.Add(new NpgsqlParameter
                            {
                                NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar,
                                Value = property.GetValue(record),
                                ParameterName = "@value"
                            });
                            var result = await cmd.ExecuteNonQueryAsync();
                            rowsAttempted++;
                            rowsInserted += result;
                            Console.WriteLine($"Inserted row {rowsInserted} ({rowsAttempted} attempted), {property.Name}:{property.GetValue(record)}");
                        }
                    }
                }
            }
        }

        private static void ReadCsv()
        {
            using (var reader = new StreamReader(@"c:\data\temp\deleteme\deleteme2.csv"))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<CsvRandomDataRow>();
                foreach (var record in records)
                {
                    var json = JsonSerializer.Serialize(record);
                    Console.WriteLine(json);
                }
            }
        }

        private static async Task<Account> GetRandomAccountAsync()
        {
            var random = new Random();
            var accounts = await GetAccountsAsync();
            var upperBound = accounts.Count();
            var accountIndex = random.Next(0, upperBound - 1);
            return accounts[accountIndex];
        }
    }
}