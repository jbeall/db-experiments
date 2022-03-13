using CsvHelper;
using experiment_app.Data;
using Npgsql;
using System;
using System.Globalization;
using System.Text.Json;

// 

namespace experiment_app
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var connString = "Host=localhost;Username=experiment_app_user;Password=Lf6awDcpNo;Database=experiment_app";

            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            var rowsInserted = 0;
            var rowsAttempted = 0;

            using (var reader = new StreamReader(@"c:\data\temp\deleteme\deleteme2.csv"))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<RandomDataRow>();
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
            //await using (var cmd = new NpgsqlCommand("INSERT INTO random_data (type,value) VALUES (@type,@value)", conn))
            //{
            //    cmd.Parameters.Add(new NpgsqlParameter
            //    {
            //        NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar,
            //        Value = "More",
            //        ParameterName = "@type"
            //    });

            //    cmd.Parameters.Add(new NpgsqlParameter
            //    {
            //        NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar,
            //        Value = "And More Stuff that goes in the DB!",
            //        ParameterName = "@value"
            //    });
            //    var result = await cmd.ExecuteNonQueryAsync();
            //    Console.WriteLine(result.ToString());
            //    Console.WriteLine();
            //}

            // Retrieve all rows
            //await using (var cmd = new NpgsqlCommand("SELECT * FROM random_data", conn))
            //await using (var reader = await cmd.ExecuteReaderAsync())
            //{
            //    while (await reader.ReadAsync())
            //    {
            //        Console.WriteLine("Start row:\n");
            //        for (int i = 0; i < reader.FieldCount; i++)
            //        {
            //            Console.WriteLine($"{reader.GetName(i)}: {Convert.ToString(reader.GetValue(i))}");
            //        }
            //        Console.WriteLine("End row.\n\n");
            //    }
            //}

            conn.Close();

            //ReadCsv();
        }

        private static void ReadCsv()
        {
            using (var reader = new StreamReader(@"c:\data\temp\deleteme\deleteme2.csv"))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<RandomDataRow>();
                foreach (var record in records)
                {
                    var json = JsonSerializer.Serialize(record);
                    Console.WriteLine(json);
                }
            }
        }
    }
}