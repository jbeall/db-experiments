using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace experiment_app.Data
{
    internal class CsvRandomDataRow
    {
        public int Id { get; set; }
        public string Lastname { get; set; }
        public string Firstname { get; set; }
        public string AppName { get; set; }
        public string AnimalName { get; set; }
        public string CompanyName { get; set; }
        public string PlantName { get; set; }
        public string Category { get; set; }
        public string Account { get; set; }
        public string Material { get; set; }
        public string Equipment { get; set; }

        public CsvRandomDataRow()
        {
            Id = -1;
            Lastname = String.Empty;
            Firstname = String.Empty;
            AppName = String.Empty;
            AnimalName = String.Empty;
            CompanyName = String.Empty;
            PlantName = String.Empty;
            Category = String.Empty;
            Account = String.Empty;
            Equipment = String.Empty;
            Material = String.Empty;
        }

    }
}
