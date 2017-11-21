using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using thrThreadLoop;

namespace RecData {

	public class RecordData {

		public string _filename;
		public int _dataColumns;
    public bool closed;
        private string textToWrite;
        private int _dataCount;
        private int _writePeriod;
        private DataQueueThreadLoop _dataQueueThread;

        /// <summary>
        /// Constructor. 
        /// Inputs: file name and number of data columns for this file.
        /// </summary>
        public RecordData (string filename = "", int dataColumns = 1, int writePeriod = 20) {
			this._filename = filename;
			this._dataColumns = dataColumns;
            this._writePeriod = writePeriod;

            // Here do the directory checking
            if (this._filename == "")
            {
                this._filename = "default_data";
            }

            //// save file to Data directory
            //this._filename = Directory.GetCurrentDirectory() + "/Assets/Data/" + this._filename;

            // check to see file name is not already in use so
            // we don't override existing data
            string temp = this._filename + ".txt";
            int k = 0;
            while (File.Exists(temp))
            {
                Debug.Log("File name already in use. Finding an alternative.");
                temp = this._filename + "_" + k + ".txt";
                k++;
            }

            this._filename = temp;

            this.closed = false;

            _dataQueueThread = new DataQueueThreadLoop("dataQueueThread", true, System.Threading.ThreadPriority.Lowest, 5, 4, this._filename);
            _dataQueueThread.Start();

        }

        /// <summary>
        /// Add Data Method. 
        /// Inputs: datapoints as strings. Expects same # as columns.
        /// Ex. if # columns is 3 --> addData("1","2","3") is accepted.
        /// Do it with threads. e.g. every N data kick off a thread to append to file.
        /// </summary>
        public void addData (params string [] datapoint) {
            // here kick off a thread to save data every N data points
            for (int c = 0; c < this._dataColumns; c++)
            {
                textToWrite = textToWrite + datapoint[c] + ",";
            }
            textToWrite = textToWrite + "\n";
            this._dataCount++;

            // Every so many data points try to write it
            if ((this._dataCount % this._writePeriod) == 0 )
            {
                _dataQueueThread.EnqueueDataToWrite(textToWrite);
                textToWrite = "";
            }
        }

        public void closeRecorder()
        {
            this.closed = true;
            Debug.Log("Closing recorder for " + _filename);
            _dataQueueThread.CloseAtConvenience();
        }

        public bool isClosed()
        {
            return this.closed;
        }

    }
}
