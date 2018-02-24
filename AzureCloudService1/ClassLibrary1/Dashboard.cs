using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class Dashboard : TableEntity
    {
        private string state; //
        private double cpuUtilizationPercent;
        private double ramAvailable;
        private int numberOfUrlCrawled; //
        private string lastTenUrl; //
        private int sizeOfQueue; //#urls left in pipeline to be crawled
        private int sizeOfIndex; //table storage with our crawled data
        private int errorNumber; //
        private string errorUrl; //

        public string State
        {
            get
            {
                return state;
            }

            set
            {
                state = value;
            }
        }

        public double CpuUtilizationPercent
        {
            get
            {
                return cpuUtilizationPercent;
            }

            set
            {
                cpuUtilizationPercent = value;
            }
        }

        public double RamAvailable
        {
            get
            {
                return ramAvailable;
            }

            set
            {
                ramAvailable = value;
            }
        }

        public int NumberOfUrlCrawled
        {
            get
            {
                return numberOfUrlCrawled;
            }

            set
            {
                numberOfUrlCrawled = value;
            }
        }

        public string LastTenUrl
        {
            get
            {
                return lastTenUrl;
            }

            set
            {
                lastTenUrl = value;
            }
        }

        public int SizeOfQueue
        {
            get
            {
                return sizeOfQueue;
            }

            set
            {
                sizeOfQueue = value;
            }
        }

        public int SizeOfIndex
        {
            get
            {
                return sizeOfIndex;
            }

            set
            {
                sizeOfIndex = value;
            }
        }

        public int ErrorNumber
        {
            get
            {
                return errorNumber;
            }

            set
            {
                errorNumber = value;
            }
        }

        public string ErrorUrl
        {
            get
            {
                return errorUrl;
            }

            set
            {
                errorUrl = value;
            }
        }

        public Dashboard()
        {
            this.PartitionKey = "Dashboard";
            this.RowKey = "Dashboard";
        }

    }
}
