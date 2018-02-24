using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class Webpage : TableEntity
    {
        private string url;
        private string title;
        private string date;

        public string Url
        {
            get
            {
                return url;
            }

            set
            {
                url = value;
            }
        }

        public string Title
        {
            get
            {
                return title;
            }

            set
            {
                title = value;
            }
        }

        public string Date
        {
            get
            {
                return date;
            }

            set
            {
                date = value;
            }
        }

        public Webpage(string url, string title)
        {
            this.PartitionKey = url;
            this.RowKey = Guid.NewGuid().ToString();
            this.Url = url;
            this.Title = title;

        }

    }
}
